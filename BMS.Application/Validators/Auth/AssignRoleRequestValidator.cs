using BMS.Application.DTOs.Auth;
using FluentValidation;

namespace BMS.Application.Validators.Auth;

/// <summary>
/// Validates the AssignRoleRequestDto.
/// "Admin" is intentionally excluded from assignable roles via this endpoint —
/// Admin accounts are seeded at startup only.
/// </summary>
public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequestDto>
{
    private static readonly string[] AssignableRoles = { "Manager", "Viewer" };

    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.NewRole)
            .NotEmpty().WithMessage("New role is required.")
            .Must(r => AssignableRoles.Contains(r))
            .WithMessage("Role must be 'Manager' or 'Viewer'. Admin role cannot be assigned via API.");
    }
}
