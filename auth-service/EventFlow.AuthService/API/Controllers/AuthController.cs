using EventFlow.AuthService.Application.DTOs;
using EventFlow.AuthService.Application.Interfaces;
using EventFlow.SharedKernel.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventFlow.AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Ok(result, "Registration successful."));
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    /// <summary>Refresh an access token using a valid refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request, CancellationToken ct)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>Health check for the auth service.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(ApiResponse<object>.Ok(claims));
    }
}
