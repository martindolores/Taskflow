using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Application.Tasks.Validators;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Tasks;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new CreateTaskRequest("Write docs", "Body", TaskPriority.Medium, null, null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBlankTitle_Fails()
    {
        var request = new CreateTaskRequest("", null, TaskPriority.Medium, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithTitleOverMaxLength_Fails()
    {
        var request = new CreateTaskRequest(new string('a', 201), null, TaskPriority.Medium, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedPriority_Fails()
    {
        var request = new CreateTaskRequest("Write docs", null, (TaskPriority)999, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
