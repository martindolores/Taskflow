using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Organizations.Validators;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Organizations;

public class CreateInvitationRequestValidatorTests
{
    private readonly CreateInvitationRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new CreateInvitationRequest("newhire@acme.com", UserRole.Member);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBlankEmail_Fails()
    {
        var request = new CreateInvitationRequest("", UserRole.Member);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithMalformedEmail_Fails()
    {
        var request = new CreateInvitationRequest("not-an-email", UserRole.Member);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedRole_Fails()
    {
        var request = new CreateInvitationRequest("newhire@acme.com", (UserRole)999);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
