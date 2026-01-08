# GatekeeperHQ Development Cleanup Script
#
# This script helps clean up stale lock files and check running services
#
# Usage:
#   .\scripts\cleanup-dev.ps1

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$LockFile = Join-Path $ProjectRoot ".start-dev.lock"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GatekeeperHQ Development Cleanup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check lock file
if (Test-Path $LockFile) {
    $OldPid = Get-Content $LockFile -ErrorAction SilentlyContinue
    Write-Host "Lock file found: $LockFile" -ForegroundColor Yellow
    Write-Host "Locked PID: $OldPid" -ForegroundColor Yellow

    if ($OldPid) {
        $Process = Get-Process -Id $OldPid -ErrorAction SilentlyContinue
        if ($Process) {
            Write-Host "Process $OldPid is running: $($Process.ProcessName)" -ForegroundColor Red
        } else {
            Write-Host "Process $OldPid is NOT running (stale lock)" -ForegroundColor Green
        }
    }
} else {
    Write-Host "No lock file found." -ForegroundColor Green
}

Write-Host ""

# Check ports
Write-Host "Checking ports..." -ForegroundColor Cyan
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
$port3000 = Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue

if ($port5000) {
    Write-Host "Port 5000 is in use (PID: $($port5000.OwningProcess))" -ForegroundColor Yellow
} else {
    Write-Host "Port 5000 is free" -ForegroundColor Green
}

if ($port3000) {
    Write-Host "Port 3000 is in use (PID: $($port3000.OwningProcess))" -ForegroundColor Yellow
} else {
    Write-Host "Port 3000 is free" -ForegroundColor Green
}

Write-Host ""

# Check running processes
Write-Host "Checking for running services..." -ForegroundColor Cyan
$dotnetProcesses = Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"}
$nodeProcesses = Get-Process | Where-Object {$_.ProcessName -like "*node*"}

if ($dotnetProcesses) {
    Write-Host "Found .NET processes:" -ForegroundColor Yellow
    $dotnetProcesses | ForEach-Object { Write-Host "  PID: $($_.Id) - $($_.ProcessName)" -ForegroundColor Gray }
} else {
    Write-Host "No .NET processes found" -ForegroundColor Green
}

if ($nodeProcesses) {
    Write-Host "Found Node.js processes:" -ForegroundColor Yellow
    $nodeProcesses | ForEach-Object { Write-Host "  PID: $($_.Id) - $($_.ProcessName)" -ForegroundColor Gray }
} else {
    Write-Host "No Node.js processes found" -ForegroundColor Green
}

Write-Host ""

# Ask to remove lock file
if (Test-Path $LockFile) {
    $OldPid = Get-Content $LockFile -ErrorAction SilentlyContinue
    if ($OldPid) {
        $Process = Get-Process -Id $OldPid -ErrorAction SilentlyContinue
        if (-not $Process) {
            Write-Host "Removing stale lock file..." -ForegroundColor Green
            Remove-Item $LockFile -Force
            Write-Host "Lock file removed!" -ForegroundColor Green
        } else {
            Write-Host "Lock file points to running process. Not removing." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Removing invalid lock file..." -ForegroundColor Green
        Remove-Item $LockFile -Force
        Write-Host "Lock file removed!" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
