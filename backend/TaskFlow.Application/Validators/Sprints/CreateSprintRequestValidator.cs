using FluentValidation;
using TaskFlow.Application.DTOs.Sprints;

namespace TaskFlow.Application.Validators.Sprints;

public class CreateSprintRequestValidator : AbstractValidator<CreateSprintRequest>
{
    public CreateSprintRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Sprint name is required.")
            .MaximumLength(100).WithMessage("Sprint name must not exceed 100 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be on or after start date.");
    }
}
