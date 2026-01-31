using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if we can access the database
        // Note: Migrations should be applied before seeding (handled in Program.cs)
        try
        {
            if (!await context.Database.CanConnectAsync())
            {
                Console.WriteLine("Warning: Cannot connect to database, skipping seed");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Database not accessible: {ex.Message}");
            return;
        }

        // Seed Default Tenant
        // Note: If Tenants table doesn't exist, AnyAsync() will throw an exception
        // which we catch below to provide a clear error message
        Tenant defaultTenant;
        try
        {
            if (!await context.Tenants.AnyAsync())
            {
                defaultTenant = new Tenant
                {
                    Name = "Default",
                    ApiKey = Guid.NewGuid().ToString("N"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await context.Tenants.AddAsync(defaultTenant);
                await context.SaveChangesAsync();
            }
            else
            {
                defaultTenant = await context.Tenants.FirstAsync();
            }
        }
        catch (Exception ex)
        {
            // Check if the error is due to missing table (relation does not exist)
            if (ex.Message.Contains("relation") && ex.Message.Contains("does not exist"))
            {
                Console.WriteLine($"Warning: Tenants table does not exist. Please ensure migrations are applied before seeding.");
                Console.WriteLine($"Error details: {ex.Message}");
            }
            else
            {
                Console.WriteLine($"Warning: Could not seed tenants: {ex.Message}");
            }
            return;
        }

        // Seed Permissions (tenant-level permissions only)
        try
        {
            if (!await context.Permissions.AnyAsync())
            {
                var permissions = Permissions.TenantPermissions.Select(key => new Permission
                {
                    TenantId = defaultTenant.Id,
                    Key = key,
                    Description = GetPermissionDescription(key)
                }).ToList();

                await context.Permissions.AddRangeAsync(permissions);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not seed permissions: {ex.Message}");
        }

        // Seed Roles
        try
        {
            if (!await context.Roles.AnyAsync())
            {
                var adminRole = new Role
                {
                    TenantId = defaultTenant.Id,
                    Name = "Admin",
                    Description = "Full system access"
                };

                var userRole = new Role
                {
                    TenantId = defaultTenant.Id,
                    Name = "User",
                    Description = "Basic user access"
                };

                await context.Roles.AddRangeAsync(adminRole, userRole);
                await context.SaveChangesAsync();

                // Assign all permissions to Admin role
                var allPermissions = await context.Permissions.Where(p => p.TenantId == defaultTenant.Id).ToListAsync();
                var adminRolePermissions = allPermissions.Select(p => new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = p.Id
                }).ToList();

                await context.RolePermissions.AddRangeAsync(adminRolePermissions);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not seed roles: {ex.Message}");
        }

        // Seed Super Admin User (password: SuperAdmin123!)
        try
        {
            if (!await context.Users.AnyAsync(u => u.IsSuperAdmin))
            {
                var superAdminUser = new User
                {
                    TenantId = null,  // Super Admin has no tenant
                    Email = "superadmin@gatekeeperhq.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123!"),
                    IsActive = true,
                    IsSuperAdmin = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(superAdminUser);
                await context.SaveChangesAsync();
                Console.WriteLine("Super Admin user created: superadmin@gatekeeperhq.com / SuperAdmin123!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not seed super admin user: {ex.Message}");
        }

        // Seed Tenant Admin User (password: Admin123!)
        try
        {
            if (!await context.Users.AnyAsync(u => u.TenantId == defaultTenant.Id))
            {
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.TenantId == defaultTenant.Id);
                if (adminRole != null)
                {
                    var adminUser = new User
                    {
                        TenantId = defaultTenant.Id,
                        Email = "admin@gatekeeperhq.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        IsActive = true,
                        IsSuperAdmin = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();

                    var userRole = new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    };

                    await context.UserRoles.AddAsync(userRole);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Tenant Admin user created: admin@gatekeeperhq.com / Admin123!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not seed tenant admin user: {ex.Message}");
        }
    }

    private static string GetPermissionDescription(string key)
    {
        return key switch
        {
            Permissions.UsersView => "View users list and details",
            Permissions.UsersEdit => "Edit user information",
            Permissions.UsersDelete => "Delete users",
            Permissions.UsersCreate => "Create new users",
            Permissions.RolesView => "View roles list and details",
            Permissions.RolesManage => "Create, edit, and delete roles",
            Permissions.PermissionsView => "View available permissions",
            Permissions.PermissionsCreate => "Create custom permissions",
            Permissions.PermissionsManage => "Edit and delete permissions",
            Permissions.DashboardAccess => "Access dashboard",
            Permissions.SettingsAccess => "Access settings",
            Permissions.TenantsView => "View all tenants",
            Permissions.TenantsCreate => "Create new tenants",
            Permissions.TenantsManage => "Edit and delete tenants",
            Permissions.InvitationsManage => "Create and revoke invitations",
            _ => $"Permission: {key}"
        };
    }
}
