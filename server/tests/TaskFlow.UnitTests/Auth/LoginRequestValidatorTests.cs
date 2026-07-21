using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Validators;

namespace TaskFlow.UnitTests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var result = _validator.Validate(new LoginRequest("user@acme.com", "some-password"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "some-password")]
    [InlineData("not-an-email", "some-password")]
    [InlineData("user@acme.com", "")]
    public void Validate_WithInvalidRequest_Fails(string email, string password)
    {
        var result = _validator.Validate(new LoginRequest(email, password));

        Assert.False(result.IsValid);
    }
}
