using FluentValidation;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Validators.Workspaces;

public class CreateWorkspaceRequestValidator : AbstractValidator<CreateWorkspaceRequest>
{
    public CreateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100).WithMessage("Workspace name must not exceed 100 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Workspace slug is required.")
            .MaximumLength(100).WithMessage("Workspace slug must not exceed 100 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Workspace slug must contain only lowercase letters, numbers, and hyphens.");
    }
}
