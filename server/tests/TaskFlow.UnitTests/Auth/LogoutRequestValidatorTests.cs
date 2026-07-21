using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Validators;

namespace TaskFlow.UnitTests.Auth;

public class LogoutRequestValidatorTests
{
    private readonly LogoutRequestValidator _validator = new();

    [Fact]
    public void Validate_WithToken_Succeeds()
    {
        Assert.True(_validator.Validate(new LogoutRequest("some-token")).IsValid);
    }

    [Fact]
    public void Validate_WithEmptyToken_Fails()
    {
        Assert.False(_validator.Validate(new LogoutRequest("")).IsValid);
    }
}
