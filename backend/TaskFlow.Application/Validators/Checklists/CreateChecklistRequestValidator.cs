using FluentValidation;
using TaskFlow.Application.DTOs.Checklists;

namespace TaskFlow.Application.Validators.Checklists;

public class CreateChecklistRequestValidator : AbstractValidator<CreateChecklistRequest>
{
    public CreateChecklistRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Checklist Title is required.")
            .MaximumLength(200).WithMessage("Checklist Title must not exceed 200 characters.");
    }
}
