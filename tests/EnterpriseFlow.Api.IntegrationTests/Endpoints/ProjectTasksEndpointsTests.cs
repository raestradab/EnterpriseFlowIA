using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using EnterpriseFlow.Application.Features.ProjectTasks.GetMyCalendar;
using EnterpriseFlow.Application.Features.ProjectTasks.GetTaskById;
using EnterpriseFlow.Application.Features.ProjectTasks.GetTasks;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Sprint 9: closes a coverage gap left by Sprint 7b — Tasks' read endpoints
/// (<c>GetTaskById</c>/<c>GetTasks</c>/<c>GetMyCalendar</c>, HU-024) and <c>CancelTask</c> had
/// zero test coverage. Cross-cutting invariants stay in
/// <see cref="BusinessModulesEndpointsTests"/>; this file is the plumbing those didn't touch.
/// </summary>
public sealed class ProjectTasksEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient(Guid tenantId, params string[] permissions)
    {
        var tokenService = factory.Services.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateAccessToken(Guid.NewGuid(), tenantId, permissions);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

    private static async Task<(Guid ProjectId, Guid ClientId)> CreateProjectAsync(HttpClient client)
    {
        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));
        var projectId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/projects",
            new { name = "New Portal", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null }));
        return (projectId, clientId);
    }

    [Fact]
    public async Task Create_Then_Get_Returns_The_Task()
    {
        var client = CreateAuthenticatedClient(
            Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Tasks.Manage, Permissions.Tasks.Read);
        var (projectId, _) = await CreateProjectAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Design schema", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = (DateOnly?)null });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = await ReadIdAsync(createResponse);

        var getResponse = await client.GetAsync($"/api/tasks/{taskId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Title.Should().Be("Design schema");
        dto.Status.Should().Be(ProjectTaskStatus.Todo);
    }

    [Fact]
    public async Task Get_Unknown_Task_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Tasks.Read);

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Filters_By_ProjectId_When_Given()
    {
        var client = CreateAuthenticatedClient(
            Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Tasks.Manage, Permissions.Tasks.Read);
        var (projectAId, _) = await CreateProjectAsync(client);
        var (projectBId, _) = await CreateProjectAsync(client);

        await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Project A Task", description = (string?)null, priority = TaskPriority.Low, projectId = projectAId, dueDate = (DateOnly?)null });
        await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Project B Task", description = (string?)null, priority = TaskPriority.Low, projectId = projectBId, dueDate = (DateOnly?)null });

        var filteredResponse = await client.GetAsync($"/api/tasks?projectId={projectAId}");

        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await filteredResponse.Content.ReadFromJsonAsync<List<TaskListItemDto>>();
        tasks.Should().ContainSingle(t => t.Title == "Project A Task");
        tasks.Should().NotContain(t => t.Title == "Project B Task");
    }

    [Fact]
    public async Task CancelTask_Marks_It_Cancelled()
    {
        var client = CreateAuthenticatedClient(
            Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Tasks.Manage, Permissions.Tasks.Read);
        var (projectId, _) = await CreateProjectAsync(client);
        var taskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Design schema", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = (DateOnly?)null }));

        var cancelResponse = await client.PostAsync($"/api/tasks/{taskId}/cancel", content: null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/tasks/{taskId}");
        var dto = await getResponse.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Status.Should().Be(ProjectTaskStatus.Cancelled);
    }

    [Fact]
    public async Task CancelTask_Unknown_Task_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Tasks.Manage);

        var response = await client.PostAsync($"/api/tasks/{Guid.NewGuid()}/cancel", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Calendar_Returns_Only_The_Caller_Own_Assigned_Tasks_In_Range()
    {
        // AddProjectMember/AssignTask both validate the user actually exists — needs a real
        // registered admin, not a directly-minted token with no backing User row (see
        // BusinessModulesEndpointsTests). The admin is also the "current user" the calendar
        // filters by, which is exactly what this test needs: assign a task to self.
        var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register-tenant", new
        {
            tenantName = "Acme Corp",
            tenantSlug = $"acme-{suffix}",
            adminEmail = $"admin-{suffix}@acme.test",
            adminPassword = "SuperSecret123!",
        });
        var registered = (await registerResponse.Content.ReadFromJsonAsync<RegisterTenantResult>())!;

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"admin-{suffix}@acme.test", password = "SuperSecret123!" });
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var (projectId, _) = await CreateProjectAsync(client);
        await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { userId = registered.AdminUserId, role = ProjectRole.Developer });

        var inRangeDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        var outOfRangeDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);

        var inRangeTaskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Due soon", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = inRangeDueDate }));
        var outOfRangeTaskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Due later", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = outOfRangeDueDate }));

        await client.PostAsJsonAsync($"/api/tasks/{inRangeTaskId}/assign", new { userId = registered.AdminUserId });
        await client.PostAsJsonAsync($"/api/tasks/{outOfRangeTaskId}/assign", new { userId = registered.AdminUserId });

        var from = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14).ToString("yyyy-MM-dd");
        var calendarResponse = await client.GetAsync($"/api/calendar?from={from}&to={to}");

        calendarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await calendarResponse.Content.ReadFromJsonAsync<List<CalendarItemDto>>();
        items.Should().ContainSingle(i => i.Title == "Due soon");
        items.Should().NotContain(i => i.Title == "Due later");
    }

    private sealed record IdResponse(Guid Id);
}
