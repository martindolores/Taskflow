using FluentValidation;
using TaskFlow.Application.Tasks.Dtos;

namespace TaskFlow.Application.Tasks.Validators;

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
