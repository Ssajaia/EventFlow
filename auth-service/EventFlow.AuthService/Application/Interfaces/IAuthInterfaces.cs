using EventFlow.AuthService.Application.DTOs;
using EventFlow.AuthService.Domain.Entities;

namespace EventFlow.AuthService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IJwtService
{
    string GenerateAccessToken(User user, string roleName);
    RefreshToken GenerateRefreshToken(Guid userId);
    bool IsTokenBlacklisted(string token);
    Task BlacklistTokenAsync(string token, TimeSpan expiry);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
