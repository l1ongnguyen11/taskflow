using FluentValidation;
using TaskFlow.Application.DTOs.Comments;

namespace TaskFlow.Application.Validators.Comments;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.");
    }
}
