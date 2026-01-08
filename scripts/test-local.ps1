# GatekeeperHQ Local Testing Script (PowerShell)
#
# This script tests if all services are running correctly:
# 1. PostgreSQL database (via Docker)
# 2. .NET Server API
# 3. Next.js Client
# 4. Key API endpoints
#
# Usage:
#   .\scripts\test-local.ps1
#   or
#   powershell -ExecutionPolicy Bypass -File .\scripts\test-local.ps1

$ErrorActionPreference = "Continue"
$allTestsPassed = $true

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "GatekeeperHQ Local Testing" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check Docker
Write-Host "[1/7] Checking Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Docker is installed: $dockerVersion" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Docker is not running or not installed" -ForegroundColor Red
        $allTestsPassed = $false
    }
} catch {
    Write-Host "  ✗ Docker is not installed" -ForegroundColor Red
    $allTestsPassed = $false
}

# Test 2: Check PostgreSQL Container
Write-Host "[2/7] Checking PostgreSQL container..." -ForegroundColor Yellow
try {
    $postgresStatus = docker ps --filter "name=gatekeeperhq-db" --format "{{.Status}}" 2>&1
    if ($postgresStatus -match "Up") {
        Write-Host "  ✓ PostgreSQL container is running" -ForegroundColor Green
        Write-Host "    Status: $postgresStatus" -ForegroundColor Gray
    } else {
        Write-Host "  ✗ PostgreSQL container is not running" -ForegroundColor Red
        Write-Host "    Run: docker-compose up -d" -ForegroundColor Yellow
        $allTestsPassed = $false
    }
} catch {
    Write-Host "  ✗ Could not check PostgreSQL container" -ForegroundColor Red
    $allTestsPassed = $false
}

# Test 3: Check Backend API (Swagger)
Write-Host "[3/7] Checking Backend API (Swagger)..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "http://localhost:5000/swagger/index.html" -Method Get -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    if ($swaggerResponse.StatusCode -eq 200) {
        Write-Host "  ✓ Backend API is running" -ForegroundColor Green
        Write-Host "    Swagger UI: http://localhost:5000/swagger" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ✗ Backend API is not responding" -ForegroundColor Red
    Write-Host "    Make sure the server is running on http://localhost:5000" -ForegroundColor Yellow
    $allTestsPassed = $false
}

# Test 4: Check Frontend
Write-Host "[4/7] Checking Frontend..." -ForegroundColor Yellow
try {
    $frontendResponse = Invoke-WebRequest -Uri "http://localhost:3000" -Method Get -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
    if ($frontendResponse.StatusCode -eq 200) {
        Write-Host "  ✓ Frontend is running" -ForegroundColor Green
        Write-Host "    URL: http://localhost:3000" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ✗ Frontend is not responding" -ForegroundColor Red
    Write-Host "    Make sure the client is running on http://localhost:3000" -ForegroundColor Yellow
    $allTestsPassed = $false
}

# Test 5: Test Login Endpoint
Write-Host "[5/7] Testing Login endpoint..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "admin@gatekeeperhq.com"
        password = "Admin123!"
    } | ConvertTo-Json

    $loginResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop

    if ($loginResponse.StatusCode -eq 200) {
        $loginData = $loginResponse.Content | ConvertFrom-Json
        if ($loginData.token) {
            Write-Host "  ✓ Login endpoint works" -ForegroundColor Green
            Write-Host "    Token received (length: $($loginData.token.Length))" -ForegroundColor Gray
            $global:testToken = $loginData.token
        } else {
            Write-Host "  ✗ Login endpoint returned no token" -ForegroundColor Red
            $allTestsPassed = $false
        }
    }
} catch {
    Write-Host "  ✗ Login endpoint failed" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "    Status Code: $statusCode" -ForegroundColor Yellow
    }
    Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Yellow
    $allTestsPassed = $false
}

# Test 6: Test Protected Endpoint (with token)
Write-Host "[6/7] Testing Protected endpoint (GET /api/auth/me)..." -ForegroundColor Yellow
if ($global:testToken) {
    try {
        $headers = @{
            "Authorization" = "Bearer $($global:testToken)"
        }
        $meResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/me" -Method Get -Headers $headers -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop

        if ($meResponse.StatusCode -eq 200) {
            $meData = $meResponse.Content | ConvertFrom-Json
            Write-Host "  ✓ Protected endpoint works" -ForegroundColor Green
            Write-Host "    User: $($meData.email)" -ForegroundColor Gray
            Write-Host "    Permissions: $($meData.permissions.Count)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  ✗ Protected endpoint failed" -ForegroundColor Red
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            Write-Host "    Status Code: $statusCode" -ForegroundColor Yellow
        }
        $allTestsPassed = $false
    }
} else {
    Write-Host "  ⚠ Skipped (no token from login test)" -ForegroundColor Yellow
}

# Test 7: Test Database Connection (via API)
Write-Host "[7/7] Testing Database connection (via API)..." -ForegroundColor Yellow
if ($global:testToken) {
    try {
        $headers = @{
            "Authorization" = "Bearer $($global:testToken)"
        }
        $usersResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/users" -Method Get -Headers $headers -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop

        if ($usersResponse.StatusCode -eq 200) {
            $usersData = $usersResponse.Content | ConvertFrom-Json
            Write-Host "  ✓ Database connection works" -ForegroundColor Green
            Write-Host "    Users in database: $($usersData.Count)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  ✗ Database connection test failed" -ForegroundColor Red
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            Write-Host "    Status Code: $statusCode" -ForegroundColor Yellow
        }
        $allTestsPassed = $false
    }
} else {
    Write-Host "  ⚠ Skipped (no token from login test)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
if ($allTestsPassed) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    Write-Host "Your application is running correctly." -ForegroundColor Green
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    Write-Host "Please check the errors above and ensure all services are running." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Quick start:" -ForegroundColor Yellow
    Write-Host "  .\scripts\start-dev.ps1" -ForegroundColor Gray
}
Write-Host "=========================================" -ForegroundColor Cyan

exit $(if ($allTestsPassed) { 0 } else { 1 })
