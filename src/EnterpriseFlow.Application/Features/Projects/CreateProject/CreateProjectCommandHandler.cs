using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.CreateProject;

public sealed class CreateProjectCommandHandler(IAppDbContext db) : IRequestHandler<CreateProjectCommand, Guid>
{
    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = Project.Create(request.Name, request.ClientId, request.StartDate, request.EstimatedEndDate);

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return project.Id;
    }
}
