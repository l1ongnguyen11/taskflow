using FluentValidation;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Validators.Workspaces;

public class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role.ToLower() == "admin" || role.ToLower() == "member")
            .WithMessage("Role must be either 'admin' or 'member'.");
    }
}
