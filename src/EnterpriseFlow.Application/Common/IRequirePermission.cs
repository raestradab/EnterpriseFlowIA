namespace EnterpriseFlow.Application.Common;

/// <summary>
/// Implemented by a Command/Query to declare the permission required to execute it.
/// <see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/> checks it before the
/// handler runs (ADR-0004) — the same enforcement point regardless of caller (HTTP endpoint,
/// future background job, etc.).
/// </summary>
public interface IRequirePermission
{
    string RequiredPermission { get; }
}
