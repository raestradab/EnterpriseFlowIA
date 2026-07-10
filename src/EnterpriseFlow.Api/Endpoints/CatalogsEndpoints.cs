using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Catalogs.AddCatalogItem;
using EnterpriseFlow.Application.Features.Catalogs.CreateCatalog;
using EnterpriseFlow.Application.Features.Catalogs.GetCatalogItems;
using EnterpriseFlow.Application.Features.Catalogs.GetCatalogs;
using EnterpriseFlow.Application.Features.Catalogs.RemoveCatalogItem;
using EnterpriseFlow.Application.Features.Catalogs.UpdateCatalogItem;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class CatalogsEndpoints
{
    public static IEndpointRouteBuilder MapCatalogsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogs").WithTags("Catalogs");

        group.MapPost("/", async (CreateCatalogCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/catalogs/{id}", new { id });
            })
            .RequirePermission(Permissions.Catalogs.Manage);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetCatalogsQuery(), ct)))
            .RequirePermission(Permissions.Catalogs.Read);

        group.MapGet("/{id:guid}/items", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetCatalogItemsQuery(id), ct)))
            .RequirePermission(Permissions.Catalogs.Read);

        group.MapPost("/{id:guid}/items", async (Guid id, AddCatalogItemRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new AddCatalogItemCommand(id, request.Key, request.Label), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Catalogs.Manage);

        group.MapPut("/{id:guid}/items/{itemId:guid}", async (
                Guid id,
                Guid itemId,
                UpdateCatalogItemRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new UpdateCatalogItemCommand(id, itemId, request.Label), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Catalogs.Manage);

        group.MapDelete("/{id:guid}/items/{itemId:guid}", async (
                Guid id,
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new RemoveCatalogItemCommand(id, itemId), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Catalogs.Manage);

        return app;
    }

    private sealed record AddCatalogItemRequest(string Key, string Label);

    private sealed record UpdateCatalogItemRequest(string Label);
}
