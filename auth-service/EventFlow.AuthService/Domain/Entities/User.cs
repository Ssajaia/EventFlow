namespace EventFlow.AuthService.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private User() { }

    public static User Create(string email, string passwordHash, string firstName, string lastName, Guid roleId)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastModified() => UpdatedAt = DateTime.UtcNow;
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
