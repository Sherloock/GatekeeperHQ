# GatekeeperHQ Development Startup Script (PowerShell - Cross-Platform)
#
# This script starts all development services:
# 1. PostgreSQL database (via Docker Compose)
# 2. .NET Server API (in a new PowerShell window)
# 3. Next.js Client (in a new PowerShell window)
#
# Usage:
#   .\scripts\start-dev.ps1
#   or
#   powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
#
# Prerequisites:
#   - Docker and Docker Compose installed
#   - .NET SDK 8.0+ installed
#   - Node.js 18+ installed
#   - PostgreSQL container will be started in detached mode
#   - PowerShell 5.1+ (Windows) or PowerShell Core 7+ (cross-platform)
#
# Note: This script opens separate PowerShell windows for server and client.
#       Close those windows individually to stop the services.
#       The script prevents multiple instances from running simultaneously.

# Get the project root directory (parent of scripts folder)
$ProjectRoot = Split-Path -Parent $PSScriptRoot

# Lock file to prevent multiple instances
$LockFile = Join-Path $ProjectRoot ".start-dev.lock"

# Check if script is already running
if (Test-Path $LockFile) {
    $OldPid = Get-Content $LockFile -ErrorAction SilentlyContinue
    if ($OldPid) {
        $Process = Get-Process -Id $OldPid -ErrorAction SilentlyContinue
        if ($Process -and $Process.ProcessName -eq "powershell") {
            Write-Host "Warning: A PowerShell process with PID $OldPid is running." -ForegroundColor Yellow
            Write-Host "Checking if it's actually running the start script..." -ForegroundColor Yellow

            # Check if ports are in use
            $port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
            $port3000 = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue

            if ($port5000 -or $port3000) {
                Write-Host "Error: Ports 5000 or 3000 are already in use!" -ForegroundColor Red
                Write-Host "Please stop the existing services first." -ForegroundColor Yellow
                Write-Host "To force remove lock file: Remove-Item $LockFile -Force" -ForegroundColor Yellow
                exit 1
            } else {
                Write-Host "Ports are free. Removing stale lock file..." -ForegroundColor Green
                Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
            }
        } else {
            # Stale lock file, remove it
            Write-Host "Removing stale lock file (process $OldPid not found)..." -ForegroundColor Yellow
            Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
        }
    } else {
        # Empty or invalid lock file, remove it
        Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
    }
}

# Create lock file with current process ID
$PID | Out-File -FilePath $LockFile -Force

# Cleanup function
$Cleanup = {
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
}
Register-EngineEvent PowerShell.Exiting -Action $Cleanup | Out-Null

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GatekeeperHQ Development Startup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if ports are already in use
Write-Host "Checking ports..." -ForegroundColor Cyan
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
$port3000 = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue

if ($port5000) {
    Write-Host "Warning: Port 5000 is already in use!" -ForegroundColor Yellow
    Write-Host "This might prevent the server from starting." -ForegroundColor Yellow
}

if ($port3000) {
    Write-Host "Warning: Port 3000 is already in use!" -ForegroundColor Yellow
    Write-Host "This might prevent the client from starting." -ForegroundColor Yellow
}

# Start PostgreSQL
Write-Host ""
Write-Host "Starting PostgreSQL..." -ForegroundColor Green
Set-Location $ProjectRoot
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start PostgreSQL. Make sure Docker is running." -ForegroundColor Red
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
    exit 1
}

# Start Server
Write-Host "Starting Server..." -ForegroundColor Green
$serverCommand = "cd '$ProjectRoot\server'; dotnet run --project GatekeeperHQ.API"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $serverCommand

Start-Sleep -Seconds 3

# Start Client
Write-Host "Starting Client..." -ForegroundColor Green
$clientCommand = "cd '$ProjectRoot\client'; npm install; npm run dev"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $clientCommand

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "All services started!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Server API: http://localhost:5000" -ForegroundColor Yellow
Write-Host "Swagger UI: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "Client: http://localhost:3000" -ForegroundColor Yellow
Write-Host ""
Write-Host "Check the opened PowerShell windows for server and client logs." -ForegroundColor Yellow
Write-Host "Close those windows to stop the respective services." -ForegroundColor Yellow
Write-Host ""
Write-Host "To stop all services:" -ForegroundColor Cyan
Write-Host "  1. Close the server and client PowerShell windows" -ForegroundColor Gray
Write-Host "  2. Run: docker-compose down" -ForegroundColor Gray
Write-Host "  3. Or remove lock file: Remove-Item $LockFile -Force" -ForegroundColor Gray
Write-Host "=========================================" -ForegroundColor Cyan
