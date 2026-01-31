namespace GatekeeperHQ.Domain.Constants;

public static class Permissions
{
    // User management
    public const string UsersView = "users.view";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersCreate = "users.create";

    // Role management
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage"; // create, edit, delete

    // Permission management
    public const string PermissionsView = "permissions.view";
    public const string PermissionsCreate = "permissions.create";
    public const string PermissionsManage = "permissions.manage"; // edit, delete

    // Dashboard
    public const string DashboardAccess = "dashboard.access";

    // Settings
    public const string SettingsAccess = "settings.access";

    // Tenant management (Super Admin only)
    public const string TenantsView = "tenants.view";
    public const string TenantsCreate = "tenants.create";
    public const string TenantsManage = "tenants.manage"; // edit, delete

    // Invitation management (Super Admin only)
    public const string InvitationsManage = "invitations.manage";

    // Get all tenant-level permissions (seeded per tenant)
    public static IReadOnlyList<string> TenantPermissions => new[]
    {
        UsersView,
        UsersEdit,
        UsersDelete,
        UsersCreate,
        RolesView,
        RolesManage,
        PermissionsView,
        PermissionsCreate,
        PermissionsManage,
        DashboardAccess,
        SettingsAccess
    };

    // Get all Super Admin permissions
    public static IReadOnlyList<string> SuperAdminPermissions => new[]
    {
        TenantsView,
        TenantsCreate,
        TenantsManage,
        InvitationsManage
    };

    // Get all permissions (for backward compatibility)
    public static IReadOnlyList<string> All => TenantPermissions
        .Concat(SuperAdminPermissions)
        .ToArray();
}
