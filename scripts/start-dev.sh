#!/bin/bash

# GatekeeperHQ Development Startup Script (Bash)
#
# This script starts all development services:
# 1. PostgreSQL database (via Docker Compose)
# 2. .NET Server API
# 3. Next.js Client
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

# Start Server
echo "Starting Server..."
cd "$PROJECT_ROOT/server"
dotnet run --project GatekeeperHQ.API &
SERVER_PID=$!
cd "$PROJECT_ROOT"

# Wait a bit for server to start
sleep 3

# Start Client
echo "Starting Client..."
cd "$PROJECT_ROOT/client"
npm install && npm run dev &
CLIENT_PID=$!
cd "$PROJECT_ROOT"

echo ""
echo "========================================="
echo "All services started!"
echo "========================================="
echo "Server PID: $SERVER_PID"
echo "Client PID: $CLIENT_PID"
echo ""
echo "Server API: http://localhost:5000"
echo "Swagger UI: http://localhost:5000/swagger"
echo "Client: http://localhost:3000"
echo ""
echo "Press Ctrl+C to stop all services"
echo "========================================="

# Wait for user interrupt
trap "echo 'Stopping services...'; kill $SERVER_PID $CLIENT_PID 2>/dev/null; docker-compose down; exit" INT
wait
