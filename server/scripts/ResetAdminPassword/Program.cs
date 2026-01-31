// Reset Admin Password Console Application
// Run with: dotnet run --project server/scripts/ResetAdminPassword/ResetAdminPassword.csproj

using Npgsql;
using BCrypt.Net;

var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=gatekeeperhq;Username=postgres;Password=postgres";

const string adminEmail = "admin@gatekeeperhq.com";
const string newPassword = "Admin123!";

Console.WriteLine("=== Reset Admin Password ===");
Console.WriteLine($"Connection: {connectionString.Replace("Password=", "Password=***")}");
Console.WriteLine($"Email: {adminEmail}");
Console.WriteLine($"New Password: {newPassword}");
Console.WriteLine();

// Hash the new password
var passwordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
Console.WriteLine("Password hash generated.");

await using var connection = new NpgsqlConnection(connectionString);
try
{
    await connection.OpenAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: Could not connect to database: {ex.Message}");
    Console.WriteLine("Please ensure PostgreSQL is running and the connection string is correct.");
    Environment.Exit(1);
}

// First, check if the user exists
var checkUserSql = @"
SELECT ""Id"", ""Email"", ""TenantId"", ""IsActive""
FROM ""Users""
WHERE ""Email"" = @email
LIMIT 1;";

int? userId = null;
int? tenantId = null;
string? currentEmail = null;
bool? isActive = null;

await using (var cmd = new NpgsqlCommand(checkUserSql, connection))
{
    cmd.Parameters.AddWithValue("email", adminEmail);
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        userId = reader.GetInt32(0);
        currentEmail = reader.GetString(1);
        tenantId = reader.GetInt32(2);
        isActive = reader.GetBoolean(3);
        Console.WriteLine($"Found user:");
        Console.WriteLine($"  ID: {userId}");
        Console.WriteLine($"  Email: {currentEmail}");
        Console.WriteLine($"  TenantId: {tenantId}");
        Console.WriteLine($"  IsActive: {isActive}");
    }
    else
    {
        Console.WriteLine($"Error: User with email '{adminEmail}' not found.");
        Console.WriteLine("Please ensure the database has been seeded.");
        Environment.Exit(1);
    }
}

if (!userId.HasValue)
{
    Console.WriteLine("Error: Could not find admin user.");
    Environment.Exit(1);
}

// Update the password
var updatePasswordSql = @"
UPDATE ""Users""
SET ""PasswordHash"" = @passwordHash, ""UpdatedAt"" = @updatedAt
WHERE ""Id"" = @userId;";

await using (var cmd = new NpgsqlCommand(updatePasswordSql, connection))
{
    cmd.Parameters.AddWithValue("passwordHash", passwordHash);
    cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("userId", userId.Value);

    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    if (rowsAffected > 0)
    {
        Console.WriteLine();
        Console.WriteLine("✓ Password reset successfully!");
        Console.WriteLine();
        Console.WriteLine("You can now login with:");
        Console.WriteLine($"  Email: {adminEmail}");
        Console.WriteLine($"  Password: {newPassword}");
    }
    else
    {
        Console.WriteLine("Warning: No rows were updated. The user might not exist or the password was already set.");
    }
}

Console.WriteLine();
Console.WriteLine("Done!");