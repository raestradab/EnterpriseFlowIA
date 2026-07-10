using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Contacts.CreateContact;
using EnterpriseFlow.Application.Features.Contacts.GetContactById;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class ContactsEndpoints
{
    public static IEndpointRouteBuilder MapContactsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contacts").WithTags("Contacts");

        group.MapPost("/", async (CreateContactCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/contacts/{id}", new { id });
            })
            .RequirePermission(Permissions.Contacts.Manage);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var contact = await sender.Send(new GetContactByIdQuery(id), ct);
                return contact is null ? Results.NotFound() : Results.Ok(contact);
            })
            .RequirePermission(Permissions.Contacts.Read);

        return app;
    }
}
