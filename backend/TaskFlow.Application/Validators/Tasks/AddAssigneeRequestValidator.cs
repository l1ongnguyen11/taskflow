using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class AddAssigneeRequestValidator : AbstractValidator<AddAssigneeRequest>
{
    public AddAssigneeRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
    }
}
