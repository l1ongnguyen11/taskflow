using FluentValidation;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Validators.Authorization;

public class AssignRolePermissionRequestValidator : AbstractValidator<AssignRolePermissionRequest>
{
    public AssignRolePermissionRequestValidator()
    {
        RuleFor(x => x.PermissionId)
            .NotEmpty().WithMessage("Permission ID is required.");
    }
}
