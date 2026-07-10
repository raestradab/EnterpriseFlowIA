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
/// Release 3, Sprint 7b (Backend — RAG), F10/HU-100/HU-101. Indexing runs synchronously as part
/// of the real <c>POST /api/documents</c> request (a domain event dispatched after SaveChanges,
/// same request/response cycle — see IndexDocumentOnUploadHandler) — no polling needed here.
/// <c>FakeEmbeddingClient</c> replaces the real OpenAI embeddings client (no API keys in this
/// environment) with a tiny fixed-vocabulary vector, but the whole pipeline it exercises —
/// extraction, chunking, embedding, persistence, cosine-similarity retrieval, tenant isolation —
/// is real, not mocked.
/// </summary>
public sealed class AssistantRagTests(CustomWebApplicationFactory factory)
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

    private static async Task<(Guid WorkflowId, Guid DraftStateId)> SeedWorkflowAsync(HttpClient client)
    {
        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Document Approval" }));
        var draftId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Borrador", isInitial = true, isFinal = false }));

        return (workflowId, draftId);
    }

    private static async Task<Guid> UploadTextDocumentAsync(HttpClient client, Guid ownerId, Guid workflowDefinitionId, string content)
    {
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "contrato.txt" },
            { new StringContent(DocumentOwnerType.Project.ToString()), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent(workflowDefinitionId.ToString()), "workflowDefinitionId" },
        };

        return await ReadIdAsync(await client.PostAsync("/api/documents", form));
    }

    [Fact]
    public async Task Uploading_A_Text_Document_Indexes_It_Synchronously()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _) = await SeedWorkflowAsync(client);

        var documentId = await UploadTextDocumentAsync(
            client, ownerId, workflowId, "El plazo del contrato es de 12 meses, con renovacion automatica.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // No HttpContext in this manually created scope, so ICurrentTenantService has no tenant
        // to scope the global query filter (ADR-0003) to — IgnoreQueryFilters() reads the raw
        // persisted state directly, same technique other tests use when inspecting the database
        // outside of an authenticated request.
        var chunks = await db.DocumentChunks.IgnoreQueryFilters().Where(c => c.DocumentId == documentId).ToListAsync();

        chunks.Should().ContainSingle();
        chunks[0].Content.Should().Contain("renovacion automatica");
    }

    [Fact]
    public async Task Uploading_An_Unsupported_File_Type_Uploads_Successfully_But_Creates_No_Chunks()
    {
        // HU-100: an unsupported extension (no text extractor for it) is not an upload failure —
        // the Document is saved either way, it just doesn't participate in RAG. A valid PNG magic
        // byte header is required for the upload itself to pass FileSignatureValidator (Release 2).
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _) = await SeedWorkflowAsync(client);

        byte[] pngMagicBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        var fileContent = new ByteArrayContent(pngMagicBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "captura.png" },
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

    [Fact]
    public async Task Asking_About_Document_Content_Answers_With_Text_Grounded_In_The_Real_Chunk()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Read);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _) = await SeedWorkflowAsync(client);
        await UploadTextDocumentAsync(client, ownerId, workflowId, "El plazo del contrato es de 12 meses, con renovacion automatica.");

        var response = await client.PostAsJsonAsync("/api/assistant/messages", new { message = "¿Cuál es el plazo del contrato?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("renovacion automatica");
    }

    [Fact]
    public async Task A_Tenants_Document_Content_Never_Leaks_Into_Another_Tenants_Search()
    {
        var tenantA = Guid.NewGuid();
        var clientA = CreateAuthenticatedClient(tenantA, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerA = await SeedProjectOwnerAsync(tenantA);
        var (workflowA, _) = await SeedWorkflowAsync(clientA);
        await UploadTextDocumentAsync(clientA, ownerA, workflowA, "El contrato secreto del tenant A tiene un plazo de 12 meses.");

        var tenantB = Guid.NewGuid();
        var clientB = CreateAuthenticatedClient(tenantB, Permissions.Documents.Read);

        var response = await clientB.PostAsJsonAsync("/api/assistant/messages", new { message = "¿Cuál es el plazo del contrato?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().NotContain("contrato secreto del tenant A");
    }

    [Fact]
    public async Task Asking_About_Document_Content_Without_Permission_Gets_A_Graceful_Denial_Not_A_Crash()
    {
        var tenantId = Guid.NewGuid();
        var uploaderClient = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _) = await SeedWorkflowAsync(uploaderClient);
        await UploadTextDocumentAsync(uploaderClient, ownerId, workflowId, "El plazo del contrato es de 12 meses, con renovacion automatica.");

        var askerClient = CreateAuthenticatedClient(tenantId); // no Permissions.Documents.Read

        var response = await askerClient.PostAsJsonAsync("/api/assistant/messages", new { message = "¿Cuál es el plazo del contrato?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ReplyResponse>();
        body!.Reply.Should().Contain("no ten").And.NotContain("renovacion automatica");
    }

    private sealed record ReplyResponse(string Reply);

    private sealed record IdResponse(Guid Id);
}
