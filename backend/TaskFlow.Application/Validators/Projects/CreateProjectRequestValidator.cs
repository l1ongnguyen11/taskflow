using FluentValidation;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Validators.Projects;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project Name is required.")
            .MaximumLength(100).WithMessage("Project Name must not exceed 100 characters.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Project Key is required.")
            .MinimumLength(2).WithMessage("Project Key must be at least 2 characters.")
            .MaximumLength(10).WithMessage("Project Key must not exceed 10 characters.")
            .Matches("^[A-Z0-9]+$").WithMessage("Project Key must contain only uppercase alphanumeric characters.");
    }
}
