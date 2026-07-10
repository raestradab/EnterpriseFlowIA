using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.ProjectTasks.AssignTask;
using EnterpriseFlow.Application.Features.ProjectTasks.CancelTask;
using EnterpriseFlow.Application.Features.ProjectTasks.CompleteTask;
using EnterpriseFlow.Application.Features.ProjectTasks.CreateTask;
using EnterpriseFlow.Application.Features.ProjectTasks.GetMyCalendar;
using EnterpriseFlow.Application.Features.ProjectTasks.GetTaskById;
using EnterpriseFlow.Application.Features.ProjectTasks.GetTaskHistory;
using EnterpriseFlow.Application.Features.ProjectTasks.GetTasks;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class ProjectTasksEndpoints
{
    public static IEndpointRouteBuilder MapProjectTasksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapPost("/", async (CreateTaskCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/tasks/{id}", new { id });
            })
            .RequirePermission(Permissions.Tasks.Manage);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var task = await sender.Send(new GetTaskByIdQuery(id), ct);
                return task is null ? Results.NotFound() : Results.Ok(task);
            })
            .RequirePermission(Permissions.Tasks.Read);

        group.MapGet("/{id:guid}/history", async (Guid id, DateTimeOffset asOf, ISender sender, CancellationToken ct) =>
            {
                var history = await sender.Send(new GetTaskHistoryQuery(id, asOf), ct);
                return history is null ? Results.NotFound() : Results.Ok(history);
            })
            .RequirePermission(Permissions.Tasks.Read)
            .WithSummary("Consulta el estado de una Tarea en un momento pasado (HU-102).")
            .WithDescription("Vía SQL Server Temporal Tables (ADR-0015), mismo mecanismo que /api/projects/{id}/history. `asOf` es UTC ISO-8601.");

        group.MapPost("/{id:guid}/assign", async (Guid id, AssignTaskRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AssignTaskCommand(id, request.UserId), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Tasks.Manage);

        group.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CompleteTaskCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Tasks.Manage);

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelTaskCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Tasks.Manage);

        group.MapGet("/", async (Guid? projectId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTasksQuery(projectId), ct)))
            .RequirePermission(Permissions.Tasks.Read);

        app.MapGet("/api/calendar", async (DateOnly from, DateOnly to, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyCalendarQuery(from, to), ct)))
            .WithTags("Calendar")
            .RequireAuthorization();

        return app;
    }

    private sealed record AssignTaskRequest(Guid UserId);
}
