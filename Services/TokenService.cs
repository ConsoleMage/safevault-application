using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using safevault_application.Models;

namespace safevault_application.Services;

public class TokenService
{
    private readonly IConfiguration _config;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public TokenService(IConfiguration config)
    {
        _config = config;
        var section = _config.GetSection("JwtSettings");
        _secret = section["Secret"] ?? throw new ArgumentNullException("JwtSettings:Secret");
        _issuer = section["Issuer"] ?? string.Empty;
        _audience = section["Audience"] ?? string.Empty;
        _expirationMinutes = int.TryParse(section["ExpirationMinutes"], out var m) ? m : 60;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
