using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FastEndpoints.Security;
using Microsoft.IdentityModel.Tokens;
using Zayna.Store.Server.Entities;

namespace Zayna.Store.Server.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var signingKey = _configuration["JwtSettings:SigningKey"] ?? "MyLongSecretForSigningJwtTokens";
        var expiryMinutes = _configuration.GetValue<int>("JwtSettings:AccessTokenExpiryMinutes");
        if (expiryMinutes == 0) expiryMinutes = 30;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        return JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            o.User.Roles.AddRange(roles);
            o.User.Claims.AddRange(claims);
        });
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
