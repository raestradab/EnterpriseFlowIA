using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Companies.CreateCompany;
using EnterpriseFlow.Application.Features.Companies.GetCompanies;
using EnterpriseFlow.Application.Features.Companies.GetCompanyById;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class CompaniesEndpoints
{
    public static IEndpointRouteBuilder MapCompaniesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/companies").WithTags("Companies");

        group.MapPost("/", async (CreateCompanyCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/companies/{id}", new { id });
            })
            .RequirePermission(Permissions.Companies.Manage);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var company = await sender.Send(new GetCompanyByIdQuery(id), ct);
                return company is null ? Results.NotFound() : Results.Ok(company);
            })
            .RequirePermission(Permissions.Companies.Read);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetCompaniesQuery(), ct)))
            .RequirePermission(Permissions.Companies.Read);

        return app;
    }
}
