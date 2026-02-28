using EventFlow.AuthService.Application.DTOs;
using EventFlow.AuthService.Application.Interfaces;
using EventFlow.AuthService.Application.Services;
using EventFlow.AuthService.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventFlow.AuthService.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepo = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IPasswordService> _password = new();
    private readonly Mock<ILogger<Application.Services.AuthService>> _logger = new();

    private Application.Services.AuthService CreateSut() =>
        new(_userRepo.Object, _roleRepo.Object, _tokenRepo.Object,
            _jwt.Object, _password.Object, _logger.Object);

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        var existingUser = User.Create("test@test.com", "hash", "Test", "User", Guid.NewGuid());
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com", default)).ReturnsAsync(existingUser);

        var request = new RegisterRequest { Email = "test@test.com", Password = "P@ssword1", FirstName = "Test", LastName = "User" };

        // Act
        var act = () => sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var sut = CreateSut();
        var user = User.Create("test@test.com", "correcthash", "Test", "User", Guid.NewGuid());
        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com", default)).ReturnsAsync(user);
        _password.Setup(p => p.Verify("wrongpassword", "correcthash")).Returns(false);

        var request = new LoginRequest { Email = "test@test.com", Password = "wrongpassword" };

        // Act
        var act = () => sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var sut = CreateSut();
        var roleId = Guid.NewGuid();
        var user = User.Create("test@test.com", "correcthash", "John", "Doe", roleId);

        _userRepo.Setup(r => r.GetByEmailAsync("test@test.com", default)).ReturnsAsync(user);
        _password.Setup(p => p.Verify("P@ssword1", "correcthash")).Returns(true);
        _jwt.Setup(j => j.GenerateAccessToken(user, It.IsAny<string>())).Returns("fake-jwt-token");
        _jwt.Setup(j => j.GenerateRefreshToken(user.Id)).Returns(RefreshToken.Create(user.Id));

        var request = new LoginRequest { Email = "test@test.com", Password = "P@ssword1" };

        // Act
        var result = await sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("fake-jwt-token");
        result.User.Email.Should().Be("test@test.com");
    }

    [Fact]
    public void User_Create_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var roleId = Guid.NewGuid();
        var user = User.Create("Test@Example.COM", "hash123", "Jane", "Doe", roleId);

        // Assert
        user.Email.Should().Be("test@example.com"); // lowercased
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Doe");
        user.RoleId.Should().Be(roleId);
        user.IsActive.Should().BeTrue();
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenNotExpiredAndNotRevoked()
    {
        // Arrange
        var token = RefreshToken.Create(Guid.NewGuid(), 30);

        // Assert
        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
    }
}
