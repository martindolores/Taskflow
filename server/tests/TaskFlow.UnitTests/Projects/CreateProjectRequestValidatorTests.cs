using TaskFlow.Application.Projects.Dtos;
using TaskFlow.Application.Projects.Validators;

namespace TaskFlow.UnitTests.Projects;

public class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new CreateProjectRequest("Marketing", "#FF5733");

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBlankName_Fails()
    {
        var request = new CreateProjectRequest("", "#FF5733");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithNameOverMaxLength_Fails()
    {
        var request = new CreateProjectRequest(new string('a', 101), "#FF5733");

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FF5733")]
    [InlineData("#FF573")]
    [InlineData("#GGGGGG")]
    [InlineData("red")]
    public void Validate_WithInvalidColor_Fails(string color)
    {
        var request = new CreateProjectRequest("Marketing", color);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
