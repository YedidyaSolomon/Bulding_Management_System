namespace BMS.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserDto?>  FindByEmailAsync(string email);
    Task<UserDto?>  FindByIdAsync(string userId);
    Task<string>    CreateAsync(string fullName, string email, string password);
    Task            AddToRoleAsync(string userId, string role);
    Task            RemoveFromRoleAsync(string userId, string role);
    Task<string?>   GetRoleAsync(string userId);
    Task<bool>      CheckPasswordAsync(string userId, string password);
    Task<bool>      IsActiveAsync(string userId);
}

public record UserDto(string Id, string Email, string FullName, bool IsActive);
