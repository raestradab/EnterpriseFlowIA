using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Catalogs.GetCatalogItems;
using EnterpriseFlow.Application.Features.Catalogs.GetCatalogs;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 2, Sprint 4 (Validación de arquitectura) — Catálogos (F8.2) is the first real
/// consumer of <c>CachingBehavior</c>/<c>CacheInvalidationBehavior</c> (ADR-0012), the one
/// mechanism Sprint 3 wired but never exercised. The tests here are deliberately black-box
/// (pure HTTP, no reach into <c>IDistributedCache</c> internals): they prove the *observable
/// contract* of cache-aside — a read may be stale until something invalidates it, and a real
/// write always does — rather than coupling to the cache key format.
/// </summary>
public sealed class CatalogsEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task Create_Catalog_Then_Add_Item_Then_List_Returns_It()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage, Permissions.Catalogs.Read);

        var catalogId = await ReadIdAsync(await client.PostAsJsonAsync("/api/catalogs", new { name = "Document Categories" }));
        (await client.PostAsJsonAsync($"/api/catalogs/{catalogId}/items", new { key = "contract", label = "Contract" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var items = await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items");

        items.Should().ContainSingle(i => i.Key == "contract" && i.Label == "Contract");
    }

    [Fact]
    public async Task AddCatalogItem_Duplicate_Key_Returns_BadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage);
        var catalogId = await ReadIdAsync(await client.PostAsJsonAsync("/api/catalogs", new { name = "Document Categories" }));
        await client.PostAsJsonAsync($"/api/catalogs/{catalogId}/items", new { key = "contract", label = "Contract" });

        var response = await client.PostAsJsonAsync($"/api/catalogs/{catalogId}/items", new { key = "contract", label = "Contract (again)" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCatalogs_Lists_Only_The_Caller_Tenant_Catalogs()
    {
        var tenantA = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage, Permissions.Catalogs.Read);
        await tenantA.PostAsJsonAsync("/api/catalogs", new { name = "Tenant A Catalog" });

        var tenantB = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage, Permissions.Catalogs.Read);
        await tenantB.PostAsJsonAsync("/api/catalogs", new { name = "Tenant B Catalog" });

        var catalogs = await tenantB.GetFromJsonAsync<List<CatalogListItemDto>>("/api/catalogs");

        catalogs.Should().ContainSingle(c => c.Name == "Tenant B Catalog");
        catalogs.Should().NotContain(c => c.Name == "Tenant A Catalog");
    }

    [Fact]
    public async Task GetCatalogItems_Serves_A_Cached_Read_Until_A_Write_Invalidates_It()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage, Permissions.Catalogs.Read);
        var catalogId = await ReadIdAsync(await client.PostAsJsonAsync("/api/catalogs", new { name = "Document Categories" }));
        await client.PostAsJsonAsync($"/api/catalogs/{catalogId}/items", new { key = "contract", label = "Contract" });

        // Populates the cache (CachingBehavior, ADR-0012).
        var firstRead = await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items");
        var itemId = firstRead!.Single().Id;
        firstRead.Should().ContainSingle(i => i.Label == "Contract");

        // Bypasses the API/cache entirely — mutates the row directly. If CachingBehavior is
        // actually caching (not silently falling through to the DB on every read), the next
        // read must still show the OLD label: proof a cached response was served, not a fresh
        // DB read, without needing to inspect IDistributedCache directly.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var item = await db.Set<CatalogItem>().FirstAsync(i => i.Id == itemId);
            db.Entry(item).Property(nameof(CatalogItem.Label)).CurrentValue = "Mutated Directly In DB";
            await db.SaveChangesAsync();
        }

        var secondRead = await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items");
        secondRead.Should().ContainSingle(i => i.Label == "Contract", "the cached response should still be served");

        // A real write through the API invalidates the cache (CacheInvalidationBehavior).
        (await client.PutAsJsonAsync($"/api/catalogs/{catalogId}/items/{itemId}", new { label = "Signed Contract" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var thirdRead = await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items");
        thirdRead.Should().ContainSingle(i => i.Label == "Signed Contract", "the write must have invalidated the stale cache entry");
    }

    [Fact]
    public async Task RemoveCatalogItem_Also_Invalidates_The_Cache()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Catalogs.Manage, Permissions.Catalogs.Read);
        var catalogId = await ReadIdAsync(await client.PostAsJsonAsync("/api/catalogs", new { name = "Document Categories" }));
        await client.PostAsJsonAsync($"/api/catalogs/{catalogId}/items", new { key = "contract", label = "Contract" });
        var itemId = (await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items"))!.Single().Id;

        (await client.DeleteAsync($"/api/catalogs/{catalogId}/items/{itemId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var items = await client.GetFromJsonAsync<List<CatalogItemDto>>($"/api/catalogs/{catalogId}/items");
        items.Should().BeEmpty();
    }

    private sealed record IdResponse(Guid Id);
}
