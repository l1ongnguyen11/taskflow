using FluentValidation;
using TaskFlow.Application.DTOs.Labels;

namespace TaskFlow.Application.Validators.Labels;

public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required.")
            .MaximumLength(50).WithMessage("Label name must not exceed 50 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Label color is required.")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Label color must be a valid hex color code (e.g., #FF00FF).");
    }
}
