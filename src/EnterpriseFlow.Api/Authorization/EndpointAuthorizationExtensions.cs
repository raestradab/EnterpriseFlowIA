namespace EnterpriseFlow.Api.Authorization;

public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Requires the given permission for this endpoint. The permission string doubles as the
    /// Policy name, resolved on demand by <see cref="DynamicPermissionPolicyProvider"/> — no
    /// per-permission registration needed anywhere.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission) =>
        builder.RequireAuthorization(permission);
}
