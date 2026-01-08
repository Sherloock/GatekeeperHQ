# Local Testing Guide

This guide helps you verify that all services in GatekeeperHQ are running correctly.

## Quick Test

Run the automated test script:

**PowerShell (Recommended):**
```powershell
.\scripts\test-local.ps1
```

**Bash (Linux/Mac/Git Bash):**
```bash
./scripts/test-local.sh
```

This script checks:
- ✅ Docker installation
- ✅ PostgreSQL container status
- ✅ Backend API availability
- ✅ Frontend availability
- ✅ Login endpoint functionality
- ✅ Protected endpoint authentication
- ✅ Database connectivity

## Manual Testing Steps

### 1. Start All Services

```powershell
.\scripts\start-dev.ps1
```

Wait for all services to start (about 10-15 seconds).

### 2. Check Service URLs

Open these URLs in your browser:

#### Backend API
- **Swagger UI**: http://localhost:5000/swagger
  - Should show the API documentation
  - All endpoints should be listed

#### Frontend
- **Client**: http://localhost:3000
  - Should load the login page
  - No console errors

### 3. Test Database Connection

Check if PostgreSQL is running:

```powershell
docker ps --filter "name=gatekeeperhq-db"
```

Should show the container with status "Up".

### 4. Test Login

#### Using Swagger UI
1. Go to http://localhost:5000/swagger
2. Find `POST /api/auth/login`
3. Click "Try it out"
4. Use these credentials:
   ```json
   {
     "email": "admin@gatekeeperhq.com",
     "password": "Admin123!"
   }
   ```
5. Click "Execute"
6. Should return a JWT token

#### Using PowerShell
```powershell
$body = @{
    email = "admin@gatekeeperhq.com"
    password = "Admin123!"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/login" `
    -Method Post -Body $body -ContentType "application/json"

$token = ($response.Content | ConvertFrom-Json).token
Write-Host "Token: $token"
```

#### Using curl (Git Bash)
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gatekeeperhq.com","password":"Admin123!"}'
```

### 5. Test Protected Endpoints

After getting a token, test a protected endpoint:

#### Using Swagger UI
1. Click the "Authorize" button (top right)
2. Enter: `Bearer YOUR_TOKEN_HERE`
3. Try `GET /api/auth/me` or `GET /api/users`

#### Using PowerShell
```powershell
$headers = @{
    "Authorization" = "Bearer $token"
}

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/auth/me" `
    -Method Get -Headers $headers

$user = $response.Content | ConvertFrom-Json
Write-Host "User: $($user.email)"
Write-Host "Permissions: $($user.permissions -join ', ')"
```

#### Using curl
```bash
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 6. Test Frontend Login

1. Go to http://localhost:3000
2. Enter credentials:
   - Email: `admin@gatekeeperhq.com`
   - Password: `Admin123!`
3. Click "Login"
4. Should redirect to dashboard
5. Check browser console (F12) for errors

### 7. Test Frontend Features

After logging in, test:
- ✅ View users list
- ✅ View roles list
- ✅ View permissions
- ✅ Create/edit/delete operations (if you have permissions)

## Common Issues

### Backend Not Responding

**Symptoms:**
- Swagger UI doesn't load
- 5000 port connection refused

**Solutions:**
1. Check if server window is open and running
2. Check for errors in server window
3. Verify .NET SDK: `dotnet --version`
4. Check if port 5000 is in use: `netstat -ano | findstr :5000`

### Frontend Not Responding

**Symptoms:**
- localhost:3000 doesn't load
- Connection refused

**Solutions:**
1. Check if client window is open and running
2. Check for errors in client window
3. Verify Node.js: `node --version`
4. Check if port 3000 is in use: `netstat -ano | findstr :3000`
5. Try: `cd client && npm install && npm run dev`

### Database Connection Failed

**Symptoms:**
- API returns 500 errors
- "Connection refused" in server logs

**Solutions:**
1. Check Docker is running: `docker ps`
2. Check PostgreSQL container: `docker ps --filter "name=gatekeeperhq-db"`
3. Start PostgreSQL: `docker-compose up -d`
4. Check connection string in `server/GatekeeperHQ.API/appsettings.json`

### Login Fails

**Symptoms:**
- 401 Unauthorized
- "Invalid email or password"

**Solutions:**
1. Verify credentials: `admin@gatekeeperhq.com` / `Admin123!`
2. Check if database was seeded (should happen on first startup)
3. Check server logs for errors
4. Verify JWT secret is configured

### CORS Errors

**Symptoms:**
- Browser console shows CORS errors
- Frontend can't call API

**Solutions:**
1. Verify CORS is configured in `Program.cs`
2. Check API URL in frontend: should be `http://localhost:5000`
3. Check frontend URL: should be `http://localhost:3000`

## Expected Test Results

When everything is working correctly:

✅ **Docker**: Running
✅ **PostgreSQL**: Container up, port 5432 accessible
✅ **Backend API**: Responding on http://localhost:5000
✅ **Swagger UI**: Accessible at http://localhost:5000/swagger
✅ **Frontend**: Responding on http://localhost:3000
✅ **Login**: Returns JWT token
✅ **Protected Endpoints**: Work with JWT token
✅ **Database**: Can query users, roles, permissions

## Performance Checks

### Response Times
- Login: < 500ms
- Get users: < 300ms
- Get roles: < 300ms
- Frontend load: < 2s

### Resource Usage
- PostgreSQL container: ~50-100MB RAM
- .NET API: ~100-200MB RAM
- Next.js dev server: ~200-300MB RAM

## Next Steps

After verifying everything works:

1. **Explore Swagger UI**: Test all endpoints
2. **Test Frontend**: Try all features
3. **Check Logs**: Monitor server and client windows
4. **Test Permissions**: Verify RBAC is working
5. **Create Test Data**: Add users, roles, permissions

## Automated Testing

For continuous testing, you can:

1. Run the test script periodically:
   ```powershell
   .\scripts\test-local.ps1
   ```

2. Add to CI/CD pipeline (when ready)

3. Create integration tests (recommended for production)

## Troubleshooting Commands

```powershell
# Check all services
docker ps
Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*node*"}

# Check ports
netstat -ano | findstr ":5000"
netstat -ano | findstr ":3000"
netstat -ano | findstr ":5432"

# Check logs
docker logs gatekeeperhq-db
# (Server and client logs are in their respective windows)

# Restart services
docker-compose restart
# (Restart server/client windows manually)
```

## Need Help?

If tests fail:
1. Check the error messages
2. Review the troubleshooting section
3. Check server/client logs
4. Verify all prerequisites are installed
5. Try restarting all services
