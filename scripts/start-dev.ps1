# GatekeeperHQ Development Startup Script (PowerShell)
#
# This script starts all development services:
# 1. PostgreSQL database (via Docker Compose)
# 2. .NET Backend API (in a new PowerShell window)
# 3. Next.js Frontend (in a new PowerShell window)
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
# Note: This script opens separate PowerShell windows for backend and frontend.
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

# Start Backend
Write-Host "Starting Backend..." -ForegroundColor Green
$backendCommand = "cd '$ProjectRoot\backend'; dotnet run --project GatekeeperHQ.API"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $backendCommand

Start-Sleep -Seconds 3

# Start Frontend
Write-Host "Starting Frontend..." -ForegroundColor Green
$frontendCommand = "cd '$ProjectRoot\frontend'; npm install; npm run dev"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $frontendCommand

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "All services started!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Backend API: http://localhost:5000" -ForegroundColor Yellow
Write-Host "Swagger UI: http://localhost:5000/swagger" -ForegroundColor Yellow
Write-Host "Frontend: http://localhost:3000" -ForegroundColor Yellow
Write-Host ""
Write-Host "Check the opened PowerShell windows for backend and frontend logs." -ForegroundColor Yellow
Write-Host "Close those windows to stop the respective services." -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Cyan
