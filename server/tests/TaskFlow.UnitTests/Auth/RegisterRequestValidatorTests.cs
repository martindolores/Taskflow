using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Validators;

namespace TaskFlow.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new RegisterRequest("Acme Inc", "founder@acme.com", "correct-horse-battery", "Ada", "Lovelace");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "founder@acme.com", "correct-horse-battery", "Ada", "Lovelace")]
    [InlineData("Acme Inc", "not-an-email", "correct-horse-battery", "Ada", "Lovelace")]
    [InlineData("Acme Inc", "founder@acme.com", "short1", "Ada", "Lovelace")]
    [InlineData("Acme Inc", "founder@acme.com", "correct-horse-battery", "", "Lovelace")]
    [InlineData("Acme Inc", "founder@acme.com", "correct-horse-battery", "Ada", "")]
    public void Validate_WithInvalidRequest_Fails(
        string organizationName, string email, string password, string firstName, string lastName)
    {
        var request = new RegisterRequest(organizationName, email, password, firstName, lastName);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
