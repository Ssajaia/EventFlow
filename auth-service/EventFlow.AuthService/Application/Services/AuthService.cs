using EventFlow.AuthService.Application.DTOs;
using EventFlow.AuthService.Application.Interfaces;
using EventFlow.AuthService.Domain.Entities;

namespace EventFlow.AuthService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtService _jwt;
    private readonly IPasswordService _password;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IJwtService jwt,
        IPasswordService password,
        ILogger<AuthService> logger)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _password = password;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var role = await _roles.GetByNameAsync(Role.Names.User, ct)
            ?? throw new InvalidOperationException("Default role not found. Run database seeding.");

        var hash = _password.Hash(request.Password);
        var user = User.Create(request.Email, hash, request.FirstName, request.LastName, role.Id);

        await _users.AddAsync(user, ct);

        var refreshToken = _jwt.GenerateRefreshToken(user.Id);
        await _refreshTokens.AddAsync(refreshToken, ct);
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email}", request.Email);

        return BuildAuthResponse(user, role.Name, refreshToken.Token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_password.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var roleName = user.Role?.Name ?? Role.Names.User;
        var refreshToken = _jwt.GenerateRefreshToken(user.Id);
        await _refreshTokens.AddAsync(refreshToken, ct);
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation("User logged in: {Email}", request.Email);

        return BuildAuthResponse(user, roleName, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _refreshTokens.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        var user = await _users.GetByIdAsync(token.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        var newRefreshToken = _jwt.GenerateRefreshToken(user.Id);
        token.Revoke(newRefreshToken.Token);
        await _refreshTokens.AddAsync(newRefreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        var roleName = user.Role?.Name ?? Role.Names.User;
        return BuildAuthResponse(user, roleName, newRefreshToken.Token);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _refreshTokens.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!token.IsActive)
            throw new UnauthorizedAccessException("Token is already revoked or expired.");

        token.Revoke();
        await _refreshTokens.SaveChangesAsync(ct);
        _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
    }

    private AuthResponse BuildAuthResponse(User user, string roleName, string refreshToken)
    {
        var expiry = DateTime.UtcNow.AddMinutes(15);
        var accessToken = _jwt.GenerateAccessToken(user, roleName);
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = expiry,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roleName
            }
        };
    }
}
