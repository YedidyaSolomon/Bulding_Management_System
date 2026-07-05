namespace BMS.Application.DTOs.Auth;

/// <summary>
/// Returned to the client after successful registration.
/// No token is issued at registration — the user must login separately.
/// </summary>
public class RegisterResponseDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Role     { get; set; } = string.Empty;
}
