using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Clients.CreateClient;
using EnterpriseFlow.Application.Features.Clients.DeactivateClient;
using EnterpriseFlow.Application.Features.Clients.GetClientById;
using EnterpriseFlow.Application.Features.Clients.GetClients;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class ClientsEndpoints
{
    public static IEndpointRouteBuilder MapClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapPost("/", async (CreateClientCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/clients/{id}", new { id });
            })
            .RequirePermission(Permissions.Clients.Manage);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var client = await sender.Send(new GetClientByIdQuery(id), ct);
                return client is null ? Results.NotFound() : Results.Ok(client);
            })
            .RequirePermission(Permissions.Clients.Read);

        group.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeactivateClientCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Clients.Manage);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetClientsQuery(), ct)))
            .RequirePermission(Permissions.Clients.Read);

        return app;
    }
}
