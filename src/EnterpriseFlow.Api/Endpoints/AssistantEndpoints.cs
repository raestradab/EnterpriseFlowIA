using EnterpriseFlow.Application.Features.Assistant.GetAssistantMessages;
using EnterpriseFlow.Application.Features.Assistant.SendAssistantMessage;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant").WithTags("Assistant").RequireAuthorization();

        group.MapPost("/messages", async (SendAssistantMessageCommand command, ISender sender, CancellationToken ct) =>
            {
                var reply = await sender.Send(command, ct);
                return Results.Ok(new { reply });
            })
            .WithSummary("Envía un mensaje al asistente de IA y devuelve su respuesta.")
            .WithDescription("Ancla la respuesta en los datos reales del tenant vía tool-use contra " +
                "Queries de Application ya existentes (ADR-0013) — nunca acceso directo a SQL desde el modelo.");

        group.MapGet("/messages", async (ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetAssistantMessagesQuery(), ct)));

        return app;
    }
}
