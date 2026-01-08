# GatekeeperHQ Development Startup Script (PowerShell)
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
#
# Note: This script opens separate PowerShell windows for server and client.
#       Close those windows individually to stop the services.

# Get the project root directory (parent of scripts folder)
$ProjectRoot = Split-Path -Parent $PSScriptRoot

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GatekeeperHQ Development Startup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Start PostgreSQL
Write-Host "Starting PostgreSQL..." -ForegroundColor Green
Set-Location $ProjectRoot
docker-compose up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to start PostgreSQL. Make sure Docker is running." -ForegroundColor Red
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
Write-Host "=========================================" -ForegroundColor Cyan
