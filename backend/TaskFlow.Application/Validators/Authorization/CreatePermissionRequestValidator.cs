using FluentValidation;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Validators.Authorization;

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Permission key is required.")
            .MaximumLength(100).WithMessage("Permission key must not exceed 100 characters.")
            .Matches(@"^[a-z][a-z0-9_]*:[a-z][a-z0-9_]*$")
            .WithMessage("Permission key must follow format 'resource:action' (e.g. task:create).");
    }
}
