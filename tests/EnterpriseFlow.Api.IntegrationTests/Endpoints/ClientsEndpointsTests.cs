using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Clients.GetClientById;
using EnterpriseFlow.Application.Features.Clients.GetClients;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Sprint 9: closes a coverage gap left by Sprint 7b — Clients' read endpoints
/// (<c>GetClientById</c>/<c>GetClients</c>) had zero test coverage even though the write side
/// (create/deactivate) was proven by <see cref="BusinessModulesEndpointsTests"/>.
/// </summary>
public sealed class ClientsEndpointsTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task Create_Then_Get_Returns_The_Client()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Clients.Read);

        var createResponse = await client.PostAsJsonAsync("/api/clients", new { name = "Acme Client", companyId = (Guid?)null });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<IdResponse>())!;

        var getResponse = await client.GetAsync($"/api/clients/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<ClientDto>();
        dto!.Name.Should().Be("Acme Client");
    }

    [Fact]
    public async Task Get_Unknown_Client_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Read);

        var response = await client.GetAsync($"/api/clients/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Only_Returns_Clients_From_The_Caller_Tenant()
    {
        var tenantA = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Clients.Read);
        await tenantA.PostAsJsonAsync("/api/clients", new { name = "Tenant A Client", companyId = (Guid?)null });

        var tenantB = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Clients.Manage, Permissions.Clients.Read);
        await tenantB.PostAsJsonAsync("/api/clients", new { name = "Tenant B Client", companyId = (Guid?)null });

        var listResponse = await tenantB.GetAsync("/api/clients");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var clients = await listResponse.Content.ReadFromJsonAsync<List<ClientListItemDto>>();
        clients.Should().ContainSingle(c => c.Name == "Tenant B Client");
        clients.Should().NotContain(c => c.Name == "Tenant A Client");
    }

    private sealed record IdResponse(Guid Id);
}
