using Microsoft.AspNetCore.Authorization;

namespace EnterpriseFlow.Api.Authorization;

/// <summary>ADR-0004: one generic requirement parametrized by permission, not one Policy per permission.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
