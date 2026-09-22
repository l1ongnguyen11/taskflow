using FluentValidation;
using TaskFlow.Application.DTOs.Activities;

namespace TaskFlow.Application.Validators.Activities;

public class LogActivityRequestValidator : AbstractValidator<LogActivityRequest>
{
    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "created", "updated", "deleted", "moved", "assigned",
        "unassigned", "commented", "attached", "status_changed"
    };

    public LogActivityRequestValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Entity type is required.")
            .MaximumLength(50).WithMessage("Entity type must not exceed 50 characters.");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .Must(action => ValidActions.Contains(action))
            .WithMessage("Action must be one of: created, updated, deleted, moved, assigned, unassigned, commented, attached, status_changed.");
    }
}
