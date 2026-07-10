using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Documents.GetDocumentById;
using EnterpriseFlow.Application.Features.Documents.GetDocuments;
using EnterpriseFlow.Application.Features.Workflows.GetWorkflowById;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 2, Sprint 7b (Backend) — F5/HU-050/HU-051/HU-081. Upload and download go through
/// <c>LocalStorageProvider</c> for real (real disk I/O under the OS temp dir, see
/// CustomWebApplicationFactory) — these tests prove the file that comes back out is byte-for-
/// byte the one that went in, not just that the Document row was created.
/// </summary>
public sealed class DocumentsEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly byte[] RealPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%mock pdf content for testing\n");

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

    private static MultipartFormDataContent BuildUploadForm(
        byte[] fileBytes, string fileName, string contentType, DocumentOwnerType ownerType, Guid ownerId, Guid workflowDefinitionId)
    {
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
            { new StringContent(ownerType.ToString()), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent(workflowDefinitionId.ToString()), "workflowDefinitionId" },
        };
    }

    /// <summary>Builds a real "Document Approval" Workflow (Borrador → Aprobado) through the
    /// Workflows Api, exactly as 7a's tests did — Documents depends on Workflow, not the other
    /// way around (see r2-07a-backend-workflow.md).</summary>
    private static async Task<(Guid WorkflowId, Guid DraftStateId, Guid ApprovedStateId)> SeedWorkflowAsync(HttpClient client)
    {
        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Document Approval" }));
        var draftId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Borrador", isInitial = true, isFinal = false }));
        var approvedId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Aprobado", isInitial = false, isFinal = true }));
        await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/transitions", new { name = "Aprobar", fromStateId = draftId, toStateId = approvedId });

        return (workflowId, draftId, approvedId);
    }

    [Fact]
    public async Task Upload_Then_Download_Returns_The_Exact_Bytes_Uploaded()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Read);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, draftId, _) = await SeedWorkflowAsync(client);

        var uploadResponse = await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId));
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentId = await ReadIdAsync(uploadResponse);

        var metadata = await client.GetFromJsonAsync<DocumentDto>($"/api/documents/{documentId}");
        metadata!.FileName.Should().Be("contract.pdf");
        metadata.CurrentWorkflowStateId.Should().Be(draftId);

        var downloadResponse = await client.GetAsync($"/api/documents/{documentId}/content");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();

        downloadedBytes.Should().Equal(RealPdfBytes, "LocalStorageProvider must return exactly what was uploaded");
    }

    [Fact]
    public async Task Upload_With_Extension_Mismatched_Content_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);

        // An executable's real header, renamed to look like a PDF — HU-051's core scenario.
        byte[] exeBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];

        var response = await client.PostAsync(
            "/api/documents", BuildUploadForm(exeBytes, "invoice.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_With_Disallowed_Extension_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);

        var response = await client.PostAsync(
            "/api/documents",
            BuildUploadForm([0x4D, 0x5A], "script.exe", "application/octet-stream", DocumentOwnerType.Project, ownerId, workflowId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_For_A_Nonexistent_Owner_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);

        var response = await client.PostAsync(
            "/api/documents",
            BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, Guid.NewGuid(), workflowId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_Against_A_Workflow_With_No_Initial_State_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Empty Workflow" }));

        var response = await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Sprint 9 (Pruebas): <c>DocumentsEndpoints.UploadAsync</c> parses the multipart
    /// request by hand (no automatic model binding, see 7b) — its three guard clauses (non-
    /// multipart body, missing file, malformed form fields) had 0% coverage; every other upload
    /// test always sent a well-formed request.</summary>
    [Fact]
    public async Task Upload_With_A_NonMultipart_Body_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Documents.Manage);

        var response = await client.PostAsJsonAsync("/api/documents", new { notAFile = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_Without_A_File_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);

        var form = new MultipartFormDataContent
        {
            { new StringContent(DocumentOwnerType.Project.ToString()), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent(workflowId.ToString()), "workflowDefinitionId" },
        };

        var response = await client.PostAsync("/api/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_With_Malformed_Form_Fields_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(tenantId, Permissions.Documents.Manage);
        var fileContent = new ByteArrayContent(RealPdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "contract.pdf" },
            { new StringContent("not-a-valid-owner-type"), "ownerType" },
            { new StringContent("not-a-guid"), "ownerId" },
            { new StringContent("not-a-guid-either"), "workflowDefinitionId" },
        };

        var response = await client.PostAsync("/api/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transition_To_An_Allowed_State_Updates_The_Document()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Read, Permissions.Documents.Approve);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(client);
        var documentId = await ReadIdAsync(await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId)));

        var response = await client.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var metadata = await client.GetFromJsonAsync<DocumentDto>($"/api/documents/{documentId}");
        metadata!.CurrentWorkflowStateId.Should().Be(approvedId);
    }

    [Fact]
    public async Task Transition_To_A_State_Without_A_Defined_Transition_Returns_BadRequest()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, draftId, approvedId) = await SeedWorkflowAsync(client);
        var documentId = await ReadIdAsync(await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId)));

        // No transition from "Aprobado" back to "Borrador" was ever defined.
        await client.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });
        var response = await client.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = draftId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Removes_The_Document_And_Its_Underlying_File()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Read);
        var ownerId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);
        var documentId = await ReadIdAsync(await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "contract.pdf", "application/pdf", DocumentOwnerType.Project, ownerId, workflowId)));

        (await client.DeleteAsync($"/api/documents/{documentId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/documents/{documentId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/documents/{documentId}/content")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocuments_Lists_Only_Documents_Of_The_Requested_Owner()
    {
        var tenantId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(
            tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Read);
        var ownerAId = await SeedProjectOwnerAsync(tenantId);
        var ownerBId = await SeedProjectOwnerAsync(tenantId);
        var (workflowId, _, _) = await SeedWorkflowAsync(client);

        await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "owner-a.pdf", "application/pdf", DocumentOwnerType.Project, ownerAId, workflowId));
        await client.PostAsync(
            "/api/documents", BuildUploadForm(RealPdfBytes, "owner-b.pdf", "application/pdf", DocumentOwnerType.Project, ownerBId, workflowId));

        var documents = await client.GetFromJsonAsync<List<DocumentListItemDto>>(
            $"/api/documents?ownerType={DocumentOwnerType.Project}&ownerId={ownerAId}");

        documents.Should().ContainSingle(d => d.FileName == "owner-a.pdf");
        documents.Should().NotContain(d => d.FileName == "owner-b.pdf");
    }

    private sealed record IdResponse(Guid Id);
}
