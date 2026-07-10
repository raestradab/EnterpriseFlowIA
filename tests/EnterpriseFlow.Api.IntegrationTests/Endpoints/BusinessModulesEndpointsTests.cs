using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// End-to-end proof for Sprint 7b: the real business invariants (HU-012 cascade, HU-021 close
/// validation, HU-023 assignment restricted to team members) exercised through HTTP with a
/// real JWT, not just at the domain-unit-test level — the same "verify behavior, not just
/// status codes" discipline that caught real bugs in Sprints 4 and 7a.
/// </summary>
public sealed class BusinessModulesEndpointsTests(CustomWebApplicationFactory factory)
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

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task Deactivating_A_Client_Cascades_To_Its_Contacts()
    {
        var client = CreateAuthenticatedClient(
            Guid.NewGuid(),
            Permissions.Clients.Manage,
            Permissions.Clients.Read,
            Permissions.Contacts.Manage,
            Permissions.Contacts.Read);

        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));
        var contactId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/contacts",
            new { name = "Jane Doe", email = (string?)null, phone = (string?)null, clientId }));

        (await client.GetAsync($"/api/contacts/{contactId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var deactivateResponse = await client.PostAsync($"/api/clients/{clientId}/deactivate", content: null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getContactAfterCascade = await client.GetAsync($"/api/contacts/{contactId}");
        getContactAfterCascade.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Project_Cannot_Close_With_Open_Tasks_But_Can_After_Completing_Them()
    {
        var client = CreateAuthenticatedClient(
            Guid.NewGuid(),
            Permissions.Clients.Manage,
            Permissions.Projects.Manage,
            Permissions.Projects.Read,
            Permissions.Tasks.Manage);

        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));
        var projectId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/projects",
            new { name = "New Portal", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null }));
        var taskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Design schema", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = (DateOnly?)null }));

        var firstCloseAttempt = await client.PostAsync($"/api/projects/{projectId}/close", content: null);
        firstCloseAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await client.PostAsync($"/api/tasks/{taskId}/complete", content: null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondCloseAttempt = await client.PostAsync($"/api/projects/{projectId}/close", content: null);
        secondCloseAttempt.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Task_Assignment_Requires_Project_Membership()
    {
        // AddProjectMember validates the user actually exists (CreateProjectValidator's sibling
        // check) — that lookup is tenant-filtered, so it must be a real User in the same
        // tenant as the Project. A directly-minted token (as the other two tests use) has no
        // backing User row, so this test registers a real tenant/admin via HTTP instead and
        // reuses the admin as the "team member" being added.
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

        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));
        var projectId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/projects",
            new { name = "New Portal", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null }));
        var taskId = await ReadIdAsync(await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Design schema", description = (string?)null, priority = TaskPriority.Medium, projectId, dueDate = (DateOnly?)null }));

        var outsider = Guid.NewGuid();
        var assignToOutsiderResponse = await client.PostAsJsonAsync($"/api/tasks/{taskId}/assign", new { userId = outsider });
        assignToOutsiderResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var teamMember = registered.AdminUserId;
        var addMemberResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { userId = teamMember, role = ProjectRole.Developer });
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var assignToMemberResponse = await client.PostAsJsonAsync($"/api/tasks/{taskId}/assign", new { userId = teamMember });
        assignToMemberResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record IdResponse(Guid Id);
}
