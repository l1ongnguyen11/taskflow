using FluentValidation;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Validators.Tasks;

public class UpdateTaskDueDateRequestValidator : AbstractValidator<UpdateTaskDueDateRequest>
{
    public UpdateTaskDueDateRequestValidator()
    {
        // DueDate is optional (nullable)
    }
}
