using EventFlow.AuthService.Application.Interfaces;
using EventFlow.AuthService.Domain.Entities;
using EventFlow.AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;
    public RefreshTokenRepository(AuthDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        _db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _db;
    public RoleRepository(AuthDbContext db) => _db = db;

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Roles.FindAsync([id], ct).AsTask()!;

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(role, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
