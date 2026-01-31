# GatekeeperHQ Server Architecture Explanation

## Overview

This server follows a **Layered Architecture (Clean Architecture)** pattern, which separates concerns into distinct layers with clear responsibilities and dependencies.

## Folder Structure

```
server/
├── GatekeeperHQ.API/          # Presentation Layer (Entry Point)
│   ├── Controllers/            # HTTP endpoints
│   ├── DTOs/                   # Data Transfer Objects (API contracts)
│   └── Program.cs              # Application startup & configuration
│
├── GatekeeperHQ.Application/  # Business Logic Layer
│   └── Services/               # Business logic services
│
├── GatekeeperHQ.Domain/        # Core Domain Layer
│   ├── Entities/               # Domain models (User, Role, Permission)
│   └── Constants/              # Domain constants (Permissions)
│
└── GatekeeperHQ.Infrastructure/ # Infrastructure Layer
    ├── Auth/                   # JWT, authentication
    └── Data/                   # Database (EF Core, DbContext)
```

## Why This Structure?

### 1. **Separation of Concerns**
Each layer has a single, well-defined responsibility:
- **API Layer**: Handles HTTP requests/responses, routing, validation
- **Application Layer**: Contains business logic and use cases
- **Domain Layer**: Core business entities and rules (no dependencies)
- **Infrastructure Layer**: External concerns (database, JWT, file system)

### 2. **Dependency Direction (Dependency Inversion Principle)**
```
API → Application → Domain ← Infrastructure
```

**Key Rule**: Dependencies flow **INWARD** toward the Domain layer.

- **Domain** has **NO dependencies** (pure business logic)
- **Application** depends on **Domain** only
- **Infrastructure** depends on **Domain** (implements interfaces)
- **API** depends on **Application** and **Infrastructure**

This means:
- ✅ You can change the database (Infrastructure) without touching business logic
- ✅ You can swap JWT implementation without changing services
- ✅ Business rules are isolated and testable
- ✅ Domain models are independent of any framework

### 3. **Testability**
- **Domain**: Pure C# classes, easy to unit test
- **Application**: Can be tested with mock repositories
- **Infrastructure**: Can be tested with in-memory databases
- **API**: Can be tested with integration tests

### 4. **Maintainability**
- Changes to one layer don't cascade to others
- Easy to find code (know where to look)
- Clear boundaries prevent mixing concerns

## How the Server Works

### Request Flow Example: User Login

```
1. HTTP Request
   ↓
   POST /api/auth/login
   { "email": "...", "password": "..." }

2. API Layer (AuthController.cs)
   ↓
   - Receives LoginRequest DTO
   - Validates input (null checks)
   - Calls IAuthService.LoginAsync()

3. Application Layer (AuthService.cs)
   ↓
   - Business logic: validates credentials
   - Uses AppDbContext to query database
   - Uses JwtService to generate token
   - Returns AuthResult

4. Infrastructure Layer
   ↓
   - AppDbContext: Queries PostgreSQL via EF Core
   - JwtService: Generates JWT token with permissions
   - BCrypt: Verifies password hash

5. Response
   ↓
   AuthController returns LoginResponse DTO
   { "token": "...", "userId": 1, "permissions": [...] }
```

### Detailed Flow Breakdown

#### Step 1: Request Arrives at Controller
```9:43:server/GatekeeperHQ.API/Controllers/AuthController.cs
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        return Ok(new LoginResponse
        {
            Token = result.Token,
            UserId = result.UserId,
            Email = result.Email,
            Permissions = result.Permissions
        });
    }
```

**What happens here:**
- Controller receives HTTP request
- Validates input (basic checks)
- Delegates to service layer (no business logic here!)
- Returns HTTP response with appropriate status code

#### Step 2: Service Layer Processes Business Logic
```25:55:server/GatekeeperHQ.Application/Services/AuthService.cs
    public async Task<AuthResult?> LoginAsync(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToList();

        var token = _jwtService.GenerateToken(user.Id, user.Email, permissions);

        return new AuthResult
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Permissions = permissions
        };
    }
```

**What happens here:**
- Queries database for user (with all related data)
- Verifies password using BCrypt
- Collects all permissions from user's roles
- Generates JWT token
- Returns result (or null if invalid)

#### Step 3: Infrastructure Handles Technical Details

**Database Access (AppDbContext):**
```6:16:server/GatekeeperHQ.Infrastructure/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
```

**JWT Token Generation (JwtService):**
```23:50:server/GatekeeperHQ.Infrastructure/Auth/JwtService.cs
    public string GenerateToken(int userId, string email, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add permissions as claims
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
```

## Dependency Injection Setup

All services are registered in `Program.cs`:

```144:171:server/GatekeeperHQ.API/Program.cs
// Register services
builder.Services.AddScoped<JwtService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var jwtSettings = configuration.GetSection("Jwt");

    var secretKey = jwtSettings["SecretKey"]
        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("JWT SecretKey not configured");

    var issuer = jwtSettings["Issuer"]
        ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
        ?? throw new InvalidOperationException("JWT Issuer not configured");

    var audience = jwtSettings["Audience"]
        ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? throw new InvalidOperationException("JWT Audience not configured");

    return new JwtService(
        secretKey,
        issuer,
        audience,
        int.Parse(jwtSettings["ExpirationMinutes"] ?? "30")
    );
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
```

