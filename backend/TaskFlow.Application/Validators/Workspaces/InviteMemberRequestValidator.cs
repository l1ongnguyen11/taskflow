using FluentValidation;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Validators.Workspaces;

public class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Role)
            .Must(r => string.IsNullOrEmpty(r) || new[] { "admin", "member", "viewer" }.Contains(r.ToLower()))
            .WithMessage("Invalid role specified. Allowed roles are admin, member, viewer.");
    }
}
