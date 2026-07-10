using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Projects.AddProjectMember;
using EnterpriseFlow.Application.Features.Projects.CloseProject;
using EnterpriseFlow.Application.Features.Projects.CreateProject;
using EnterpriseFlow.Application.Features.Projects.GetProjectById;
using EnterpriseFlow.Application.Features.Projects.GetProjectHistory;
using EnterpriseFlow.Application.Features.Projects.GetProjects;
using EnterpriseFlow.Application.Features.Projects.RemoveProjectMember;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects");

        group.MapPost("/", async (CreateProjectCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/projects/{id}", new { id });
            })
            .RequirePermission(Permissions.Projects.Manage);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var project = await sender.Send(new GetProjectByIdQuery(id), ct);
                return project is null ? Results.NotFound() : Results.Ok(project);
            })
            .RequirePermission(Permissions.Projects.Read);

        group.MapGet("/{id:guid}/history", async (Guid id, DateTimeOffset asOf, ISender sender, CancellationToken ct) =>
            {
                var history = await sender.Send(new GetProjectHistoryQuery(id, asOf), ct);
                return history is null ? Results.NotFound() : Results.Ok(history);
            })
            .RequirePermission(Permissions.Projects.Read)
            .WithSummary("Consulta el estado de un Proyecto en un momento pasado (HU-102).")
            .WithDescription("Vía SQL Server Temporal Tables (ADR-0015) — el historial completo lo mantiene " +
                "la base de datos, no un log de auditoría escrito a mano. `asOf` es UTC ISO-8601.");

        group.MapPost("/{id:guid}/close", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CloseProjectCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Projects.Manage);

        group.MapPost("/{id:guid}/members", async (Guid id, AddProjectMemberRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddProjectMemberCommand(id, request.UserId, request.Role), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Projects.Manage);

        group.MapDelete("/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RemoveProjectMemberCommand(id, userId), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Projects.Manage);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetProjectsQuery(), ct)))
            .RequirePermission(Permissions.Projects.Read);

        return app;
    }

    private sealed record AddProjectMemberRequest(Guid UserId, ProjectRole Role);
}
