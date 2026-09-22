using FluentValidation;
using TaskFlow.Application.DTOs.Checklists;

namespace TaskFlow.Application.Validators.Checklists;

public class UpdateChecklistRequestValidator : AbstractValidator<UpdateChecklistRequest>
{
    public UpdateChecklistRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Checklist Title is required.")
            .MaximumLength(200).WithMessage("Checklist Title must not exceed 200 characters.");
    }
}
