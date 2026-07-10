using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Api.Authorization;

/// <summary>
/// ADR-0004: resolves a Policy for any permission string on demand — an endpoint calls
/// <c>RequirePermission("companies.manage")</c> and this provider builds the corresponding
/// Policy the first time it's asked, instead of every permission needing an
/// <c>AddPolicy(...)</c> line registered up front in <c>Program.cs</c> (which would be exactly
/// the "duplicated code" the project rules disallow, at N permissions).
/// </summary>
public sealed class DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
