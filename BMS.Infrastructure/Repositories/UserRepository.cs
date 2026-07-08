using BMS.Application.Interfaces.Repositories;
using BMS.Infrastructure.Data;
using BMS.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BMS.Infrastructure.Repositories;

/// <summary>
/// Wraps ASP.NET Core Identity UserManager/SignInManager so the Application
/// layer never depends on Identity directly.
/// AppUser no longer has a TenantId — tenant ownership lives on Tenant.AppUserId.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserManager<AppUser>   _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public UserRepository(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    public async Task<UserDto?> FindByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : new UserDto(user.Id, user.Email!, user.FullName, user.IsActive);
    }

    public async Task<UserDto?> FindByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : new UserDto(user.Id, user.Email!, user.FullName, user.IsActive);
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Email)
            .ToListAsync();
        return users.Select(u => new UserDto(u.Id, u.Email!, u.FullName, u.IsActive));
    }

    public async Task<string> CreateAsync(string fullName, string email, string password)
    {
        var user = new AppUser
        {
            FullName = fullName,
            Email    = email,
            UserName = email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    public async Task AddToRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new KeyNotFoundException($"User {userId} not found.");

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task RemoveFromRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new KeyNotFoundException($"User {userId} not found.");

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<string?> GetRoleAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        return result.Succeeded;
    }

    public async Task<bool> IsActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user?.IsActive ?? false;
    }
}
