using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Validators;

namespace TaskFlow.UnitTests.Auth;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact]
    public void Validate_WithToken_Succeeds()
    {
        Assert.True(_validator.Validate(new RefreshTokenRequest("some-token")).IsValid);
    }

    [Fact]
    public void Validate_WithEmptyToken_Fails()
    {
        Assert.False(_validator.Validate(new RefreshTokenRequest("")).IsValid);
    }
}
