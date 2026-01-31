# Reset Database Script
# This script drops and recreates the database for development

Write-Host "Resetting GatekeeperHQ database..." -ForegroundColor Yellow

# Stop any running containers
docker-compose down

# Remove the database volume to start fresh
docker volume rm gatekeeperhq_postgres_data 2>$null

Write-Host "Database volume removed. Starting fresh database..." -ForegroundColor Green

# Start database
docker-compose up -d postgres

# Wait for database to be ready
Write-Host "Waiting for database to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "Database reset complete! Run the application to create the new schema." -ForegroundColor Green
