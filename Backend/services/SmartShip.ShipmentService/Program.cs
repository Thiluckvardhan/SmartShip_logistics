using System.Text;
using System.Text.Json.Serialization;
using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartShip.Core.Exceptions;
using SmartShip.Core.Email;
using SmartShip.Core.Messaging;
using SmartShip.Core.Observability;
using SmartShip.ShipmentService.Data;
using SmartShip.ShipmentService.Repositories;
using SmartShip.ShipmentService.Services;
using Serilog;

LoadDotEnvIntoEnvironment(Directory.GetCurrentDirectory());
var builder = WebApplication.CreateBuilder(args);

var solutionRoot = ResolveSolutionRoot(builder.Environment.ContentRootPath);
var logsDirectory = Path.Combine(solutionRoot, "logs");
var serviceLogsDirectory = Path.Combine(logsDirectory, "shipment-service");
Directory.CreateDirectory(logsDirectory);
Directory.CreateDirectory(serviceLogsDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(serviceLogsDirectory, "shipment-service-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [Corr:{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing configuration: Jwt:Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing configuration: Jwt:Issuer");
var jwtAudiences = GetJwtAudiences(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartShip Shipment Service",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement((document) => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudiences = jwtAudiences,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDbContext<ShipmentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartShip.ShipmentService")));
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddSingleton<IServiceTokenGenerator, ServiceTokenGenerator>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = builder.Configuration["Services:IdentityServiceBaseUrl"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
});
builder.Services.AddSingleton<IEventBus, RabbitMQService>();
builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipmentDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Shipment API v1"));
}

static string[] GetJwtAudiences(IConfiguration configuration)
{
    var audiences = configuration.GetSection("Jwt:Audiences").Get<string[]>();
    if (audiences is { Length: > 0 })
    {
        return audiences.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    return [configuration["Jwt:Audience"] ?? "SmartShipClients"];
}

app.UseCors("AllowAll");
app.UseCorrelationId();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});
app.UseMiddleware<GlobalExceptionHandler>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

static string ResolveSolutionRoot(string contentRoot)
{
    var current = new DirectoryInfo(contentRoot);

    while (current != null)
    {
        var hasGateway = Directory.Exists(Path.Combine(current.FullName, "gateway"));
        var hasServices = Directory.Exists(Path.Combine(current.FullName, "services"));
        var hasShared = Directory.Exists(Path.Combine(current.FullName, "shared"));

        if (hasGateway && hasServices && hasShared)
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return contentRoot;
}

static void LoadDotEnvIntoEnvironment(string startPath)
{
    var envFilePath = FindFileInParents(startPath, ".env");
    if (envFilePath is null || !File.Exists(envFilePath))
    {
        return;
    }

    foreach (var rawLine in File.ReadLines(envFilePath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            continue;
        }

        var key = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(key))
        {
            continue;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

static string? FindFileInParents(string startPath, string fileName)
{
    var current = new DirectoryInfo(startPath);
    while (current is not null)
    {
        var candidate = Path.Combine(current.FullName, fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        current = current.Parent;
    }

    return null;
}
