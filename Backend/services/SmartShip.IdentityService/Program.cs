using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartShip.Core.Email;
using SmartShip.Core.Exceptions;
using SmartShip.Core.Observability;
using SmartShip.IdentityService.Data;
using SmartShip.IdentityService.Models;
using SmartShip.IdentityService.Repositories;
using SmartShip.IdentityService.Services;
using Serilog;

LoadDotEnvIntoEnvironment(Directory.GetCurrentDirectory());
var builder = WebApplication.CreateBuilder(args);

var solutionRoot = ResolveSolutionRoot(builder.Environment.ContentRootPath);
var logsDirectory = Path.Combine(solutionRoot, "logs");
var serviceLogsDirectory = Path.Combine(logsDirectory, "identity-service");
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
        path: Path.Combine(serviceLogsDirectory, "identity-service-.txt"),
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

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(new
                {
                    message = "Unauthorized. Please login with valid credentials and provide a valid access token."
                });

                await context.Response.WriteAsync(payload);
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted) return;

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var payload = JsonSerializer.Serialize(new
                {
                    message = "Forbidden. You do not have permission to access this endpoint."
                });

                await context.Response.WriteAsync(payload);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartShip.IdentityService")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Seed default roles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Roles.AnyAsync(r => r.RoleName == "Admin"))
    {
        db.Roles.Add(new Role { RoleName = "Admin" });
    }

    if (!await db.Roles.AnyAsync(r => r.RoleName == "Customer"))
    {
        db.Roles.Add(new Role { RoleName = "Customer" });
    }

    await db.SaveChangesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.InjectJavascript("/swagger-prefill.js");
    });
}

app.UseStaticFiles();
app.UseCorrelationId();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});
app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors("AllowAll");
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

static string[] GetJwtAudiences(IConfiguration configuration)
{
    var audiences = configuration.GetSection("Jwt:Audiences").Get<string[]>();
    if (audiences is { Length: > 0 })
    {
        return audiences.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    return [configuration["Jwt:Audience"] ?? "SmartShipClients"];
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
