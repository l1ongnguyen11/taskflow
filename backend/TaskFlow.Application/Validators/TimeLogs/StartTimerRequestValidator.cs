using FluentValidation;
using TaskFlow.Application.DTOs.TimeLogs;

namespace TaskFlow.Application.Validators.TimeLogs;

public class StartTimerRequestValidator : AbstractValidator<StartTimerRequest>
{
    public StartTimerRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
