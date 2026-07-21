using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Organizations.Validators;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Organizations;

public class UpdateMemberRoleRequestValidatorTests
{
    private readonly UpdateMemberRoleRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRole_Succeeds()
    {
        var request = new UpdateMemberRoleRequest(UserRole.Admin);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithUndefinedRole_Fails()
    {
        var request = new UpdateMemberRoleRequest((UserRole)999);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
