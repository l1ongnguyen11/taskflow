using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class MoveTaskRequestValidator : AbstractValidator<MoveTaskRequest>
{
    public MoveTaskRequestValidator()
    {
        RuleFor(x => x.TargetColumnId).NotEmpty().WithMessage("Target Column ID is required.");
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0).WithMessage("Position must be non-negative.");
    }
}
