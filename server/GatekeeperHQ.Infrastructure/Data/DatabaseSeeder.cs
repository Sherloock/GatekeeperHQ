using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Permissions
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = Permissions.All.Select(key => new Permission
            {
                Key = key,
                Description = GetPermissionDescription(key)
            }).ToList();

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();
        }

        // Seed Roles
        if (!await context.Roles.AnyAsync())
        {
            var adminRole = new Role
            {
                Name = "Admin",
                Description = "Full system access"
            };

            var userRole = new Role
            {
                Name = "User",
                Description = "Basic user access"
            };

            await context.Roles.AddRangeAsync(adminRole, userRole);
            await context.SaveChangesAsync();

            // Assign all permissions to Admin role
            var allPermissions = await context.Permissions.ToListAsync();
            var adminRolePermissions = allPermissions.Select(p => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id
            }).ToList();

            await context.RolePermissions.AddRangeAsync(adminRolePermissions);
            await context.SaveChangesAsync();
        }

        // Seed Admin User (password: Admin123!)
        if (!await context.Users.AnyAsync())
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole != null)
            {
                var adminUser = new User
                {
                    Email = "admin@gatekeeperhq.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    IsActive = true,
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
            }
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
            Permissions.DashboardAccess => "Access dashboard",
            Permissions.SettingsAccess => "Access settings",
            _ => $"Permission: {key}"
        };
    }
}
