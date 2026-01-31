// Quick script to fix migration history
// Run with: dotnet script FixMigrationHistory.cs

using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? "Host=localhost;Port=5432;Database=gatekeeperhq;Username=postgres;Password=postgres";

Console.WriteLine("Fixing migration history...");
Console.WriteLine($"Connection: {connectionString.Replace("Password=", "Password=***")}");

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Create migration history table if it doesn't exist
var createTableSql = @"
CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);";

await using (var cmd = new NpgsqlCommand(createTableSql, connection))
{
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("Migration history table created/verified.");
}

// Insert migration record if it doesn't exist
var insertSql = @"
INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
VALUES ('20260112144947_InitialCreate', '8.0.0')
ON CONFLICT (""MigrationId"") DO NOTHING;";

await using (var cmd = new NpgsqlCommand(insertSql, connection))
{
    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    if (rowsAffected > 0)
    {
        Console.WriteLine("Migration history record inserted successfully!");
    }
    else
    {
        Console.WriteLine("Migration history record already exists.");
    }
}

Console.WriteLine("Done! You can now run the application.");
