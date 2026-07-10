using System.Reflection;

namespace EnterpriseFlow.Application.Common;

/// <summary>
/// Catalog of permission strings (ADR-0004). This is the single source of truth for what a
/// permission is called; which Roles grant it is tenant-configurable data (RolePermissions),
/// seeded but editable — not defined here.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// All permissions currently in the catalog, discovered by reflection over the nested
    /// classes below. Used to seed the "Administrator" role on tenant registration — adding a
    /// new permission constant anywhere in this file is all a future module needs to do for
    /// new tenants' admins to receive it automatically, no seeding code to update.
    /// </summary>
    public static IReadOnlyCollection<string> All() => typeof(Permissions)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();

    public static class Companies
    {
        public const string Read = "companies.read";

        public const string Manage = "companies.manage";
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";
    }

    public static class Users
    {
        public const string Manage = "users.manage";
    }

    public static class Clients
    {
        public const string Read = "clients.read";

        public const string Manage = "clients.manage";
    }

    public static class Contacts
    {
        public const string Read = "contacts.read";

        public const string Manage = "contacts.manage";
    }

    public static class Projects
    {
        public const string Read = "projects.read";

        public const string Manage = "projects.manage";
    }

    public static class Tasks
    {
        public const string Read = "tasks.read";

        public const string Manage = "tasks.manage";
    }

    public static class Catalogs
    {
        public const string Read = "catalogs.read";

        public const string Manage = "catalogs.manage";
    }

    public static class Workflows
    {
        public const string Read = "workflows.read";

        public const string Manage = "workflows.manage";
    }

    public static class Documents
    {
        public const string Read = "documents.read";

        public const string Manage = "documents.manage";

        /// <summary>HU-081: transitioning a Document's workflow state (e.g. approving/rejecting)
        /// is a distinct permission from uploading/deleting one — a reviewer who can approve
        /// documents doesn't necessarily manage the document lifecycle itself, and vice versa.</summary>
        public const string Approve = "documents.approve";
    }
}
