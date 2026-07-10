using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.CreateCatalog;

public sealed class CreateCatalogCommandHandler(IAppDbContext db) : IRequestHandler<CreateCatalogCommand, Guid>
{
    public async Task<Guid> Handle(CreateCatalogCommand request, CancellationToken cancellationToken)
    {
        var catalog = CatalogDefinition.Create(request.Name);

        db.CatalogDefinitions.Add(catalog);

        await db.SaveChangesAsync(cancellationToken);

        return catalog.Id;
    }
}
