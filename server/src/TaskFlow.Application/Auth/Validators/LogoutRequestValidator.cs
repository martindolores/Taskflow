using FluentValidation;
using TaskFlow.Application.Auth.Dtos;

namespace TaskFlow.Application.Auth.Validators;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
