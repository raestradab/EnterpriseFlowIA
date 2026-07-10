using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Companies.GetCompanies;
using EnterpriseFlow.Application.Features.Companies.GetCompanyById;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// End-to-end proof of concept for Sprint 4 (Companies), updated in Sprint 7a to authenticate
/// with real JWTs instead of the header stand-ins those services have since been replaced by.
/// Tokens are minted directly via <see cref="ITokenService"/> (resolved from the test host's
/// DI container) rather than through a full Register+Login round trip — that flow is proven
/// separately by IdentityEndpointsTests; this file stays focused on Companies' own
/// tenant-isolation and permission semantics (ADR-0003, ADR-0004).
/// </summary>
public sealed class CompaniesEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task Create_Then_Get_Returns_The_Company()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Manage, Permissions.Companies.Read);

        var createResponse = await client.PostAsJsonAsync(
            "/api/companies",
            new { name = "Acme Corp", taxId = "123-456" });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var getResponse = await client.GetAsync($"/api/companies/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var company = await getResponse.Content.ReadFromJsonAsync<CompanyDto>();
        company.Should().NotBeNull();
        company!.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task Get_From_A_Different_Tenant_Returns_NotFound()
    {
        var owner = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Manage);

        var createResponse = await owner.PostAsJsonAsync(
            "/api/companies",
            new { name = "Tenant A Company", taxId = (string?)null });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var otherTenant = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Read);

        var getResponse = await otherTenant.GetAsync($"/api/companies/{created!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Without_Manage_Permission_Returns_Forbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new { name = "Should Not Be Created", taxId = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_With_Empty_Name_Returns_BadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Manage);

        var response = await client.PostAsJsonAsync(
            "/api/companies",
            new { name = string.Empty, taxId = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_Only_Returns_Companies_From_The_Caller_Tenant()
    {
        var tenantA = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Manage, Permissions.Companies.Read);
        await tenantA.PostAsJsonAsync("/api/companies", new { name = "Tenant A Co", taxId = (string?)null });

        var tenantB = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Companies.Manage, Permissions.Companies.Read);
        await tenantB.PostAsJsonAsync("/api/companies", new { name = "Tenant B Co", taxId = (string?)null });

        var listResponse = await tenantB.GetAsync("/api/companies");

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var companies = await listResponse.Content.ReadFromJsonAsync<List<CompanyListItemDto>>();
        companies.Should().ContainSingle(c => c.Name == "Tenant B Co");
        companies.Should().NotContain(c => c.Name == "Tenant A Co");
    }

    [Fact]
    public async Task Anonymous_Request_Returns_Unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/companies/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CreatedResponse(Guid Id);
}
