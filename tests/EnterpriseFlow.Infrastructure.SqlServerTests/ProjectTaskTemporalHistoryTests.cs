using EnterpriseFlow.Application.Features.ProjectTasks.GetTaskHistory;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Infrastructure.SqlServerTests;

/// <summary>
/// Same coverage as <see cref="ProjectTemporalHistoryTests"/>, for the second entity HU-102
/// names (r4-07-backend.md) — a Task's status before/after <see cref="ProjectTask.Cancel"/>.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "RequiresSqlServer")]
public sealed class ProjectTaskTemporalHistoryTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task History_Reflects_The_Status_At_The_Requested_Point_In_Time()
    {
        var tenantId = Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;
        await Task.Delay(50);

        await using var writeContext = fixture.CreateDbContext(tenantId);
        var client = Client.Create("Initech", companyId: null);
        writeContext.Clients.Add(client);
        var project = Project.Create("Billing revamp", client.Id, startDate: null, estimatedEndDate: null);
        writeContext.Projects.Add(project);
        var task = ProjectTask.Create("Migrate invoices", description: null, TaskPriority.Medium, project.Id, dueDate: null);
        writeContext.ProjectTasks.Add(task);
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var afterCreationBeforeCancel = DateTimeOffset.UtcNow;
        await Task.Delay(50);

        writeContext.ChangeTracker.Clear();
        var toCancel = await writeContext.ProjectTasks.SingleAsync(t => t.Id == task.Id);
        toCancel.Cancel();
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var afterCancel = DateTimeOffset.UtcNow;

        await using var readContext = fixture.CreateDbContext(tenantId);
        var handler = new GetTaskHistoryQueryHandler(readContext);

        var beforeCreationResult = await handler.Handle(
            new GetTaskHistoryQuery(task.Id, beforeCreation), CancellationToken.None);
        var todoResult = await handler.Handle(
            new GetTaskHistoryQuery(task.Id, afterCreationBeforeCancel), CancellationToken.None);
        var cancelledResult = await handler.Handle(
            new GetTaskHistoryQuery(task.Id, afterCancel), CancellationToken.None);

        beforeCreationResult.Should().BeNull("the Task did not exist yet at that point in time");
        todoResult.Should().NotBeNull();
        todoResult!.Status.Should().Be(ProjectTaskStatus.Todo);
        cancelledResult.Should().NotBeNull();
        cancelledResult!.Status.Should().Be(ProjectTaskStatus.Cancelled);
    }

    [Fact]
    public async Task History_Is_Isolated_Per_Tenant_Even_Under_TemporalAsOf()
    {
        var ownerTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        await using var writeContext = fixture.CreateDbContext(ownerTenantId);
        var client = Client.Create("Umbrella", companyId: null);
        writeContext.Clients.Add(client);
        var project = Project.Create("Isolated project", client.Id, startDate: null, estimatedEndDate: null);
        writeContext.Projects.Add(project);
        var task = ProjectTask.Create("Isolated task", description: null, TaskPriority.Low, project.Id, dueDate: null);
        writeContext.ProjectTasks.Add(task);
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var asOf = DateTimeOffset.UtcNow;

        await using var otherTenantContext = fixture.CreateDbContext(otherTenantId);
        var handler = new GetTaskHistoryQueryHandler(otherTenantContext);

        var result = await handler.Handle(new GetTaskHistoryQuery(task.Id, asOf), CancellationToken.None);

        result.Should().BeNull("the global tenant query filter (ADR-0003) must still apply under TemporalAsOf");
    }
}
