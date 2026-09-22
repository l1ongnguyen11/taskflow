using FluentValidation;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Validators.Authorization;

public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Key)
            .MaximumLength(100).WithMessage("Permission key must not exceed 100 characters.")
            .Matches(@"^[a-z][a-z0-9_]*:[a-z][a-z0-9_]*$")
            .WithMessage("Permission key must follow format 'resource:action'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Key));
    }
}
