# GatekeeperHQ Development Scripts

This folder contains scripts to help you start and manage the GatekeeperHQ development environment.

## Available Scripts

### `start-dev.ps1` (PowerShell - Cross-Platform)

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
- Prevents multiple instances from running simultaneously
- Cross-platform (works on Windows, Linux, and macOS with PowerShell Core)

**Prerequisites:**
- Docker and Docker Compose
- .NET SDK 8.0+
- Node.js 18+
- PowerShell 5.1+ (Windows PowerShell) or PowerShell Core 7+ (cross-platform)

**Note:** If you get an execution policy error on Windows, run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**Troubleshooting:**
- If you see "already running" error, check if a previous instance is still active
- To force remove the lock file: `Remove-Item .start-dev.lock -Force`
- To stop services, close the individual PowerShell windows for server and client
- To stop PostgreSQL: `docker-compose down`

---

### `test-local.ps1` (PowerShell) - Testing Script

Tests if all services are running correctly:
- PostgreSQL database (via Docker)
- .NET Server API
- Next.js Client
- Key API endpoints (login, protected routes)

**Usage:**
```powershell
.\scripts\test-local.ps1
# or
powershell -ExecutionPolicy Bypass -File .\scripts\test-local.ps1
```

**Features:**
- Automated testing of all services
- Checks Docker, PostgreSQL, Backend, Frontend
- Tests login and protected endpoints
- Color-coded output with clear pass/fail indicators

**Prerequisites:**
- All services must be running (use `start-dev.ps1` first)
- PowerShell 5.1+ (Windows PowerShell or PowerShell Core)

**See also:** `TESTING.md` for detailed manual testing guide

---

### `test-local.sh` (Bash) - Testing Script

Same as PowerShell version but for Bash environments (Linux/Mac/Git Bash).

**Usage:**
```bash
./scripts/test-local.sh
# or
bash scripts/test-local.sh
```

**Prerequisites:**
- All services must be running (use `start-dev.ps1` first, or start manually)
- Bash shell (Git Bash on Windows)
- `curl` and `jq` installed (for API testing)

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

### For `start-dev.ps1`:
- Close the individual PowerShell windows for server and client
- To stop PostgreSQL, run: `docker-compose down`

## Troubleshooting

### Docker not running
- Make sure Docker Desktop is running (Windows/Mac)
- Verify Docker is running: `docker ps`

### Port already in use
- Server (5000): Check if another .NET app is running
- Client (3000): Check if another Next.js app is running
- PostgreSQL (5432): Check if local PostgreSQL is running

### PowerShell execution policy (Windows)
- Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Script already running
- If you see "already running" error, check if a previous instance is still active
- To force remove the lock file: `Remove-Item .start-dev.lock -Force`
- Check running processes: `Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*node*"}`

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
```powershell
docker-compose up -d
```

### 2. Start Server
```powershell
cd server
dotnet run --project GatekeeperHQ.API
```

### 3. Start Client (in a new terminal)
```powershell
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
- Server and client run in separate windows so you can see their logs
- The scripts wait 3 seconds between starting server and client to allow the API to initialize
- A lock file (`.start-dev.lock`) prevents multiple instances from running simultaneously