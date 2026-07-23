using FluentValidation;
using TaskFlow.Application.Projects.Dtos;

namespace TaskFlow.Application.Projects.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9A-Fa-f]{6}$");
        RuleFor(x => x.Description).MaximumLength(280);
    }
}
