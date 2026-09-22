using FluentValidation;
using TaskFlow.Application.DTOs.Boards;

namespace TaskFlow.Application.Validators.Boards;

public class UpdateBoardColumnRequestValidator : AbstractValidator<UpdateBoardColumnRequest>
{
    public UpdateBoardColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column Name is required.")
            .MaximumLength(100).WithMessage("Column Name must not exceed 100 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("Color string must not exceed 50 characters.");

        RuleFor(x => x.WipLimit)
            .GreaterThan(0).When(x => x.WipLimit.HasValue)
            .WithMessage("WIP Limit must be greater than zero.");
    }
}
