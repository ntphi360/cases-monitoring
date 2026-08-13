using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Models.Auth;
using HoSoMonitoring.Core.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HoSoMonitoring.Data.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    public TokenService(JwtOptions options) => _options = options;

    public IssuedAccessToken CreateAccessToken(User user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _options.Issuer, _options.Audience, claims,
            expires: expiresAt, signingCredentials: credentials);
        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new IssuedRefreshToken(
            value, HashRefreshToken(value),
            DateTime.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    public string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
