using FluentValidation;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Validators.Projects;

public class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
{
    public AddProjectMemberRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
}
