using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Workflows.GetWorkflowById;
using EnterpriseFlow.Application.Features.Workflows.GetWorkflows;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseFlow.Api.IntegrationTests.Endpoints;

/// <summary>
/// Release 2, Sprint 7a (Backend) — F8.1. HU-080's central claim ("transiciones válidas son
/// datos, no código") is proven here through HTTP: a Workflow is built entirely from requests
/// (create → add states → add transitions), and an invalid transition is rejected without any
/// code change, only because the data doesn't define it.
/// </summary>
public sealed class WorkflowsEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task Build_A_Workflow_From_States_And_Transitions_Then_Read_It_Back()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage, Permissions.Workflows.Read);

        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Document Approval" }));
        var draftId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Borrador", isInitial = true, isFinal = false }));
        var reviewId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "En Revisión", isInitial = false, isFinal = false }));
        var approvedId = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Aprobado", isInitial = false, isFinal = true }));

        (await client.PostAsJsonAsync($"/api/workflows/{workflowId}/transitions", new { name = "Enviar a revisión", fromStateId = draftId, toStateId = reviewId }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync($"/api/workflows/{workflowId}/transitions", new { name = "Aprobar", fromStateId = reviewId, toStateId = approvedId }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var workflow = await client.GetFromJsonAsync<WorkflowDto>($"/api/workflows/{workflowId}");

        workflow!.States.Should().HaveCount(3);
        workflow.Transitions.Should().HaveCount(2);
        workflow.States.Should().ContainSingle(s => s.Name == "Borrador" && s.IsInitial);
        workflow.States.Should().ContainSingle(s => s.Name == "Aprobado" && s.IsFinal);
    }

    [Fact]
    public async Task AddWorkflowTransition_Referencing_A_State_From_Another_Workflow_Returns_BadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage);
        var workflowAId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Workflow A" }));
        var stateInA = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowAId}/states", new { name = "Borrador", isInitial = true, isFinal = false }));

        var workflowBId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Workflow B" }));
        var stateInB = await ReadIdAsync(await client.PostAsJsonAsync(
            $"/api/workflows/{workflowBId}/states", new { name = "Aprobado", isInitial = true, isFinal = true }));

        // stateInB doesn't belong to Workflow A — HU-080's invariant, enforced in Domain
        // (WorkflowDefinition.AddTransition), not just checked in the UI.
        var response = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowAId}/transitions",
            new { name = "Cross-workflow", fromStateId = stateInA, toStateId = stateInB });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddWorkflowState_Second_Initial_State_Returns_BadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage);
        var workflowId = await ReadIdAsync(await client.PostAsJsonAsync("/api/workflows", new { name = "Document Approval" }));
        await client.PostAsJsonAsync($"/api/workflows/{workflowId}/states", new { name = "Borrador", isInitial = true, isFinal = false });

        var response = await client.PostAsJsonAsync(
            $"/api/workflows/{workflowId}/states", new { name = "Otro Inicial", isInitial = true, isFinal = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Unknown_Workflow_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Read);

        var response = await client.GetAsync($"/api/workflows/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddWorkflowState_On_Unknown_Workflow_Returns_NotFound()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage);

        var response = await client.PostAsJsonAsync(
            $"/api/workflows/{Guid.NewGuid()}/states", new { name = "Borrador", isInitial = true, isFinal = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkflows_Lists_Only_The_Caller_Tenant_Workflows()
    {
        var tenantA = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage, Permissions.Workflows.Read);
        await tenantA.PostAsJsonAsync("/api/workflows", new { name = "Tenant A Workflow" });

        var tenantB = CreateAuthenticatedClient(Guid.NewGuid(), Permissions.Workflows.Manage, Permissions.Workflows.Read);
        await tenantB.PostAsJsonAsync("/api/workflows", new { name = "Tenant B Workflow" });

        var workflows = await tenantB.GetFromJsonAsync<List<WorkflowListItemDto>>("/api/workflows");

        workflows.Should().ContainSingle(w => w.Name == "Tenant B Workflow");
        workflows.Should().NotContain(w => w.Name == "Tenant A Workflow");
    }

    private sealed record IdResponse(Guid Id);
}
