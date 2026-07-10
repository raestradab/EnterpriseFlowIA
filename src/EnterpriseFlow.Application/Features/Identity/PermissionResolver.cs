using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity;

/// <summary>
/// Shared by <see cref="Login.LoginCommandHandler"/> and
/// <see cref="RefreshAccessToken.RefreshAccessTokenCommandHandler"/> — both need "flatten this
/// user's roles into a permission list" and neither runs in an authenticated context (so both
/// must bypass the tenant query filter deliberately, same as the user lookup itself).
/// </summary>
internal static class PermissionResolver
{
    public static async Task<IReadOnlyCollection<string>> ResolveAsync(
        IAppDbContext db,
        User user,
        CancellationToken cancellationToken)
    {
        var roleIds = user.RoleAssignments.Select(r => r.RoleId).ToList();

        return await db.Roles.IgnoreQueryFilters()
            .Where(r => roleIds.Contains(r.Id))
            .SelectMany(r => r.Permissions.Select(p => p.Permission))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
