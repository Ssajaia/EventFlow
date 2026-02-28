namespace EventFlow.AuthService.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, int expiryDays = 30)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray() + Guid.NewGuid().ToByteArray()),
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
    }
}
