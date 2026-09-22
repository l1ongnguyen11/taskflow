using FluentValidation;
using TaskFlow.Application.DTOs.Checklists;

namespace TaskFlow.Application.Validators.Checklists;

public class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Item Content is required.")
            .MaximumLength(500).WithMessage("Item Content must not exceed 500 characters.");
    }
}
