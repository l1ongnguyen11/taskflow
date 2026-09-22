using FluentValidation;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Validators.Projects;

public class UpdateProjectMemberRoleRequestValidator : AbstractValidator<UpdateProjectMemberRoleRequest>
{
    public UpdateProjectMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
}
