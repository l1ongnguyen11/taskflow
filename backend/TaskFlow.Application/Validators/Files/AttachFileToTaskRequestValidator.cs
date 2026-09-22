using FluentValidation;
using TaskFlow.Application.DTOs.Files;

namespace TaskFlow.Application.Validators.Files;

public class AttachFileToTaskRequestValidator : AbstractValidator<AttachFileToTaskRequest>
{
    public AttachFileToTaskRequestValidator()
    {
        RuleFor(x => x.FileId)
            .NotEmpty().WithMessage("File ID is required.");
    }
}
