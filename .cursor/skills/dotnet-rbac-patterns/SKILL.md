---
name: dotnet-rbac-patterns
description: Use when implementing role-based access control features, permission checks, or authorization policies in .NET.
---

## Checklist

- Define permissions in `GatekeeperHQ.Domain/Constants/Permissions.cs`.
- Register authorization policies in `Program.cs`.
- Use `[Authorize(Policy = Permissions.Xxx)]` on controllers/actions.
- Frontend checks are UX only; backend must enforce.

## Adding a new permission

1. Add constant to `GatekeeperHQ.Domain/Constants/Permissions.cs`
2. Register policy in `Program.cs` using `PermissionRequirement`
3. Use `[Authorize(Policy = Permissions.Xxx)]` on endpoints
4. Check with `canAccess(user, 'permission.key')` in frontend

## Authorization patterns

```csharp
// Controller
[Authorize(Policy = Permissions.UsersRead)]
[HttpGet]
public async Task<ActionResult<List<UserDto>>> GetUsers()
{
    return Ok(await _userService.GetAllAsync());
}

// Service - no authorization logic here, just business logic
public async Task<List<UserDto>> GetAllAsync()
{
    return await _context.Users
        .Select(u => new UserDto { Id = u.Id, Email = u.Email })
        .ToListAsync();
}
```

## Repo references

- Permissions: `GatekeeperHQ.Domain/Constants/Permissions.cs`
- Auth handler: `GatekeeperHQ.Infrastructure/Auth/`
- Frontend permission check: `lib/auth/canAccess.ts`
