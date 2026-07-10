using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 3, Sprint 7c (Backend), HU-092/HU-093. <c>GetMyOverdueTasksQuery</c> resolves "overdue"
/// for real (DueDate in the past, Status still open) — this proves the assistant's answer comes
/// from that real calculation, not the model eyeballing a raw task list, exactly what HU-092's
/// Gherkin requires. Uses the real register/login flow (not a directly-minted token) because
/// <c>AssignTask</c> validates the assignee actually exists — same reasoning as
/// <c>ProjectTasksEndpointsTests.Calendar_Returns_Only_The_Caller_Own_Assigned_Tasks_In_Range</c>.
/// </summary>
public sealed class AssistantOverdueTasksTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

    private async Task<(HttpClient Client, Guid AdminUserId)> RegisterAndLoginAsync()
    {
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
            "/api/auth/login", new { email = $"admin-{suffix}@acme.test", password = "SuperSecret123!" });
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResult>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return (client, registered.AdminUserId);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));
        return await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/projects", new { name = "New Portal", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null }));
    }

    [Fact]
    public async Task Asking_About_Overdue_Tasks_Answers_With_The_Real_Task_Title()
    {
        var (client, adminUserId) = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(client);
        await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { userId = adminUserId, role = ProjectRole.Developer });

        var overdueDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var taskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Entregar informe atrasado", description = (string?)null, priority = TaskPriority.High, projectId, dueDate = overdueDueDate }));
        (await client.PostAsJsonAsync($"/api/tasks/{taskId}/assign", new { userId = adminUserId })).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿Cuántas tareas tengo atrasadas?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("Entregar informe atrasado");
    }

    [Fact]
    public async Task A_Completed_Task_Past_Its_Due_Date_Is_Not_Reported_As_Overdue()
    {
        var (client, adminUserId) = await RegisterAndLoginAsync();
        var projectId = await CreateProjectAsync(client);
        await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { userId = adminUserId, role = ProjectRole.Developer });

        var pastDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        var taskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Tarea ya completada", description = (string?)null, priority = TaskPriority.Low, projectId, dueDate = pastDueDate }));
        (await client.PostAsJsonAsync($"/api/tasks/{taskId}/assign", new { userId = adminUserId })).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/tasks/{taskId}/complete", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿Cuántas tareas tengo atrasadas?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().NotContain("Tarea ya completada");
    }

    [Fact]
    public async Task Asking_For_A_Project_Summary_Answers_Using_Real_Project_Data_Not_A_Generic_Text()
    {
        // HU-093: no new mechanism — the model synthesizes a summary from the same real Query
        // results HU-092 already grounds every other answer in. This proves that already works,
        // it doesn't add anything new to the pipeline itself.
        var (client, _) = await RegisterAndLoginAsync();
        await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null });
        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Cliente Resumen", companyId = (Guid?)null }));
        await client.PostAsJsonAsync(
            "/api/projects", new { name = "Proyecto Para Resumen", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null });

        var response = await client.PostAsJsonAsync(
            "/api/assistant/messages", new { message = "Resumime el estado de mis proyectos activos." });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("Proyecto Para Resumen");
    }

    private sealed record ReplyResponse(string Reply);

    private sealed record IdResponse(Guid Id);
}
