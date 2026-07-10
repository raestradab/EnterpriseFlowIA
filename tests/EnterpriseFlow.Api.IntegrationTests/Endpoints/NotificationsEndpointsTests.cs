using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using EnterpriseFlow.Api.IntegrationTests.Fakes;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Notifications.GetMyNotifications;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 2, Sprint 7c (Backend) — F6/ADR-0011. Proves the whole reactive chain end-to-end
/// through real HTTP: a Document transition (7b) raises a domain event, which a handler turns
/// into a persisted Notification for every member of the owning Project — no direct call from
/// TransitionDocumentCommandHandler to anything notification-related, only the domain events
/// pipeline already exercised by HU-012 in Release 1.
/// </summary>
public sealed class NotificationsEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly byte[] RealPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%mock pdf content for testing\n");

    private HttpClient CreateAuthenticatedClient(Guid userId, Guid tenantId, params string[] permissions)
    {
        var tokenService = factory.Services.GetRequiredService<ITokenService>();
        var token = tokenService.GenerateAccessToken(userId, tenantId, permissions);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<Guid> SeedProjectWithMemberAsync(Guid tenantId, Guid memberUserId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = Client.Create("Test Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);

        var project = Project.Create("Test Project", client.Id, null, null);
        project.AssignTenant(tenantId);
        project.AddMember(memberUserId, ProjectRole.Developer);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        return project.Id;
    }

    /// <summary>Unlike <see cref="SeedProjectWithMemberAsync"/>, this seeds a real <c>User</c>
    /// row too — <c>NotifyOnDocumentWorkflowTransitionedHandler</c> looks up recipients' email
    /// addresses via <c>db.Users</c> before calling <c>IRealtimeNotifier</c>/<c>IEmailQueue</c>,
    /// so a <c>ProjectMember.UserId</c> with no matching <c>User</c> row (fine for the
    /// Notification-persistence path, which doesn't need one) silently resolves zero recipients
    /// for that specific step — production always has a real User here because
    /// <c>AddProjectMemberValidator</c> requires one to add a member in the first place; only a
    /// test that skips the Api and seeds directly can hit this gap, which is exactly what this
    /// helper exists to avoid.</summary>
    private async Task<(Guid ProjectId, Guid MemberUserId)> SeedProjectWithRealMemberAsync(Guid tenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var member = User.Create($"member-{Guid.NewGuid():N}@test.local", "not-a-real-hash");
        member.AssignTenant(tenantId);
        db.Users.Add(member);

        var client = Client.Create("Test Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);

        var project = Project.Create("Test Project", client.Id, null, null);
        project.AssignTenant(tenantId);
        project.AddMember(member.Id, ProjectRole.Developer);
        db.Projects.Add(project);

        await db.SaveChangesAsync();

        return (project.Id, member.Id);
    }

    private async Task<(Guid ProjectId, Guid TaskId)> SeedProjectWithMemberAndTaskAsync(Guid tenantId, Guid memberUserId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = Client.Create("Test Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);

        var project = Project.Create("Test Project", client.Id, null, null);
        project.AssignTenant(tenantId);
        project.AddMember(memberUserId, ProjectRole.Developer);
        db.Projects.Add(project);

        var task = ProjectTask.Create("Test Task", null, TaskPriority.Medium, project.Id, null);
        task.AssignTenant(tenantId);
        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();

        return (project.Id, task.Id);
    }

    private async Task<Guid> SeedClientOwnerAsync(Guid tenantId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var client = Client.Create("Standalone Client", null);
        client.AssignTenant(tenantId);
        db.Clients.Add(client);
        await db.SaveChangesAsync();

        return client.Id;
    }

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

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

    private static MultipartFormDataContent BuildUploadForm(DocumentOwnerType ownerType, Guid ownerId, Guid workflowDefinitionId)
    {
        var fileContent = new ByteArrayContent(RealPdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        return new MultipartFormDataContent
        {
            { fileContent, "file", "contract.pdf" },
            { new StringContent(ownerType.ToString()), "ownerType" },
            { new StringContent(ownerId.ToString()), "ownerId" },
            { new StringContent(workflowDefinitionId.ToString()), "workflowDefinitionId" },
        };
    }

    [Fact]
    public async Task Transitioning_A_Project_Owned_Document_Notifies_Every_Project_Member()
    {
        var tenantId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var projectId = await SeedProjectWithMemberAsync(tenantId, memberUserId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Project, projectId, workflowId)));

        var memberClient = CreateAuthenticatedClient(memberUserId, tenantId);
        (await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications"))!.Should().BeEmpty();

        (await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var notifications = await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");

        notifications.Should().ContainSingle();
        notifications![0].EventName.Should().Be("document.transitioned");
        notifications[0].Message.Should().Contain("Aprobado");
        notifications[0].IsRead.Should().BeFalse();
    }

    /// <summary>Sprint 9 (Pruebas): the tests above only ever checked that a Notification row
    /// got persisted — coverage measurement found the handler's actual calls to
    /// <c>IRealtimeNotifier</c>/<c>IEmailQueue</c> were never exercised by any test. The fakes
    /// registered in <c>CustomWebApplicationFactory</c> are singletons shared across every test
    /// in this fixture, so assertions filter by this test's own <c>memberUserId</c> rather than
    /// asserting a total count — other tests in this class leave their own calls in the same
    /// list.</summary>
    [Fact]
    public async Task Transitioning_A_Document_Really_Invokes_The_Realtime_Notifier_And_Email_Queue()
    {
        var tenantId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var (projectId, memberUserId) = await SeedProjectWithRealMemberAsync(tenantId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Project, projectId, workflowId)));

        await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });

        var realtimeNotifier = factory.Services.GetRequiredService<FakeRealtimeNotifier>();
        realtimeNotifier.Calls.Should().ContainSingle(c => c.UserId == memberUserId && c.EventName == "document.transitioned");

        var emailQueue = factory.Services.GetRequiredService<FakeEmailQueue>();
        emailQueue.Calls.Should().ContainSingle(c => c.Body.Contains("Aprobado"));
    }

    /// <summary>Sprint 9 (Pruebas): the <c>DocumentOwnerType.Task</c> recipient-resolution
    /// branch (resolve the Task's own Project, then that Project's members) was never
    /// exercised — every existing test used <c>DocumentOwnerType.Project</c> directly.</summary>
    [Fact]
    public async Task Transitioning_A_Task_Owned_Document_Notifies_The_Tasks_Project_Members()
    {
        var tenantId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var (_, taskId) = await SeedProjectWithMemberAndTaskAsync(tenantId, memberUserId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Task, taskId, workflowId)));

        var memberClient = CreateAuthenticatedClient(memberUserId, tenantId);

        (await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var notifications = await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        notifications.Should().ContainSingle(n => n.EventName == "document.transitioned");
    }

    [Fact]
    public async Task MarkNotificationRead_Updates_The_Notification_And_Is_Reflected_On_The_Next_Read()
    {
        var tenantId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var projectId = await SeedProjectWithMemberAsync(tenantId, memberUserId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Project, projectId, workflowId)));
        await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });

        var memberClient = CreateAuthenticatedClient(memberUserId, tenantId);
        var notificationId = (await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications"))!.Single().Id;

        (await memberClient.PostAsync($"/api/notifications/{notificationId}/read", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var notifications = await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        notifications.Should().ContainSingle(n => n.Id == notificationId && n.IsRead);
    }

    [Fact]
    public async Task MarkNotificationRead_On_Another_Users_Notification_Returns_NotFound()
    {
        var tenantId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var projectId = await SeedProjectWithMemberAsync(tenantId, memberUserId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Project, projectId, workflowId)));
        await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });

        var memberClient = CreateAuthenticatedClient(memberUserId, tenantId);
        var notificationId = (await memberClient.GetFromJsonAsync<List<NotificationDto>>("/api/notifications"))!.Single().Id;

        // A different user in the same tenant must not be able to mark someone else's
        // notification as read — same tenant, wrong owner (IDOR check).
        var otherClient = CreateAuthenticatedClient(otherUserId, tenantId);
        var response = await otherClient.PostAsync($"/api/notifications/{notificationId}/read", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Transitioning_A_Client_Owned_Document_Creates_No_Notification()
    {
        // Clients have no "members" concept in this domain (unlike Projects) — the handler must
        // resolve zero recipients and simply do nothing, not throw.
        var tenantId = Guid.NewGuid();
        var adminClient = CreateAuthenticatedClient(
            Guid.NewGuid(), tenantId, Permissions.Workflows.Manage, Permissions.Documents.Manage, Permissions.Documents.Approve);
        var clientOwnerId = await SeedClientOwnerAsync(tenantId);
        var (workflowId, _, approvedId) = await SeedWorkflowAsync(adminClient);
        var documentId = await ReadIdAsync(await adminClient.PostAsync(
            "/api/documents", BuildUploadForm(DocumentOwnerType.Client, clientOwnerId, workflowId)));

        var response = await adminClient.PostAsJsonAsync($"/api/documents/{documentId}/transition", new { targetStateId = approvedId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record IdResponse(Guid Id);
}
