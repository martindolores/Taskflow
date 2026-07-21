using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Validators;

namespace TaskFlow.UnitTests.Auth;

public class AcceptInvitationRequestValidatorTests
{
    private readonly AcceptInvitationRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new AcceptInvitationRequest("some-opaque-token", "correct-horse-battery", "Ada", "Lovelace");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "correct-horse-battery", "Ada", "Lovelace")]
    [InlineData("some-opaque-token", "short1", "Ada", "Lovelace")]
    [InlineData("some-opaque-token", "correct-horse-battery", "", "Lovelace")]
    [InlineData("some-opaque-token", "correct-horse-battery", "Ada", "")]
    public void Validate_WithInvalidRequest_Fails(string token, string password, string firstName, string lastName)
    {
        var request = new AcceptInvitationRequest(token, password, firstName, lastName);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
