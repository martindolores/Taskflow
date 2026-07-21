using FluentValidation;
using TaskFlow.Application.Organizations.Dtos;

namespace TaskFlow.Application.Organizations.Validators;

public sealed class UpdateMemberRoleRequestValidator : AbstractValidator<UpdateMemberRoleRequest>
{
    public UpdateMemberRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
