using FluentValidation;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Validators.Authorization;

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));
    }
}
