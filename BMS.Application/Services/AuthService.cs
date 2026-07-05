using BMS.Application.DTOs.Auth;
using BMS.Application.Interfaces;
using BMS.Application.Interfaces.Repositories;

namespace BMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository  _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    // Roles that can be assigned via the API by an Admin.
    // "Admin" is intentionally excluded — Admin is seeded at startup only.
    private static readonly HashSet<string> AssignableRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Manager", "Viewer" };

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository  = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var existing = await _userRepository.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        // Role is always hardcoded to "Viewer" — clients cannot self-assign roles.
        // Admin accounts are provisioned exclusively via the AdminSeeder at startup.
        const string defaultRole = "Viewer";

        var userId = await _userRepository.CreateAsync(request.FullName, request.Email, request.Password);
        await _userRepository.AddToRoleAsync(userId, defaultRole);

        return new RegisterResponseDto
        {
            FullName = request.FullName,
            Email    = request.Email,
            Role     = defaultRole
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var passwordValid = await _userRepository.CheckPasswordAsync(user.Id, request.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var role   = await _userRepository.GetRoleAsync(user.Id) ?? "Viewer";
        var token  = _jwtTokenService.GenerateToken(user.Id, user.Email, user.FullName, role);
        var expiry = _jwtTokenService.GetExpiry();

        return new AuthResponseDto
        {
            Token    = token,
            FullName = user.FullName,
            Email    = user.Email,
            Role     = role,
            Expiry   = expiry
        };
    }

    public async Task<AssignRoleResponseDto> AssignRoleAsync(AssignRoleRequestDto request)
    {
        // Guard: only Manager and Viewer are assignable via API
        if (!AssignableRoles.Contains(request.NewRole))
            throw new InvalidOperationException(
                "Role must be 'Manager' or 'Viewer'. Admin role cannot be assigned via API.");

        var user = await _userRepository.FindByEmailAsync(request.UserEmail)
                   ?? throw new KeyNotFoundException($"No user found with email '{request.UserEmail}'.");

        var currentRole = await _userRepository.GetRoleAsync(user.Id) ?? "Viewer";

        // Guard: prevent assigning the same role the user already has
        if (string.Equals(currentRole, request.NewRole, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"User '{request.UserEmail}' already has the '{request.NewRole}' role.");

        // Guard: Admin accounts cannot be demoted via API
        if (string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Admin accounts cannot be reassigned via API.");

        // Swap role: remove current, assign new
        await _userRepository.RemoveFromRoleAsync(user.Id, currentRole);
        await _userRepository.AddToRoleAsync(user.Id, request.NewRole);

        return new AssignRoleResponseDto
        {
            UserEmail    = request.UserEmail,
            PreviousRole = currentRole,
            NewRole      = request.NewRole
        };
    }
}
