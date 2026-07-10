using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Assistant.GetAssistantMessages;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 3, Sprint 4 (Validación de arquitectura) — first real vertical slice through the AI
/// skeleton from Sprint 3 (ADR-0013). <c>FakeAiChatClient</c> replaces the real OpenAI/Anthropic
/// clients (no API keys in this environment, r3-01-vision-y-alcance.md sección 0), but the
/// tool-use loop itself, the persistence, and — most importantly — the tenant/permission
/// boundary around what the model can see are all real, not mocked.
/// </summary>
public sealed class AssistantEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task SeedProjectAsync(Guid tenantId, string projectName)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = Client.Create("Test Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);

        var project = Project.Create(projectName, client.Id, null, null);
        project.AssignTenant(tenantId);
        db.Projects.Add(project);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Asking_About_Projects_Answers_With_Real_Tenant_Data()
    {
        var tenantId = Guid.NewGuid();
        await SeedProjectAsync(tenantId, "Proyecto Real Anclado");
        var client = CreateAuthenticatedClient(tenantId, Permissions.Projects.Read);

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿qué proyectos tengo?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("Proyecto Real Anclado");
    }

    [Fact]
    public async Task Conversation_Persists_Both_The_Users_Message_And_The_Assistants_Reply()
    {
        var tenantId = Guid.NewGuid();
        await SeedProjectAsync(tenantId, "Proyecto Con Historial");
        var client = CreateAuthenticatedClient(tenantId, Permissions.Projects.Read);

        await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿qué proyectos tengo?" });

        var history = await client.GetFromJsonAsync<List<AssistantMessageDto>>("/api/assistant/messages");

        history.Should().HaveCount(2);
        history![0].Role.Should().Be(AssistantMessageRole.User);
        history[0].Content.Should().Be("¿qué proyectos tengo?");
        history[1].Role.Should().Be(AssistantMessageRole.Assistant);
        history[1].Content.Should().Contain("Proyecto Con Historial");
    }

    [Fact]
    public async Task Asking_Without_Projects_Read_Permission_Gets_A_Graceful_Denial_Not_A_Crash()
    {
        var tenantId = Guid.NewGuid();
        await SeedProjectAsync(tenantId, "Proyecto Restringido");
        var client = CreateAuthenticatedClient(tenantId); // no Permissions.Projects.Read

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿qué proyectos tengo?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("no ten").And.NotContain("Proyecto Restringido");
    }

    [Fact]
    public async Task A_Tenants_Projects_Never_Leak_Into_Another_Tenants_Assistant_Answer()
    {
        var tenantA = Guid.NewGuid();
        await SeedProjectAsync(tenantA, "Proyecto Secreto De Otro Tenant");

        var tenantB = Guid.NewGuid();
        var clientB = CreateAuthenticatedClient(tenantB, Permissions.Projects.Read);

        var response = await clientB.PostAsJsonAsync("/api/assistant/messages", new { message = "¿qué proyectos tengo?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().NotContain("Proyecto Secreto De Otro Tenant");
    }

    private sealed record ReplyResponse(string Reply);
}
