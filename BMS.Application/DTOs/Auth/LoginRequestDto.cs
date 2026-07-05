namespace BMS.Application.DTOs.Auth;

/// <summary>
/// Credentials submitted by the client on the login screen.
/// </summary>
public class LoginRequestDto
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
