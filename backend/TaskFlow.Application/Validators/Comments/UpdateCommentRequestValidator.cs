using FluentValidation;
using TaskFlow.Application.DTOs.Comments;

namespace TaskFlow.Application.Validators.Comments;

public class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.");
    }
}
