using System.Security.Claims;
using System.Text;
using Identity.Api.Domain;
using Identity.Api.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Api.Services;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Issues HMAC-SHA256 signed JWTs. Claims: sub (user id), email, name, role and jti. The gateway and every
/// service validate the same issuer, audience and key, and read the role claim for authorization.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedToken CreateToken(User user)
    {
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new IssuedToken(_handler.CreateToken(descriptor), expires);
    }
}
