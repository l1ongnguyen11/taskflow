using FluentValidation;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Validators.Workspaces;

public class UpdateWorkspaceRequestValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100).WithMessage("Workspace name must not exceed 100 characters.");
    }
}
