using FluentValidation;
using TaskFlow.Application.TaskComments.Dtos;

namespace TaskFlow.Application.TaskComments.Validators;

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty();
    }
}
