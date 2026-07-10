using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Workflows.AddWorkflowState;
using EnterpriseFlow.Application.Features.Workflows.AddWorkflowTransition;
using EnterpriseFlow.Application.Features.Workflows.CreateWorkflow;
using EnterpriseFlow.Application.Features.Workflows.GetWorkflowById;
using EnterpriseFlow.Application.Features.Workflows.GetWorkflows;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class WorkflowsEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workflows").WithTags("Workflows");

        group.MapPost("/", async (CreateWorkflowCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/workflows/{id}", new { id });
            })
            .RequirePermission(Permissions.Workflows.Manage);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetWorkflowsQuery(), ct)))
            .RequirePermission(Permissions.Workflows.Read);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var workflow = await sender.Send(new GetWorkflowByIdQuery(id), ct);
                return workflow is null ? Results.NotFound() : Results.Ok(workflow);
            })
            .RequirePermission(Permissions.Workflows.Read);

        group.MapPost("/{id:guid}/states", async (Guid id, AddWorkflowStateRequest request, ISender sender, CancellationToken ct) =>
            {
                var stateId = await sender.Send(new AddWorkflowStateCommand(id, request.Name, request.IsInitial, request.IsFinal), ct);
                return Results.Created($"/api/workflows/{id}", new { id = stateId });
            })
            .RequirePermission(Permissions.Workflows.Manage);

        group.MapPost("/{id:guid}/transitions", async (Guid id, AddWorkflowTransitionRequest request, ISender sender, CancellationToken ct) =>
            {
                var transitionId = await sender.Send(
                    new AddWorkflowTransitionCommand(id, request.Name, request.FromStateId, request.ToStateId), ct);
                return Results.Created($"/api/workflows/{id}", new { id = transitionId });
            })
            .RequirePermission(Permissions.Workflows.Manage);

        return app;
    }

    private sealed record AddWorkflowStateRequest(string Name, bool IsInitial, bool IsFinal);

    private sealed record AddWorkflowTransitionRequest(string Name, Guid FromStateId, Guid ToStateId);
}
