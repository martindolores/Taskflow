using TaskFlow.Application.TaskComments.Dtos;
using TaskFlow.Application.TaskComments.Validators;

namespace TaskFlow.UnitTests.TaskComments;

public class CreateCommentRequestValidatorTests
{
    private readonly CreateCommentRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidBody_Succeeds()
    {
        var request = new CreateCommentRequest("Looks good to me.");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBlankBody_Fails()
    {
        var request = new CreateCommentRequest("");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
