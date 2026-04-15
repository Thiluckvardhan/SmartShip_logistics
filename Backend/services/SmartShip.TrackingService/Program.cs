using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartShip.Core.Exceptions;
using SmartShip.Core.Observability;
using SmartShip.TrackingService.Data;
using SmartShip.TrackingService.Repositories;
using SmartShip.TrackingService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var solutionRoot = ResolveSolutionRoot(builder.Environment.ContentRootPath);
var logsDirectory = Path.Combine(solutionRoot, "logs");
var serviceLogsDirectory = Path.Combine(logsDirectory, "tracking-service");
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
        path: Path.Combine(serviceLogsDirectory, "tracking-service-.txt"),
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
builder.Services.AddDbContext<TrackingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartShip.TrackingService")));
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddHostedService<TrackingEventConsumer>();

await EnsureTrackingSchemaAsync(builder.Services);

var app = builder.Build();

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

static async Task EnsureTrackingSchemaAsync(IServiceCollection services)
{
    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TrackingDbContext>();

    await dbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.TrackingLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrackingLogs (
        TrackingLogId uniqueidentifier NOT NULL PRIMARY KEY,
        ShipmentId uniqueidentifier NOT NULL,
        TrackingNumber nvarchar(100) NOT NULL,
        Status nvarchar(50) NOT NULL,
        Location nvarchar(200) NOT NULL,
        Description nvarchar(1000) NOT NULL,
        Timestamp datetime2 NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.TrackingLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrackingLocations (
        LocationId uniqueidentifier NOT NULL PRIMARY KEY,
        TrackingNumber nvarchar(100) NOT NULL,
        Latitude float NOT NULL,
        Longitude float NOT NULL,
        Timestamp datetime2 NOT NULL
    );
END;
");
}

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
