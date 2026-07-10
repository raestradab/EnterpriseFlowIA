using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.GetWorkflowById;

public sealed record GetWorkflowByIdQuery(Guid Id) : IRequest<WorkflowDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Workflows.Read;
}

public sealed record WorkflowDto(
    Guid Id,
    string Name,
    IReadOnlyCollection<WorkflowStateDto> States,
    IReadOnlyCollection<WorkflowTransitionDto> Transitions);

public sealed record WorkflowStateDto(Guid Id, string Name, bool IsInitial, bool IsFinal);

public sealed record WorkflowTransitionDto(Guid Id, string Name, Guid FromStateId, Guid ToStateId);
