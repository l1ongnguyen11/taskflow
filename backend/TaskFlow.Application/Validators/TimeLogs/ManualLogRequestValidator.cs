using FluentValidation;
using TaskFlow.Application.DTOs.TimeLogs;

namespace TaskFlow.Application.Validators.TimeLogs;

public class ManualLogRequestValidator : AbstractValidator<ManualLogRequest>
{
    public ManualLogRequestValidator()
    {
        RuleFor(x => x.StartedAt)
            .NotEmpty().WithMessage("Started at is required.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duration must be greater than zero.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
