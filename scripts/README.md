# GatekeeperHQ Development Scripts

This folder contains scripts to help you start and manage the GatekeeperHQ development environment.

## Available Scripts

### `start-dev.sh` (Bash/Linux/Mac/Git Bash)

Starts all development services in a single terminal session:
- PostgreSQL database (via Docker Compose)
- .NET Backend API
- Next.js Frontend

**Usage:**
```bash
./scripts/start-dev.sh
# or
bash scripts/start-dev.sh
```

**Features:**
- Runs all services in the background
- Displays PIDs for each service
- Gracefully stops all services on Ctrl+C
- Automatically stops PostgreSQL when script exits

**Prerequisites:**
- Docker and Docker Compose
- .NET SDK 8.0+
- Node.js 18+
- Bash shell (Git Bash on Windows)

---

### `start-dev.ps1` (PowerShell)

Starts all development services in separate PowerShell windows:
- PostgreSQL database (via Docker Compose)
- .NET Backend API (new window)
- Next.js Frontend (new window)

**Usage:**
```powershell
.\scripts\start-dev.ps1
# or
powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

**Features:**
- Opens separate windows for backend and frontend
- Easy to view logs for each service
- Color-coded output
- Windows stay open after script completes

**Prerequisites:**
- Docker and Docker Compose
- .NET SDK 8.0+
- Node.js 18+
- PowerShell 5.1+ (Windows PowerShell or PowerShell Core)

**Note:** If you get an execution policy error, run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

### `start-dev.bat` (Windows Command Prompt)

Starts all development services in separate command windows:
- PostgreSQL database (via Docker Compose)
- .NET Backend API (new window)
- Next.js Frontend (new window)

**Usage:**
```cmd
scripts\start-dev.bat
# or
cd scripts && start-dev.bat
```

**Features:**
- Opens separate windows for backend and frontend
- Easy to view logs for each service
- Works in standard Windows CMD
- Windows stay open after script completes

**Prerequisites:**
- Docker and Docker Compose
- .NET SDK 8.0+
- Node.js 18+
- Windows Command Prompt

---

## Service URLs

After starting the services, you can access:

- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Frontend**: http://localhost:3000

## Default Credentials

- **Email**: `admin@gatekeeperhq.com`
- **Password**: `Admin123!`

## Stopping Services

### For `start-dev.sh`:
- Press `Ctrl+C` in the terminal where the script is running
- This will stop all services and shut down PostgreSQL

### For `start-dev.ps1` and `start-dev.bat`:
- Close the individual PowerShell/CMD windows for backend and frontend
- To stop PostgreSQL, run: `docker-compose down`

## Troubleshooting

### Docker not running
- Make sure Docker Desktop is running (Windows/Mac)
- Verify Docker is running: `docker ps`

### Port already in use
- Backend (5000): Check if another .NET app is running
- Frontend (3000): Check if another Next.js app is running
- PostgreSQL (5432): Check if local PostgreSQL is running

### Permission errors (Linux/Mac)
- Make scripts executable: `chmod +x scripts/*.sh`

### PowerShell execution policy (Windows)
- Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Backend won't start
- Ensure you're in the `backend` directory
- Check that `GatekeeperHQ.API` project exists
- Verify .NET SDK is installed: `dotnet --version`

### Frontend won't start
- Ensure you're in the `frontend` directory
- Check that `package.json` exists
- Verify Node.js is installed: `node --version`
- Try deleting `node_modules` and running `npm install` again

## Manual Startup

If you prefer to start services manually:

### 1. Start PostgreSQL
```bash
docker-compose up -d
```

### 2. Start Backend
```bash
cd backend
dotnet run --project GatekeeperHQ.API
```

### 3. Start Frontend (in a new terminal)
```bash
cd frontend
npm install
npm run dev
```

## Project Structure

The scripts assume the following directory structure:
```
GatekeeperHQ/
├── backend/          # .NET backend solution
│   └── GatekeeperHQ.API/
├── frontend/         # Next.js frontend
├── docker-compose.yml
└── scripts/          # This folder
```

## Additional Notes

- The scripts automatically install frontend dependencies (`npm install`) before starting
- PostgreSQL runs in detached mode (`-d` flag) so it doesn't block the terminal
- Backend and frontend run in the foreground so you can see their logs
- The scripts wait 3 seconds between starting backend and frontend to allow the API to initialize
