namespace EventFlow.AuthService.Application.DTOs;

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record RevokeTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiry { get; init; }
    public UserDto User { get; init; } = null!;
}

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
