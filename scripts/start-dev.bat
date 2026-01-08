@echo off
REM GatekeeperHQ Development Startup Script (Windows Batch)
REM
REM This script starts all development services:
REM 1. PostgreSQL database (via Docker Compose)
REM 2. .NET Backend API (in a new command window)
REM 3. Next.js Frontend (in a new command window)
REM
REM Usage:
REM   scripts\start-dev.bat
REM   or
REM   cd scripts && start-dev.bat
REM
REM Prerequisites:
REM   - Docker and Docker Compose installed
REM   - .NET SDK 8.0+ installed
REM   - Node.js 18+ installed
REM   - PostgreSQL container will be started in detached mode
REM
REM Note: This script opens separate command windows for backend and frontend.
REM       Close those windows individually to stop the services.

echo =========================================
echo GatekeeperHQ Development Startup
echo =========================================
echo.

REM Get the project root directory (parent of scripts folder)
cd /d "%~dp0\.."

REM Start PostgreSQL
echo Starting PostgreSQL...
docker-compose up -d

if errorlevel 1 (
    echo Failed to start PostgreSQL. Make sure Docker is running.
    pause
    exit /b 1
)

REM Start Backend
echo Starting Backend...
start "GatekeeperHQ Backend" cmd /k "cd /d %~dp0\..\backend && dotnet run --project GatekeeperHQ.API"

timeout /t 3 /nobreak >nul

REM Start Frontend
echo Starting Frontend...
start "GatekeeperHQ Frontend" cmd /k "cd /d %~dp0\..\frontend && npm install && npm run dev"

echo.
echo =========================================
echo All services started!
echo =========================================
echo Backend API: http://localhost:5000
echo Swagger UI: http://localhost:5000/swagger
echo Frontend: http://localhost:3000
echo.
echo Check the opened command windows for backend and frontend logs.
echo Close those windows to stop the respective services.
echo =========================================
echo.
pause
