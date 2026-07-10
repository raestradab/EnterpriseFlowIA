using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class ProjectTaskTests
{
    private static ProjectTask CreateTask() =>
        ProjectTask.Create("Design schema", null, TaskPriority.Medium, Guid.NewGuid(), null);

    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var task = CreateTask();

        task.Status.Should().Be(ProjectTaskStatus.Todo);
        task.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Title_Throws(string title)
    {
        var act = () => ProjectTask.Create(title, null, TaskPriority.Medium, Guid.NewGuid(), null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Without_ProjectId_Throws()
    {
        var act = () => ProjectTask.Create("Design schema", null, TaskPriority.Medium, Guid.Empty, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Trims_The_Description()
    {
        var task = ProjectTask.Create("Design schema", "  Do the thing  ", TaskPriority.Medium, Guid.NewGuid(), null);

        task.Description.Should().Be("Do the thing");
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var task = CreateTask();
        var tenantId = Guid.NewGuid();

        task.AssignTenant(tenantId);

        task.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Start_Sets_Status_To_InProgress()
    {
        var task = CreateTask();

        task.Start();

        task.Status.Should().Be(ProjectTaskStatus.InProgress);
        task.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var task = CreateTask();

        task.MarkDeleted();

        task.IsDeleted.Should().BeTrue();
        task.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AssignTo_Project_Member_Succeeds()
    {
        var task = CreateTask();
        var userId = Guid.NewGuid();

        task.AssignTo(userId, isProjectMember: true);

        task.AssignedToUserId.Should().Be(userId);
    }

    [Fact]
    public void AssignTo_Non_Member_Throws()
    {
        var task = CreateTask();

        var act = () => task.AssignTo(Guid.NewGuid(), isProjectMember: false);

        act.Should().Throw<TaskAssigneeMustBeProjectMemberException>();
        task.AssignedToUserId.Should().BeNull();
    }

    [Fact]
    public void Complete_Sets_Status_And_Closes_The_Task()
    {
        var task = CreateTask();

        task.Complete();

        task.Status.Should().Be(ProjectTaskStatus.Completed);
        task.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void Cancel_Sets_Status_And_Closes_The_Task()
    {
        var task = CreateTask();

        task.Cancel();

        task.Status.Should().Be(ProjectTaskStatus.Cancelled);
        task.IsOpen.Should().BeFalse();
    }
}
