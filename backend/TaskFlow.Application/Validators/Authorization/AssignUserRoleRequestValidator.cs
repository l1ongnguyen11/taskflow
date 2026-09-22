using FluentValidation;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Validators.Authorization;

public class AssignUserRoleRequestValidator : AbstractValidator<AssignUserRoleRequest>
{
    public AssignUserRoleRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required.");
    }
}
