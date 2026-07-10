using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using EnterpriseFlow.Application.Features.Projects.GetProjectById;
using EnterpriseFlow.Application.Features.Projects.GetProjects;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Sprint 9: closes a coverage gap left by Sprint 7b — Projects' read endpoints
/// (<c>GetProjectById</c>/<c>GetProjects</c>) and <c>RemoveProjectMember</c> had zero test
/// coverage. Cross-cutting invariants (HU-021 close validation, HU-023 assignment) stay in
/// <see cref="BusinessModulesEndpointsTests"/>; this file is just the plumbing those didn't touch.
/// </summary>
public sealed class ProjectsEndpointsTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task Create_Then_Get_Returns_The_Project()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Projects.Read);
        var clientId = await ReadIdAsync(await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null }));

        var createResponse = await client.PostAsJsonAsync(
            "/api/projects",
            new { name = "New Portal", clientId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var projectId = await ReadIdAsync(createResponse);

        var getResponse = await client.GetAsync($"/api/projects/{projectId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
        dto!.Name.Should().Be("New Portal");
        dto.Status.Should().Be(ProjectStatus.Planned);
        dto.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Unknown_Project_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Projects.Read);

        var response = await client.GetAsync($"/api/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Only_Returns_Projects_From_The_Caller_Tenant()
    {
        var tenantA = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Projects.Read);
        var clientAId = await ReadIdAsync(await tenantA.PostAsJsonAsync("/api/clients", new { name = "Client A", companyId = (Guid?)null }));
        await tenantA.PostAsJsonAsync(
            "/api/projects",
            new { name = "Tenant A Project", clientId = clientAId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null });

        var tenantB = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Projects.Manage, Permissions.Projects.Read);
        var clientBId = await ReadIdAsync(await tenantB.PostAsJsonAsync("/api/clients", new { name = "Client B", companyId = (Guid?)null }));
        await tenantB.PostAsJsonAsync(
            "/api/projects",
            new { name = "Tenant B Project", clientId = clientBId, startDate = (DateOnly?)null, estimatedEndDate = (DateOnly?)null });

        var listResponse = await tenantB.GetAsync("/api/projects");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projects = await listResponse.Content.ReadFromJsonAsync<List<ProjectListItemDto>>();
        projects.Should().ContainSingle(p => p.Name == "Tenant B Project");
        projects.Should().NotContain(p => p.Name == "Tenant A Project");
    }

    [Fact]
    public async Task RemoveProjectMember_Removes_A_Real_Member()
    {
        // AddProjectMember validates the user actually exists (see BusinessModulesEndpointsTests)
        // — needs a real registered admin, not a directly-minted token with no backing User row.
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

        var teamMember = registered.AdminUserId;
        (await client.PostAsJsonAsync($"/api/projects/{projectId}/members", new { userId = teamMember, role = ProjectRole.Developer }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var removeResponse = await client.DeleteAsync($"/api/projects/{projectId}/members/{teamMember}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/projects/{projectId}");
        var dto = await getResponse.Content.ReadFromJsonAsync<ProjectDto>();
        dto!.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveProjectMember_From_Unknown_Project_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Projects.Manage);

        var response = await client.DeleteAsync($"/api/projects/{Guid.NewGuid()}/members/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record IdResponse(Guid Id);
}
