using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SmartShip.ShipmentService.Services;

public interface IServiceTokenGenerator
{
    string GenerateToken();
}

public class ServiceTokenGenerator(IConfiguration configuration) : IServiceTokenGenerator
{
    private readonly string _jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing configuration: Jwt:Key");
    private readonly string _jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing configuration: Jwt:Issuer");
    private readonly string _jwtAudience = configuration["Jwt:Audience"] ?? "SmartShipClients";

    public string GenerateToken()
    {
        var claims = new List<Claim>
        {
            new("service", "shipment"),
            new(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
