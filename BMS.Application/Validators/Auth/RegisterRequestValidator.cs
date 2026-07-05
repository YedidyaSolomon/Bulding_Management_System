using BMS.Application.DTOs.Auth;
using FluentValidation;

namespace BMS.Application.Validators.Auth;

/// <summary>
/// Validates the RegisterRequestDto before it reaches the service layer.
/// FluentValidation runs this automatically via the pipeline filter.
/// Role validation is intentionally absent — role is always hardcoded
/// to "Viewer" by the server and never accepted from the client.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
