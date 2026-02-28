using EventFlow.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.AuthService.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Token).IsRequired();
            e.HasIndex(t => t.Token).IsUnique();
            e.HasIndex(t => new { t.UserId, t.ExpiresAt });
            e.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId);
        });

        // Seed roles
        var adminRole = Role.Create(Role.Names.Admin);
        var userRole = Role.Create(Role.Names.User);

        // Use fixed GUIDs for seeding so migrations are idempotent
        var adminId = new Guid("a0000000-0000-0000-0000-000000000001");
        var userId = new Guid("a0000000-0000-0000-0000-000000000002");

        modelBuilder.Entity<Role>().HasData(
            new { Id = adminId, Name = Role.Names.Admin },
            new { Id = userId, Name = Role.Names.User }
        );
    }
}
