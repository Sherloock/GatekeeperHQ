using System.Text;
using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Infrastructure.Auth;
using GatekeeperHQ.Infrastructure.Data;
using GatekeeperHQ.Application.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GatekeeperHQ API",
        Version = "v1",
        Description = "RBAC Admin Panel API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS with restricted headers and methods
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:3001" };

        policy.WithOrigins(allowedOrigins)
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .AllowCredentials();
    });
});

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Database connection string not configured. Set it in appsettings.json or DATABASE_CONNECTION_STRING environment variable.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT SecretKey not configured. Set it in appsettings.json or JWT_SECRET_KEY environment variable.");

if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long.");
}

var issuer = jwtSettings["Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? throw new InvalidOperationException("JWT Issuer not configured");

var audience = jwtSettings["Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? throw new InvalidOperationException("JWT Audience not configured");

var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "30");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        NameClaimType = "sub"
    };
});

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

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

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

// Configure Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = rateLimitConfig["Window"] ?? "15m",
            Limit = int.Parse(rateLimitConfig["PermitLimit"] ?? "5")
        },
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline

// Enforce HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    // Remove server header
    context.Response.Headers.Remove("Server");

    // Add security headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: https:; " +
              "font-src 'self' data:; " +
              "connect-src 'self'; " +
              "frame-ancestors 'none';";
    context.Response.Headers.Append("Content-Security-Policy", csp);

    // Strict Transport Security (HSTS) - only in production
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    }

    await next();
});

// Rate Limiting
var rateLimitConfig = app.Configuration.GetSection("RateLimiting");
if (bool.Parse(rateLimitConfig["EnableRateLimiting"] ?? "true"))
{
    app.UseIpRateLimiting();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
