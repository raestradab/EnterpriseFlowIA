using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Sprint 9 (Pruebas), Release 3. Every other assistant/RAG test replaces <c>IAiChatClient</c>/
/// <c>IEmbeddingClient</c> with fakes that always answer — this file is the one place the real
/// <c>NullAiChatClient</c>/<c>NullEmbeddingClient</c> graceful-degradation path (Sprint 3, ADR-0013)
/// actually runs end to end through real HTTP requests: a deployment with no AI provider
/// configured at all must not crash on chat or on Document upload.
/// </summary>
public sealed class AssistantNullAiTests(NullAiWebApplicationFactory factory) : IClassFixture<NullAiWebApplicationFactory>
{
    private HttpClient CreateAuthenticatedClient(Guid tenantId, params string[] permissions)
    {
        var tokenService = factory.Services.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateAccessToken(Guid.NewGuid(), tenantId, permissions);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<Guid> SeedProjectOwnerAsync(Guid tenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = Client.Create("Test Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);

        var project = Project.Create("Test Project", client.Id, null, null);
        project.AssignTenant(tenantId);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        return project.Id;
    }

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

    private static async Task<Guid> SeedWorkflowAsync(HttpClient client)
    {
        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Document Approval" }));
        await client.PostAsJsonAsync($"/api/workflows/{workflowId}/states", new { name = "Borrador", isInitial = true, isFinal = false });
        return workflowId;
    }

    [Fact]
    public async Task Asking_The_Assistant_With_No_Provider_Configured_Returns_The_Real_Null_Client_Message()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId);

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "Hola" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Be("El asistente de IA no está configurado en este entorno.");
    }

    [Fact]
    public async Task Uploading_A_Document_With_No_Embedding_Provider_Configured_Skips_Indexing_Without_Failing_The_Upload()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var workflowId = await SeedWorkflowAsync(client);

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Contenido de prueba."));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "nota.txt" },
            { new StringContent(DocumentOwnerType.Project.ToString()), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent(workflowId.ToString()), "workflowDefinitionId" },
        };

        var uploadResponse = await client.PostAsync("/api/documents", form);
        uploadResponse.EnsureSuccessStatusCode();
        var documentId = await ReadIdAsync(uploadResponse);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var chunks = await db.DocumentChunks.IgnoreQueryFilters().Where(c => c.DocumentId == documentId).ToListAsync();

        chunks.Should().BeEmpty();
    }

    private sealed record ReplyResponse(string Reply);

    private sealed record IdResponse(Guid Id);
}
