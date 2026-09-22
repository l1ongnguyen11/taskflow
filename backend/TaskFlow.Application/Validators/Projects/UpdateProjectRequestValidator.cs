using FluentValidation;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Validators.Projects;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project Name is required.")
            .MaximumLength(100).WithMessage("Project Name must not exceed 100 characters.");
    }
}
