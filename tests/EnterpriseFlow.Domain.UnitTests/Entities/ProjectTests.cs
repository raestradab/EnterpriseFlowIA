using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Events;
using EnterpriseFlow.Domain.Exceptions;
using FluentAssertions;

namespace EnterpriseFlow.Domain.UnitTests.Entities;

public class ProjectTests
{
    private static Project CreateProject() =>
        Project.Create("New Portal", Guid.NewGuid(), null, null);

    [Fact]
    public void Create_With_Valid_Data_Succeeds()
    {
        var project = CreateProject();

        project.Status.Should().Be(ProjectStatus.Planned);
        project.Members.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Missing_Name_Throws(string name)
    {
        var act = () => Project.Create(name, Guid.NewGuid(), null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Without_ClientId_Throws()
    {
        var act = () => Project.Create("New Portal", Guid.Empty, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignTenant_Sets_The_TenantId()
    {
        var project = CreateProject();
        var tenantId = Guid.NewGuid();

        project.AssignTenant(tenantId);

        project.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void MarkDeleted_Sets_IsDeleted_And_DeletedAtUtc()
    {
        var project = CreateProject();

        project.MarkDeleted();

        project.IsDeleted.Should().BeTrue();
        project.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Create_With_EstimatedEndDate_Before_StartDate_Throws()
    {
        var start = new DateOnly(2026, 6, 1);
        var end = new DateOnly(2026, 5, 1);

        var act = () => Project.Create("New Portal", Guid.NewGuid(), start, end);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Close_With_No_Open_Tasks_Succeeds_And_Raises_Event()
    {
        var project = CreateProject();

        project.Close(hasOpenTasks: false);

        project.Status.Should().Be(ProjectStatus.Closed);
        project.DomainEvents.Should().ContainSingle(e => e is ProjectClosedDomainEvent);
    }

    [Fact]
    public void Close_With_Open_Tasks_Throws_And_Does_Not_Change_Status()
    {
        var project = CreateProject();

        var act = () => project.Close(hasOpenTasks: true);

        act.Should().Throw<ProjectHasOpenTasksException>();
        project.Status.Should().Be(ProjectStatus.Planned);
    }

    [Fact]
    public void AddMember_Adds_A_New_Team_Member()
    {
        var project = CreateProject();
        var userId = Guid.NewGuid();

        project.AddMember(userId, ProjectRole.Developer);

        project.Members.Should().ContainSingle(m => m.UserId == userId && m.Role == ProjectRole.Developer);
        project.IsMember(userId).Should().BeTrue();
    }

    [Fact]
    public void AddMember_Same_User_Twice_Throws()
    {
        var project = CreateProject();
        var userId = Guid.NewGuid();
        project.AddMember(userId, ProjectRole.Developer);

        var act = () => project.AddMember(userId, ProjectRole.QaEngineer);

        act.Should().Throw<ProjectMemberAlreadyExistsException>();
    }

    [Fact]
    public void RemoveMember_Removes_The_User()
    {
        var project = CreateProject();
        var userId = Guid.NewGuid();
        project.AddMember(userId, ProjectRole.Developer);

        project.RemoveMember(userId);

        project.IsMember(userId).Should().BeFalse();
    }
}
