# GatekeeperHQ Development Scripts

This folder contains scripts to help you start and manage the GatekeeperHQ development environment.

## Available Scripts

### `start-dev.sh` (Bash/Linux/Mac/Git Bash)

Starts all development services in a single terminal session:
- PostgreSQL database (via Docker Compose)
- .NET Server API
- Next.js Client

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
- .NET Server API (new window)
- Next.js Client (new window)

**Usage:**
```powershell
.\scripts\start-dev.ps1
# or
powershell -ExecutionPolicy Bypass -File .\scripts\start-dev.ps1
```

**Features:**
- Opens separate windows for server and client
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
- .NET Server API (new window)
- Next.js Client (new window)

**Usage:**
```cmd
scripts\start-dev.bat
# or
cd scripts && start-dev.bat
```

**Features:**
- Opens separate windows for server and client
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

- **Server API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Client**: http://localhost:3000

## Default Credentials

- **Email**: `admin@gatekeeperhq.com`
- **Password**: `Admin123!`

## Stopping Services

### For `start-dev.sh`:
- Press `Ctrl+C` in the terminal where the script is running
- This will stop all services and shut down PostgreSQL

### For `start-dev.ps1` and `start-dev.bat`:
- Close the individual PowerShell/CMD windows for server and client
- To stop PostgreSQL, run: `docker-compose down`

## Troubleshooting

### Docker not running
- Make sure Docker Desktop is running (Windows/Mac)
- Verify Docker is running: `docker ps`

### Port already in use
- Server (5000): Check if another .NET app is running
- Client (3000): Check if another Next.js app is running
- PostgreSQL (5432): Check if local PostgreSQL is running

### Permission errors (Linux/Mac)
- Make scripts executable: `chmod +x scripts/*.sh`

### PowerShell execution policy (Windows)
- Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Server won't start
- Ensure you're in the `server` directory
- Check that `GatekeeperHQ.API` project exists
- Verify .NET SDK is installed: `dotnet --version`

### Client won't start
- Ensure you're in the `client` directory
- Check that `package.json` exists
- Verify Node.js is installed: `node --version`
- Try deleting `node_modules` and running `npm install` again

## Manual Startup

If you prefer to start services manually:

### 1. Start PostgreSQL
```bash
docker-compose up -d
```

### 2. Start Server
```bash
cd server
dotnet run --project GatekeeperHQ.API
```

### 3. Start Client (in a new terminal)
```bash
cd client
npm install
npm run dev
```

## Project Structure

The scripts assume the following directory structure:
```
GatekeeperHQ/
├── server/          # .NET server solution
│   └── GatekeeperHQ.API/
├── client/         # Next.js client
├── docker-compose.yml
└── scripts/          # This folder
```

## Additional Notes

- The scripts automatically install client dependencies (`npm install`) before starting
- PostgreSQL runs in detached mode (`-d` flag) so it doesn't block the terminal
- Server and client run in the foreground so you can see their logs
- The scripts wait 3 seconds between starting server and client to allow the API to initialize
