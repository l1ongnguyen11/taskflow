using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task Title is required.")
            .MaximumLength(500).WithMessage("Task Title must not exceed 500 characters.");

        RuleFor(x => x.Type)
            .Must(t => string.IsNullOrEmpty(t) || new[] { "task", "bug", "story", "epic", "subtask" }.Contains(t.Trim().ToLowerInvariant()))
            .WithMessage("Task Type must be one of: task, bug, story, epic, subtask.");

        RuleFor(x => x.Priority)
            .Must(p => string.IsNullOrEmpty(p) || new[] { "lowest", "low", "medium", "high", "highest" }.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Task Priority must be one of: lowest, low, medium, high, highest.");

        RuleFor(x => x.StoryPoints)
            .GreaterThanOrEqualTo((short)0).When(x => x.StoryPoints.HasValue)
            .WithMessage("Story Points must be non-negative.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value).When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Due Date must be greater than or equal to Start Date.");
    }
}
