using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Application.Tasks.Validators;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Tasks;

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Succeeds()
    {
        var request = new UpdateTaskRequest("Write docs", null, TaskItemStatus.InProgress, TaskPriority.High, null, null);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithBlankTitle_Fails()
    {
        var request = new UpdateTaskRequest("", null, TaskItemStatus.InProgress, TaskPriority.High, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedStatus_Fails()
    {
        var request = new UpdateTaskRequest("Write docs", null, (TaskItemStatus)999, TaskPriority.High, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedPriority_Fails()
    {
        var request = new UpdateTaskRequest("Write docs", null, TaskItemStatus.InProgress, (TaskPriority)999, null, null);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
