using BMS.Application.DTOs.Auth;

namespace BMS.Application.Interfaces;

public interface IAuthService
{
    Task<RegisterResponseDto>  RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto>      LoginAsync(LoginRequestDto request);
    Task<AssignRoleResponseDto> AssignRoleAsync(AssignRoleRequestDto request);
}
