using FluentValidation;
using TaskFlow.Application.DTOs.Sprints;

namespace TaskFlow.Application.Validators.Sprints;

public class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "planning", "active", "completed", "cancelled"
    };

    public UpdateSprintRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Sprint name must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Status)
            .Must(status => status == null || ValidStatuses.Contains(status))
            .WithMessage("Status must be one of: planning, active, completed, cancelled.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be on or after start date.");
    }
}
