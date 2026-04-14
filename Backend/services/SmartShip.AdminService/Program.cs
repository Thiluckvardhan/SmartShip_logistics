using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartShip.Core.Exceptions;
using SmartShip.Core.Observability;
using SmartShip.AdminService.Data;
using SmartShip.AdminService.Repositories;
using SmartShip.AdminService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var solutionRoot = ResolveSolutionRoot(builder.Environment.ContentRootPath);
var logsDirectory = Path.Combine(solutionRoot, "logs");
var serviceLogsDirectory = Path.Combine(logsDirectory, "admin-service");
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
        path: Path.Combine(serviceLogsDirectory, "admin-service-.txt"),
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
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartShip.AdminService")));
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IServiceTokenGenerator, ServiceTokenGenerator>();
builder.Services.AddHttpClient("ShipmentService", client =>
{
    var baseUrl = builder.Configuration["Services:ShipmentServiceBaseUrl"] ?? "http://localhost:8002";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddHttpClient("IdentityService", client =>
{
    var baseUrl = builder.Configuration["Services:IdentityServiceBaseUrl"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
