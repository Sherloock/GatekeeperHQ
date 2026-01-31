# Fix Migration History Script
# This script marks the InitialCreate migration as applied without running it
# Use this when tables already exist but migration history is missing

param(
    [string]$ConnectionString = ""
)

Write-Host "Fixing migration history..." -ForegroundColor Yellow

# Get connection string from appsettings.json if not provided
if ([string]::IsNullOrEmpty($ConnectionString)) {
    $appsettingsPath = Join-Path $PSScriptRoot "..\GatekeeperHQ.API\appsettings.json"
    if (Test-Path $appsettingsPath) {
        $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
        $ConnectionString = $appsettings.ConnectionStrings.DefaultConnection
    }

    if ([string]::IsNullOrEmpty($ConnectionString)) {
        $ConnectionString = $env:DATABASE_CONNECTION_STRING
    }
}

if ([string]::IsNullOrEmpty($ConnectionString)) {
    Write-Host "Error: Connection string not found. Please provide it as a parameter or set DATABASE_CONNECTION_STRING environment variable." -ForegroundColor Red
    exit 1
}

Write-Host "Connection string: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray

# Extract database name from connection string
$dbName = ""
if ($ConnectionString -match "Database=([^;]+)") {
    $dbName = $matches[1]
} elseif ($ConnectionString -match "dbname=([^;]+)") {
    $dbName = $matches[1]
}

if ([string]::IsNullOrEmpty($dbName)) {
    Write-Host "Error: Could not extract database name from connection string." -ForegroundColor Red
    exit 1
}

Write-Host "Database: $dbName" -ForegroundColor Gray

# Check if PostgreSQL is accessible via psql
$psqlPath = "psql"
try {
    $null = Get-Command $psqlPath -ErrorAction Stop
} catch {
    Write-Host "Error: psql command not found. Please ensure PostgreSQL client tools are installed and in PATH." -ForegroundColor Red
    Write-Host "Alternatively, you can:" -ForegroundColor Yellow
    Write-Host "1. Run the SQL script directly: psql -h localhost -U postgres -d gatekeeperhq -f scripts/fix-migration-history.sql" -ForegroundColor Cyan
    Write-Host "2. Or use EF Core tools: dotnet ef database update --connection `"$ConnectionString`"" -ForegroundColor Cyan
    Write-Host "3. Or manually run the SQL from scripts/fix-migration-history.sql" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

# Check if migration history table exists
Write-Host "Checking migration history table..." -ForegroundColor Yellow
$checkTableQuery = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory');"
$tableExists = & $psqlPath $ConnectionString -t -c $checkTableQuery 2>&1 | ForEach-Object { $_.Trim() }

if ($tableExists -eq "f") {
    Write-Host "Migration history table does not exist. Creating it..." -ForegroundColor Yellow
    $createTableQuery = @"
CREATE TABLE ""__EFMigrationsHistory"" (
    ""MigrationId"" character varying(150) NOT NULL,
    ""ProductVersion"" character varying(32) NOT NULL,
    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
);
"@
    & $psqlPath $ConnectionString -c $createTableQuery 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to create migration history table." -ForegroundColor Red
        exit 1
    }
    Write-Host "Migration history table created." -ForegroundColor Green
}

# Check if migration is already recorded
Write-Host "Checking if migration is already recorded..." -ForegroundColor Yellow
$checkMigrationQuery = 'SELECT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = ''20260112144947_InitialCreate'');'
$migrationExists = & $psqlPath $ConnectionString -t -c $checkMigrationQuery 2>&1 | ForEach-Object { $_.Trim() }

if ($migrationExists -eq "t") {
    Write-Host "Migration '20260112144947_InitialCreate' is already recorded in history." -ForegroundColor Green
    Write-Host "No action needed." -ForegroundColor Green
    exit 0
}

# Insert migration record
Write-Host "Marking migration as applied..." -ForegroundColor Yellow
$insertQuery = 'INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES (''20260112144947_InitialCreate'', ''8.0.0'');'
$result = & $psqlPath $ConnectionString -c $insertQuery 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration history fixed successfully!" -ForegroundColor Green
    Write-Host "You can now run the application and it should start without migration errors." -ForegroundColor Green
} else {
    Write-Host "Error: Failed to insert migration record." -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}
