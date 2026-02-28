using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventFlow.AuthService.Application.Interfaces;
using EventFlow.AuthService.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace EventFlow.AuthService.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly IConnectionMultiplexer _redis;

    public JwtService(IConfiguration config, IConnectionMultiplexer redis)
    {
        _config = config;
        _redis = redis;
    }

    public string GenerateAccessToken(User user, string roleName)
    {
        var secret = _config["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = int.Parse(_config["JWT_EXPIRY_MINUTES"] ?? "15");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
        };

        var token = new JwtSecurityToken(
            issuer: _config["JWT_ISSUER"] ?? "eventflow-auth",
            audience: _config["JWT_AUDIENCE"] ?? "eventflow",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId) => RefreshToken.Create(userId, 30);

    public bool IsTokenBlacklisted(string token)
    {
        var db = _redis.GetDatabase();
        return db.KeyExists($"blacklist:{token}");
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"blacklist:{token}", "1", expiry);
    }
}

public class PasswordService : IPasswordService
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, 12);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
