# GatekeeperHQ Development Cleanup Script
#
# This script stops all running development services:
# 1. Stops processes on port 5000 (Server API)
# 2. Stops processes on port 3000 (Next.js Client)
# 3. Removes lock files
# 4. Optionally stops Docker containers
#
# Usage:
#   .\scripts\cleanup-dev.ps1
#   .\scripts\cleanup-dev.ps1 -StopDocker  # Also stops Docker containers

param(
    [switch]$StopDocker = $false
)

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LockFile = Join-Path $ProjectRoot ".start-dev.lock"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GatekeeperHQ Development Cleanup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Read client port from package.json
$packageJsonPath = Join-Path $ProjectRoot "client\package.json"
$clientPort = 3000
if (Test-Path $packageJsonPath) {
    $packageJson = Get-Content $packageJsonPath | ConvertFrom-Json
    if ($packageJson.scripts.dev -match '--port\s+(\d+)') {
        $clientPort = [int]$matches[1]
    }
}

# Stop Server (port 5000)
Write-Host "Stopping Server (port 5000)..." -ForegroundColor Cyan
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | Select-Object -First 1
if ($port5000) {
    $serverPid = $port5000.OwningProcess
    Stop-Process -Id $serverPid -Force -ErrorAction SilentlyContinue
    Write-Host "  Stopped Server API (PID: $serverPid)" -ForegroundColor Green
} else {
    Write-Host "  Port 5000 is free" -ForegroundColor Gray
}

# Stop Client (port 3000 or configured port)
Write-Host "Stopping Client (port $clientPort)..." -ForegroundColor Cyan
$portClient = Get-NetTCPConnection -LocalPort $clientPort -ErrorAction SilentlyContinue | Select-Object -First 1
if ($portClient) {
    $clientPid = $portClient.OwningProcess
    Stop-Process -Id $clientPid -Force -ErrorAction SilentlyContinue
    Write-Host "  Stopped Next.js Client (PID: $clientPid)" -ForegroundColor Green
} else {
    Write-Host "  Port $clientPort is free" -ForegroundColor Gray
}

Write-Host ""

# Remove lock file
Write-Host "Removing lock file..." -ForegroundColor Cyan
if (Test-Path $LockFile) {
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
    Write-Host "  Lock file removed" -ForegroundColor Green
} else {
    Write-Host "  No lock file found" -ForegroundColor Gray
}

Write-Host ""

# Optionally stop Docker containers
if ($StopDocker) {
    Write-Host "Stopping Docker containers..." -ForegroundColor Cyan
    Set-Location $ProjectRoot
    docker-compose down 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Docker containers stopped" -ForegroundColor Green
    } else {
        Write-Host "  Failed to stop Docker containers" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "All local services have been stopped." -ForegroundColor Yellow
if (-not $StopDocker) {
    Write-Host "Note: Docker containers are still running." -ForegroundColor Gray
    Write-Host "      Use -StopDocker flag to stop them: .\scripts\cleanup-dev.ps1 -StopDocker" -ForegroundColor Gray
}
