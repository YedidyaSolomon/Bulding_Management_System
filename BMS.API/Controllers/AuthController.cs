using BMS.API.Wrappers;
using BMS.Application.DTOs.Auth;
using BMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMS.API.Controllers;

/// <summary>
/// Handles authentication and role management endpoints.
///
/// RBAC design:
///   - Register  : public, always creates a "Viewer" account.
///   - Login     : public, returns a JWT with the user's role embedded as a claim.
///   - AssignRole: Admin-only. Promotes/demotes users between Viewer and Manager.
///                 Admin role cannot be assigned via API — seeded at startup only.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new Viewer account.
    /// Role is always set to "Viewer" by the server.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register),
            ApiResponse<RegisterResponseDto>.Ok(result, "Registration successful."));
    }

    /// <summary>
    /// Authenticate and receive a JWT token.
    /// The token includes the user's role claim for use with [Authorize(Roles=...)].
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
    }

    /// <summary>
    /// Assign a role to an existing user. Admin-only.
    /// Assignable roles: Manager, Viewer.
    /// Admin role cannot be assigned via this endpoint.
    /// </summary>
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<AssignRoleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
    {
        var result = await _authService.AssignRoleAsync(request);
        return Ok(ApiResponse<AssignRoleResponseDto>.Ok(
            result,
            $"Role updated: '{result.PreviousRole}' → '{result.NewRole}' for {result.UserEmail}."));
    }
}
