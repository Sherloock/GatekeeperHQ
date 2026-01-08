# GatekeeperHQ - RBAC Admin Panel

A complete Role-Based Access Control (RBAC) Admin Panel built with Next.js frontend and ASP.NET Core backend.

## Features

- **Authentication**: JWT-based authentication with secure password hashing
- **Authorization**: Permission-based access control with role management
- **User Management**: Create, read, update, and delete users with role assignment
- **Role Management**: Create and manage roles with permission assignment
- **Permission System**: Fine-grained permission control for features and actions
- **Modern UI**: Clean, responsive admin interface built with Next.js and Tailwind CSS

## Architecture

```
┌─────────────────┐
│  Next.js Client │  (TypeScript, App Router, TanStack Query)
└────────┬────────┘
         │ HTTP/REST + JWT
         ▼
┌─────────────────┐
│  .NET 8 API     │  (ASP.NET Core, JWT Auth, Policy-based AuthZ)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  PostgreSQL DB  │
└─────────────────┘
```

## Tech Stack

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Bearer Authentication
- BCrypt for password hashing
- Swagger/OpenAPI

### Frontend
- Next.js 14 (App Router)
- TypeScript
- TanStack Query
- React Hook Form
- Zod for validation
- Tailwind CSS
- Axios

## Prerequisites

- .NET SDK 8.0 or later
- Node.js 18+ (LTS recommended)
- PostgreSQL 15+ (or Docker)
- EF Core Tools: `dotnet tool install --global dotnet-ef`

## Quick Start

### Option 1: Using Development Scripts (Recommended)

The easiest way to start all services is using the provided scripts:

**Windows (PowerShell):**
```powershell
.\scripts\start-dev.ps1
```

**Windows (Command Prompt):**
```cmd
scripts\start-dev.bat
```

**Linux/Mac/Git Bash:**
```bash
./scripts/start-dev.sh
```

These scripts will automatically:
1. Start PostgreSQL (Docker)
2. Start the Backend API
3. Install frontend dependencies and start the Frontend

See [scripts/README.md](scripts/README.md) for detailed documentation.

---

### Option 2: Manual Setup

### 1. Database Setup

**Option A: Using Docker (Recommended)**
```bash
docker-compose up -d
```

**Option B: Local PostgreSQL**
```bash
createdb gatekeeperhq
```

### 2. Backend Setup

```bash
cd backend

# Restore NuGet packages
dotnet restore

# Update connection string in appsettings.Development.json if needed
# Default: Host=localhost;Port=5432;Database=gatekeeperhq;Username=postgres;Password=postgres

# Run database migrations (EF Core will create the database automatically)
dotnet ef database update --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API

# Run the API
dotnet run --project GatekeeperHQ.API
```

The API will be available at `http://localhost:5000` (or the port configured in `launchSettings.json`).

Swagger UI: `http://localhost:5000/swagger`

### 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install
# or
pnpm install

# Set API URL (optional, defaults to http://localhost:5000/api)
# Create .env.local:
# NEXT_PUBLIC_API_URL=http://localhost:5000/api

# Run the development server
npm run dev
# or
pnpm dev
```

The frontend will be available at `http://localhost:3000`.

## Default Credentials

After seeding the database, you can login with:

- **Email**: `admin@gatekeeperhq.com`
- **Password**: `Admin123!`

This user has the "Admin" role with all permissions.

## Project Structure

```
GatekeeperHQ/
├── backend/
│   ├── GatekeeperHQ.API/          # Presentation layer (Controllers, DTOs)
│   ├── GatekeeperHQ.Application/   # Business logic (Services)
│   ├── GatekeeperHQ.Domain/       # Entities and constants
│   ├── GatekeeperHQ.Infrastructure/ # EF Core, JWT, Password hashing
│   └── GatekeeperHQ.sln           # Solution file
├── frontend/
│   ├── app/                        # Next.js App Router
│   ├── components/                 # UI components
│   ├── features/                   # Feature modules
│   ├── lib/                        # API client, auth utils
│   └── types/                      # TypeScript types
├── docker-compose.yml              # PostgreSQL container
└── README.md
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login (public)
- `GET /api/auth/me` - Get current user with roles/permissions (protected)

### Users
- `GET /api/users` - List users (requires: `users.view`)
- `GET /api/users/{id}` - Get user details (requires: `users.view`)
- `POST /api/users` - Create user (requires: `users.create`)
- `PUT /api/users/{id}` - Update user (requires: `users.edit`)
- `DELETE /api/users/{id}` - Delete user (requires: `users.delete`)

### Roles
- `GET /api/roles` - List roles (requires: `roles.view`)
- `GET /api/roles/{id}` - Get role with permissions (requires: `roles.view`)
- `POST /api/roles` - Create role (requires: `roles.manage`)
- `PUT /api/roles/{id}` - Update role (requires: `roles.manage`)
- `DELETE /api/roles/{id}` - Delete role (requires: `roles.manage`)

### Role Permissions
- `GET /api/roles/{id}/permissions` - Get role permissions (requires: `roles.view`)
- `POST /api/roles/{id}/permissions` - Add permission to role (requires: `roles.manage`)
- `DELETE /api/roles/{id}/permissions/{permissionId}` - Remove permission (requires: `roles.manage`)

### Permissions
- `GET /api/permissions` - List all permissions (requires: `permissions.view`)

## Permissions

The system includes the following permissions:

- `users.view` - View users list and details
- `users.edit` - Edit user information
- `users.delete` - Delete users
- `users.create` - Create new users
- `roles.view` - View roles list and details
- `roles.manage` - Create, edit, and delete roles
- `permissions.view` - View available permissions
- `dashboard.access` - Access dashboard
- `settings.access` - Access settings

## Security Considerations

1. **Password Storage**: Passwords are hashed using BCrypt with salt
2. **JWT Expiry**: Access tokens expire after 30 minutes (configurable)
3. **HTTPS**: Required in production
4. **CORS**: Configured for frontend origin only
5. **Input Validation**: All DTOs validated using Data Annotations
6. **SQL Injection**: EF Core uses parameterized queries (automatic protection)
7. **XSS**: React escapes by default, but user inputs should be sanitized

## Development

### Backend

```bash
cd backend

# Watch mode (auto-reload on changes)
dotnet watch run --project GatekeeperHQ.API

# Create migration
dotnet ef migrations add MigrationName --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API

# Apply migrations
dotnet ef database update --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API
```

### Frontend

```bash
cd frontend

# Development server
npm run dev

# Build for production
npm run build

# Start production server
npm start
```

## Database Migrations

The database is automatically seeded on first run with:
- All permissions
- Admin role (with all permissions)
- User role (basic role)
- Admin user (admin@gatekeeperhq.com / Admin123!)

## Troubleshooting

### Database Connection Issues
- Ensure PostgreSQL is running
- Check connection string in `appsettings.Development.json`
- Verify database exists: `psql -U postgres -l`

### CORS Issues
- Ensure backend CORS is configured for frontend origin
- Check `Program.cs` CORS policy

### JWT Token Issues
- Verify JWT secret key is set in `appsettings.json`
- Check token expiration time
- Ensure frontend is sending token in Authorization header

## License

This project is for demonstration purposes.

## Contributing

This is a portfolio/demo project. Feel free to fork and modify for your own use.
