using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class AddLabelToTaskRequestValidator : AbstractValidator<AddLabelToTaskRequest>
{
    public AddLabelToTaskRequestValidator()
    {
        RuleFor(x => x.LabelId)
            .NotEmpty().WithMessage("LabelId is required.");
    }
}
