using System.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public abstract class ModelTestBase(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    protected IModel Model { get; } = factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>().Model;

    protected static IIndex? FindIndex(IEntityType entityType, params string[] propertyNames) =>
        entityType.GetIndexes().SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual(propertyNames));
}

public class OrganizationConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void Organization_MapsToOrganizationsTable()
    {
        var entityType = Model.FindEntityType(typeof(Organization))!;

        Assert.Equal("organizations", entityType.GetTableName());
    }

    [Fact]
    public void Organization_SlugHasUniqueIndex()
    {
        var entityType = Model.FindEntityType(typeof(Organization))!;

        var index = FindIndex(entityType, nameof(Organization.Slug));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }
}

public class UserConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void User_MapsToUsersTable()
    {
        var entityType = Model.FindEntityType(typeof(User))!;

        Assert.Equal("users", entityType.GetTableName());
    }

    [Fact]
    public void User_EmailHasGlobalUniqueIndex()
    {
        var entityType = Model.FindEntityType(typeof(User))!;

        var index = FindIndex(entityType, nameof(User.Email));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void User_OrganizationIdIsIndexed()
    {
        var entityType = Model.FindEntityType(typeof(User))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Organization));

        Assert.Contains(foreignKey.Properties, p => p.Name == nameof(User.OrganizationId));
        Assert.Contains(entityType.GetIndexes(), i => i.Properties.SequenceEqual(foreignKey.Properties));
    }

    [Fact]
    public void User_RoleIsStoredAsString()
    {
        var entityType = Model.FindEntityType(typeof(User))!;
        var property = entityType.FindProperty(nameof(User.Role))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }

    [Fact]
    public void User_StatusIsStoredAsString()
    {
        var entityType = Model.FindEntityType(typeof(User))!;
        var property = entityType.FindProperty(nameof(User.Status))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }
}

public class TaskItemConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void TaskItem_MapsToTasksTable()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;

        Assert.Equal("tasks", entityType.GetTableName());
    }

    [Fact]
    public void TaskItem_StatusIsStoredAsStringWithToDoDefault()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;
        var property = entityType.FindProperty(nameof(TaskItem.Status))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
        Assert.Equal(TaskItemStatus.ToDo, property.GetDefaultValue());
    }

    [Fact]
    public void TaskItem_PriorityIsStoredAsStringWithMediumDefault()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;
        var property = entityType.FindProperty(nameof(TaskItem.Priority))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
        Assert.Equal(TaskPriority.Medium, property.GetDefaultValue());
    }

    [Fact]
    public void TaskItem_AssigneeIdIsNullable()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;
        var property = entityType.FindProperty(nameof(TaskItem.AssigneeId))!;

        Assert.True(property.IsNullable);
    }

    [Fact]
    public void TaskItem_HasCompositeIndexOnOrganizationAndStatus()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;

        var index = FindIndex(entityType, nameof(TaskItem.OrganizationId), nameof(TaskItem.Status));

        Assert.NotNull(index);
    }

    [Fact]
    public void TaskItem_HasCompositeIndexOnOrganizationAndAssignee()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;

        var index = FindIndex(entityType, nameof(TaskItem.OrganizationId), nameof(TaskItem.AssigneeId));

        Assert.NotNull(index);
    }

    [Fact]
    public void TaskItem_CreatedByDeletionIsRestricted()
    {
        var entityType = Model.FindEntityType(typeof(TaskItem))!;

        var foreignKey = entityType.GetForeignKeys()
            .Single(fk => fk.PrincipalEntityType.ClrType == typeof(User) && fk.Properties.Any(p => p.Name == nameof(TaskItem.CreatedById)));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}

public class TaskCommentConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void TaskComment_MapsToTaskCommentsTable()
    {
        var entityType = Model.FindEntityType(typeof(TaskComment))!;

        Assert.Equal("task_comments", entityType.GetTableName());
    }

    [Fact]
    public void TaskComment_DeletingParentTaskCascades()
    {
        var entityType = Model.FindEntityType(typeof(TaskComment))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(TaskItem));

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void TaskComment_DeletingAuthorIsRestricted()
    {
        var entityType = Model.FindEntityType(typeof(TaskComment))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}

public class InvitationConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void Invitation_MapsToInvitationsTable()
    {
        var entityType = Model.FindEntityType(typeof(Invitation))!;

        Assert.Equal("invitations", entityType.GetTableName());
    }

    [Fact]
    public void Invitation_TokenHasUniqueIndex()
    {
        var entityType = Model.FindEntityType(typeof(Invitation))!;

        var index = FindIndex(entityType, nameof(Invitation.Token));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void Invitation_StatusDefaultsToPending()
    {
        var entityType = Model.FindEntityType(typeof(Invitation))!;
        var property = entityType.FindProperty(nameof(Invitation.Status))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
        Assert.Equal(InvitationStatus.Pending, property.GetDefaultValue());
    }
}

public class ActivityLogEntryConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void ActivityLogEntry_MapsToActivityLogTable()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;

        Assert.Equal("activity_log", entityType.GetTableName());
    }

    [Fact]
    public void ActivityLogEntry_TypeIsStoredAsString()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;
        var property = entityType.FindProperty(nameof(ActivityLogEntry.Type))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }

    [Fact]
    public void ActivityLogEntry_TaskIdIsNullable()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;
        var property = entityType.FindProperty(nameof(ActivityLogEntry.TaskId))!;

        Assert.True(property.IsNullable);
    }

    [Fact]
    public void ActivityLogEntry_OrganizationIdIsIndexed()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Organization));

        Assert.Contains(foreignKey.Properties, p => p.Name == nameof(ActivityLogEntry.OrganizationId));
        Assert.Contains(entityType.GetIndexes(), i => i.Properties.SequenceEqual(foreignKey.Properties));
    }

    [Fact]
    public void ActivityLogEntry_DeletingTaskSetsNull()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(TaskItem));

        Assert.Equal(DeleteBehavior.SetNull, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void ActivityLogEntry_DeletingActorIsRestricted()
    {
        var entityType = Model.FindEntityType(typeof(ActivityLogEntry))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}

public class RefreshTokenConfigurationTests(WebApplicationFactory<Program> factory) : ModelTestBase(factory)
{
    [Fact]
    public void RefreshToken_MapsToRefreshTokensTable()
    {
        var entityType = Model.FindEntityType(typeof(RefreshToken))!;

        Assert.Equal("refresh_tokens", entityType.GetTableName());
    }

    [Fact]
    public void RefreshToken_HasNoOrganizationColumn()
    {
        var entityType = Model.FindEntityType(typeof(RefreshToken))!;

        Assert.Null(entityType.FindProperty("OrganizationId"));
    }

    [Fact]
    public void RefreshToken_DeletingUserCascades()
    {
        var entityType = Model.FindEntityType(typeof(RefreshToken))!;

        var foreignKey = entityType.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }
}
