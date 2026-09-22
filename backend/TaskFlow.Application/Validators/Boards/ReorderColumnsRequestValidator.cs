using FluentValidation;
using TaskFlow.Application.DTOs.Boards;

namespace TaskFlow.Application.Validators.Boards;

public class ReorderColumnsRequestValidator : AbstractValidator<ReorderColumnsRequest>
{
    public ReorderColumnsRequestValidator()
    {
        RuleFor(x => x.Columns)
            .NotEmpty().WithMessage("Columns list must not be empty.");

        RuleForEach(x => x.Columns).ChildRules(item =>
        {
            item.RuleFor(i => i.ColumnId).NotEmpty().WithMessage("Column ID is required.");
        });
    }
}
