using FluentValidation;
using TaskFlow.Application.DTOs.Boards;

namespace TaskFlow.Application.Validators.Boards;

public class CreateBoardRequestValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Board Name is required.")
            .MaximumLength(100).WithMessage("Board Name must not exceed 100 characters.");
    }
}
