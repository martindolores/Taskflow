using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Application.Tasks.Validators;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Tasks;

public class UpdateTaskStatusRequestValidatorTests
{
    private readonly UpdateTaskStatusRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidStatus_Succeeds()
    {
        var request = new UpdateTaskStatusRequest(TaskItemStatus.Done);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedStatus_Fails()
    {
        var request = new UpdateTaskStatusRequest((TaskItemStatus)999);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
