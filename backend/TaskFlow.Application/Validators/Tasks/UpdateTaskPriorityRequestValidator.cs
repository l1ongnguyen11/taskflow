using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class UpdateTaskPriorityRequestValidator : AbstractValidator<UpdateTaskPriorityRequest>
{
    public UpdateTaskPriorityRequestValidator()
    {
        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => new[] { "lowest", "low", "medium", "high", "highest" }.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Priority must be one of: lowest, low, medium, high, highest.");
    }
}
