using EnterpriseFlow.Application.Features.Notifications.GetMyNotifications;
using EnterpriseFlow.Application.Features.Notifications.MarkNotificationRead;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetMyNotificationsQuery(), ct)));

        group.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new MarkNotificationReadCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
