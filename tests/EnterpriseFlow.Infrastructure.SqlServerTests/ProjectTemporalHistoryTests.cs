using EnterpriseFlow.Application.Features.Projects.GetProjectHistory;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Infrastructure.SqlServerTests;

/// <summary>
/// Automates exactly what r4-04-validacion.md verified by hand against LocalDB: a Project's
/// status before/after <see cref="Project.Close"/>, tenant isolation on top of
/// <c>TemporalAsOf</c>, and a lookup before the row ever existed.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "RequiresSqlServer")]
public sealed class ProjectTemporalHistoryTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task History_Reflects_The_Status_At_The_Requested_Point_In_Time()
    {
        var tenantId = Guid.NewGuid();
        var beforeCreation = DateTimeOffset.UtcNow;
        await Task.Delay(50);

        await using var writeContext = fixture.CreateDbContext(tenantId);
        var client = Client.Create("Acme Corp", companyId: null);
        writeContext.Clients.Add(client);
        var project = Project.Create("Portal migration", client.Id, startDate: null, estimatedEndDate: null);
        writeContext.Projects.Add(project);
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var afterCreationBeforeClose = DateTimeOffset.UtcNow;
        await Task.Delay(50);

        writeContext.ChangeTracker.Clear();
        var toClose = await writeContext.Projects.SingleAsync(p => p.Id == project.Id);
        toClose.Close(hasOpenTasks: false);
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var afterClose = DateTimeOffset.UtcNow;

        await using var readContext = fixture.CreateDbContext(tenantId);
        var handler = new GetProjectHistoryQueryHandler(readContext);

        var beforeCreationResult = await handler.Handle(
            new GetProjectHistoryQuery(project.Id, beforeCreation), CancellationToken.None);
        var plannedResult = await handler.Handle(
            new GetProjectHistoryQuery(project.Id, afterCreationBeforeClose), CancellationToken.None);
        var closedResult = await handler.Handle(
            new GetProjectHistoryQuery(project.Id, afterClose), CancellationToken.None);

        beforeCreationResult.Should().BeNull("the Project did not exist yet at that point in time");
        plannedResult.Should().NotBeNull();
        plannedResult!.Status.Should().Be(ProjectStatus.Planned);
        closedResult.Should().NotBeNull();
        closedResult!.Status.Should().Be(ProjectStatus.Closed);
    }

    [Fact]
    public async Task History_Is_Isolated_Per_Tenant_Even_Under_TemporalAsOf()
    {
        var ownerTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        await using var writeContext = fixture.CreateDbContext(ownerTenantId);
        var client = Client.Create("Globex", companyId: null);
        writeContext.Clients.Add(client);
        var project = Project.Create("Isolated project", client.Id, startDate: null, estimatedEndDate: null);
        writeContext.Projects.Add(project);
        await writeContext.SaveChangesAsync();

        await Task.Delay(50);
        var asOf = DateTimeOffset.UtcNow;

        await using var otherTenantContext = fixture.CreateDbContext(otherTenantId);
        var handler = new GetProjectHistoryQueryHandler(otherTenantContext);

        var result = await handler.Handle(new GetProjectHistoryQuery(project.Id, asOf), CancellationToken.None);

        result.Should().BeNull("the global tenant query filter (ADR-0003) must still apply under TemporalAsOf");
    }
}