**Why Dependency Injection?**
- **Loose Coupling**: Controllers depend on interfaces, not concrete classes
- **Testability**: Easy to inject mock services in tests
- **Flexibility**: Can swap implementations without changing code
- **Lifetime Management**: ASP.NET Core manages object lifetimes (Scoped, Singleton, Transient)

## Project Dependencies (csproj files)

### GatekeeperHQ.API.csproj
```16:19:server/GatekeeperHQ.API/GatekeeperHQ.API.csproj
  <ItemGroup>
    <ProjectReference Include="..\GatekeeperHQ.Application\GatekeeperHQ.Application.csproj" />
    <ProjectReference Include="..\GatekeeperHQ.Infrastructure\GatekeeperHQ.Infrastructure.csproj" />
  </ItemGroup>
```
**Depends on:** Application + Infrastructure

### GatekeeperHQ.Application.csproj
```13:16:server/GatekeeperHQ.Application/GatekeeperHQ.Application.csproj
  <ItemGroup>
    <ProjectReference Include="..\GatekeeperHQ.Domain\GatekeeperHQ.Domain.csproj" />
    <ProjectReference Include="..\GatekeeperHQ.Infrastructure\GatekeeperHQ.Infrastructure.csproj" />
  </ItemGroup>
```
**Depends on:** Domain + Infrastructure (for DbContext)

### GatekeeperHQ.Infrastructure.csproj
```21:23:server/GatekeeperHQ.Infrastructure/GatekeeperHQ.Infrastructure.csproj
  <ItemGroup>
    <ProjectReference Include="..\GatekeeperHQ.Domain\GatekeeperHQ.Domain.csproj" />
  </ItemGroup>
```
**Depends on:** Domain only

### GatekeeperHQ.Domain.csproj
```1:9:server/GatekeeperHQ.Domain/GatekeeperHQ.Domain.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```
**Depends on:** Nothing! Pure business logic.

## Security Flow

### Authentication (Login)
1. User sends email/password
2. Server verifies password with BCrypt
3. Server generates JWT with user ID, email, and permissions
4. Client stores JWT token

### Authorization (Protected Endpoints)
1. Client sends request with `Authorization: Bearer <token>` header
2. JWT middleware validates token
3. `PermissionAuthorizationHandler` checks if user has required permission
4. Request proceeds or returns 403 Forbidden

```118:142:server/GatekeeperHQ.API/Program.cs
// Configure Authorization with Permission-based policies
builder.Services.AddAuthorization(options =>
{
    // Register policies for each permission
    options.AddPolicy(Permissions.UsersView, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.UsersView)));
    options.AddPolicy(Permissions.UsersEdit, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.UsersEdit)));
    options.AddPolicy(Permissions.UsersDelete, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.UsersDelete)));
    options.AddPolicy(Permissions.UsersCreate, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.UsersCreate)));
    options.AddPolicy(Permissions.RolesView, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.RolesView)));
    options.AddPolicy(Permissions.RolesManage, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.RolesManage)));
    options.AddPolicy(Permissions.PermissionsView, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.PermissionsView)));
    options.AddPolicy(Permissions.DashboardAccess, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.DashboardAccess)));
    options.AddPolicy(Permissions.SettingsAccess, policy =>
        policy.Requirements.Add(new PermissionRequirement(Permissions.SettingsAccess)));
});
```

## Benefits of This Architecture

### 1. **Scalability**
- Easy to add new features (new controller, service, entity)
- Can scale layers independently
- Clear separation makes it easy to understand

### 2. **Maintainability**
- Changes are localized to specific layers
- Easy to find and fix bugs
- Code is organized logically

### 3. **Testability**
- Each layer can be tested independently
- Mock dependencies easily
- Unit tests for business logic, integration tests for API

### 4. **Flexibility**
- Can swap database (change Infrastructure)
- Can change API framework (change API layer)
- Business logic remains unchanged

### 5. **Team Collaboration**
- Different developers can work on different layers
- Clear boundaries prevent conflicts
- Easy code reviews (know what to look for)

## Common Patterns Used

### 1. **Repository Pattern** (via DbContext)
- Abstracts database access
- Services don't know about SQL
- Easy to test with in-memory database

### 2. **DTO Pattern**
- API layer uses DTOs (not domain entities)
- Prevents exposing internal structure
- Allows versioning API independently

### 3. **Service Pattern**
- Business logic in services
- Controllers are thin (just HTTP handling)
- Services are reusable

### 4. **Dependency Injection**
- Loose coupling
- Testable
- Managed by ASP.NET Core

## Summary

**The folder structure exists to:**
1. **Separate concerns** - Each layer has one job
2. **Control dependencies** - Dependencies flow inward
3. **Enable testing** - Each layer is independently testable
4. **Improve maintainability** - Changes are localized
5. **Support scalability** - Easy to add features

**The server works by:**
1. Receiving HTTP requests at Controllers
2. Delegating to Services for business logic
3. Using Infrastructure for technical details (DB, JWT)
4. Returning responses through DTOs

This architecture follows industry best practices and makes the codebase professional, maintainable, and scalable.
