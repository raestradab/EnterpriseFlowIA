namespace EnterpriseFlow.Infrastructure.Identity;

/// <summary>Custom claim types embedded in the access token — shared by issuing and reading code.</summary>
public static class AppClaimTypes
{
    public const string TenantId = "tenant_id";

    public const string Permission = "permission";
}
