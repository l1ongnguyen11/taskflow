using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class AddDependencyRequestValidator : AbstractValidator<AddDependencyRequest>
{
    public AddDependencyRequestValidator()
    {
        RuleFor(x => x.DependsOnId).NotEmpty().WithMessage("DependsOn Task ID is required.");
        
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Dependency Type is required.")
            .Must(t => new[] { "finish_to_start", "start_to_start", "finish_to_finish", "start_to_finish" }.Contains(t.Trim().ToLowerInvariant()))
            .WithMessage("Dependency Type must be one of: finish_to_start, start_to_start, finish_to_finish, start_to_finish.");
    }
}
