#!/bin/bash

# GatekeeperHQ Development Startup Script (Bash)
#
# This script starts all development services:
# 1. PostgreSQL database (via Docker Compose)
# 2. .NET Backend API
# 3. Next.js Frontend
#
# Usage:
#   ./scripts/start-dev.sh
#   or
#   bash scripts/start-dev.sh
#
# Prerequisites:
#   - Docker and Docker Compose installed
#   - .NET SDK 8.0+ installed
#   - Node.js 18+ installed
#   - PostgreSQL container will be started in detached mode
#
# Note: Press Ctrl+C to stop all services

set -e

# Get the project root directory (parent of scripts folder)
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Start PostgreSQL
echo "Starting PostgreSQL..."
cd "$PROJECT_ROOT"
docker-compose up -d

# Start Backend
echo "Starting Backend..."
cd "$PROJECT_ROOT/backend"
dotnet run --project GatekeeperHQ.API &
BACKEND_PID=$!
cd "$PROJECT_ROOT"

# Wait a bit for backend to start
sleep 3

# Start Frontend
echo "Starting Frontend..."
cd "$PROJECT_ROOT/frontend"
npm install && npm run dev &
FRONTEND_PID=$!
cd "$PROJECT_ROOT"

echo ""
echo "========================================="
echo "All services started!"
echo "========================================="
echo "Backend PID: $BACKEND_PID"
echo "Frontend PID: $FRONTEND_PID"
echo ""
echo "Backend API: http://localhost:5000"
echo "Swagger UI: http://localhost:5000/swagger"
echo "Frontend: http://localhost:3000"
echo ""
echo "Press Ctrl+C to stop all services"
echo "========================================="

# Wait for user interrupt
trap "echo 'Stopping services...'; kill $BACKEND_PID $FRONTEND_PID 2>/dev/null; docker-compose down; exit" INT
wait
