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

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
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
    return new JwtService(
        jwtSettings["SecretKey"]!,
        jwtSettings["Issuer"]!,
        jwtSettings["Audience"]!,
        int.Parse(jwtSettings["ExpirationMinutes"] ?? "30")
    );
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
