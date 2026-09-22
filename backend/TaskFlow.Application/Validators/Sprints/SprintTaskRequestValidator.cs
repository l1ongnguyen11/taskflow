using FluentValidation;
using TaskFlow.Application.DTOs.Sprints;

namespace TaskFlow.Application.Validators.Sprints;

public class SprintTaskRequestValidator : AbstractValidator<SprintTaskRequest>
{
    public SprintTaskRequestValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");
    }
}
