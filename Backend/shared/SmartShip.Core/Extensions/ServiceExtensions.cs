using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartShip.Core.Authentication;
using SmartShip.Core.Email;

namespace SmartShip.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddSmartShipCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration);
        services.AddSingleton<IEmailService, EmailService>();
        return services;
    }
}