# Reset Admin Password Script
# This script resets the admin user password to "Admin123!"
# Usage: .\reset-admin-password.ps1

param(
    [string]$ConnectionString = "",
    [string]$AdminEmail = "admin@gatekeeperhq.com",
    [string]$NewPassword = "Admin123!"
)

Write-Host "Resetting admin password..." -ForegroundColor Yellow

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

Write-Host "Connection: $($ConnectionString -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray
Write-Host "Email: $AdminEmail" -ForegroundColor Gray
Write-Host "New Password: $NewPassword" -ForegroundColor Gray

# Check if psql is available
$psqlPath = "psql"
try {
    $null = Get-Command $psqlPath -ErrorAction Stop
} catch {
    Write-Host "Error: psql command not found. Please ensure PostgreSQL client tools are installed and in PATH." -ForegroundColor Red
    Write-Host "Alternatively, you can use the C# script: dotnet script server/scripts/ResetAdminPassword.cs" -ForegroundColor Cyan
    exit 1
}

# Check if user exists
Write-Host "Checking if admin user exists..." -ForegroundColor Yellow
$checkUserQuery = "SELECT COUNT(*) FROM `"Users`" WHERE `"Email`" = '$AdminEmail';"
$userCount = & $psqlPath $ConnectionString -t -c $checkUserQuery 2>&1 | ForEach-Object { $_.Trim() }

if ($userCount -eq "0") {
    Write-Host "Error: User with email '$AdminEmail' not found." -ForegroundColor Red
    Write-Host "Please ensure the database has been seeded." -ForegroundColor Yellow
    exit 1
}

Write-Host "Admin user found. Resetting password..." -ForegroundColor Yellow

# Generate BCrypt hash using a simple C# script approach
# Since PowerShell doesn't have BCrypt built-in, we'll use a workaround
# We need to use the C# script or create a temporary .NET program

Write-Host "Note: This script requires BCrypt to hash the password." -ForegroundColor Yellow
Write-Host "For best results, use the C# script instead:" -ForegroundColor Cyan
Write-Host "  dotnet script server/scripts/ResetAdminPassword.cs" -ForegroundColor Cyan
Write-Host ""
Write-Host "Alternatively, you can manually run this SQL after generating the hash:" -ForegroundColor Yellow
Write-Host "  UPDATE `"Users`" SET `"PasswordHash`" = '<BCryptHash>', `"UpdatedAt`" = NOW() WHERE `"Email`" = '$AdminEmail';" -ForegroundColor Gray
Write-Host ""

# Try to use the console application (preferred method)
$dotnetPath = "dotnet"
try {
    $null = Get-Command $dotnetPath -ErrorAction Stop

    Write-Host "Attempting to reset password using console application..." -ForegroundColor Yellow
    $projectPath = Join-Path $PSScriptRoot "ResetAdminPassword\ResetAdminPassword.csproj"

    if (Test-Path $projectPath) {
        $projectDir = Split-Path $projectPath
        Push-Location $projectDir
        $env:DATABASE_CONNECTION_STRING = $ConnectionString
        & $dotnetPath run --project ResetAdminPassword.csproj
        $scriptResult = $LASTEXITCODE
        Pop-Location

        if ($scriptResult -eq 0) {
            exit 0
        } else {
            Write-Host "Error: Failed to reset password." -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Error: ResetAdminPassword project not found at $projectPath" -ForegroundColor Red
        Write-Host "Trying alternative method with dotnet script..." -ForegroundColor Yellow

        # Fallback to dotnet script
        $scriptPath = Join-Path $PSScriptRoot "ResetAdminPassword.cs"
        if (Test-Path $scriptPath) {
            Push-Location $PSScriptRoot
            $env:DATABASE_CONNECTION_STRING = $ConnectionString
            & $dotnetPath script ResetAdminPassword.cs
            $scriptResult = $LASTEXITCODE
            Pop-Location

            if ($scriptResult -eq 0) {
                exit 0
            } else {
                Write-Host "Error: Failed to reset password using dotnet script." -ForegroundColor Red
                exit 1
            }
        } else {
            Write-Host "Error: No reset script found. Please ensure the scripts are present." -ForegroundColor Red
            exit 1
        }
    }
} catch {
    Write-Host "Error: dotnet command not found. Please install .NET SDK." -ForegroundColor Red
    Write-Host "You can manually reset the password by:" -ForegroundColor Yellow
    Write-Host "1. Installing .NET SDK" -ForegroundColor Cyan
    Write-Host "2. Running: dotnet run --project server/scripts/ResetAdminPassword/ResetAdminPassword.csproj" -ForegroundColor Cyan
    Write-Host "3. Or: dotnet script server/scripts/ResetAdminPassword.cs" -ForegroundColor Cyan
    exit 1
}