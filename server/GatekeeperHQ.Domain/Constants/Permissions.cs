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

    // Dashboard
    public const string DashboardAccess = "dashboard.access";

    // Settings
    public const string SettingsAccess = "settings.access";

    // Get all permissions as a list
    public static IReadOnlyList<string> All => new[]
    {
        UsersView,
        UsersEdit,
        UsersDelete,
        UsersCreate,
        RolesView,
        RolesManage,
        PermissionsView,
        DashboardAccess,
        SettingsAccess
    };
}
