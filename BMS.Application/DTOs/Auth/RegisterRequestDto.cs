namespace BMS.Application.DTOs.Auth;

/// <summary>
/// Data sent by the client when registering a new user account.
/// Role is NOT accepted from the client — all self-registered users
/// are assigned the "Viewer" role by the server. Admin accounts are
/// created exclusively via the database seeder.
/// </summary>
public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
