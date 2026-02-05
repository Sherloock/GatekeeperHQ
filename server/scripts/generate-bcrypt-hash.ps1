# Generate BCrypt hash for GatekeeperHQ Super Admin password
# Usage: .\generate-bcrypt-hash.ps1 -Password "YourSecurePassword"

param(
    [Parameter(Mandatory=$true)]
    [string]$Password
)

# Check if BCrypt module is installed
if (-not (Get-Module -ListAvailable -Name "BCrypt")) {
    Write-Host "Installing BCrypt PowerShell module..." -ForegroundColor Yellow
    Install-Module -Name BCrypt -Scope CurrentUser -Force
}

Import-Module BCrypt

# Generate hash with cost factor 11 (same as the .NET app uses by default)
$hash = [BCrypt.Net.BCrypt]::HashPassword($Password, 11)

Write-Host ""
Write-Host "Generated BCrypt hash:" -ForegroundColor Green
Write-Host $hash -ForegroundColor Cyan
Write-Host ""
Write-Host "Use this hash in the seed-super-admin.sql script" -ForegroundColor Yellow
