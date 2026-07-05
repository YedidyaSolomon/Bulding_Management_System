namespace BMS.Application.DTOs.Auth;

/// <summary>
/// Returned to the client after successful register or login.
/// The Angular app stores the token and uses it in every subsequent request.
/// </summary>
public class AuthResponseDto
{
    public string Token     { get; set; } = string.Empty;
    public string FullName  { get; set; } = string.Empty;
    public string Email     { get; set; } = string.Empty;
    public string Role      { get; set; } = string.Empty;
    public DateTime Expiry  { get; set; }
}
