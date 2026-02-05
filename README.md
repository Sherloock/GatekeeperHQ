# GatekeeperHQ

A multi-tenant Role-Based Access Control (RBAC) service built with Next.js and ASP.NET Core.

## Features

- **Multi-Tenancy**: Subdomain, API key, or JWT-based tenant resolution
- **Authentication**: JWT tokens with refresh token support
- **Authorization**: Permission-based access control with roles
- **User Management**: CRUD operations with role assignment
- **Role Management**: Create and manage roles with permissions
- **API Keys**: Scoped API keys for service-to-service auth
- **Webhooks**: Subscribe to events (user.created, role.updated, etc.)
- **Invitations**: Invite users to tenants via email tokens
- **TypeScript SDK**: Client SDK for easy integration

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

| Server                | Client          |
| --------------------- | --------------- |
| .NET 8 / ASP.NET Core | Next.js 14      |
| Entity Framework Core | TanStack Query  |
| PostgreSQL            | React Hook Form |
| JWT + API Key Auth    | Zod             |
| BCrypt                | Tailwind CSS    |

## Prerequisites

- .NET SDK 8.0+
- Node.js 18+
- PostgreSQL 15+ (or Docker)
- EF Core Tools: `dotnet tool install --global dotnet-ef`

## Quick Start

### 1. Database

```bash
docker-compose up -d
```

### 2. Server

```bash
cd server
dotnet restore
dotnet ef database update --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API
dotnet run --project GatekeeperHQ.API
```

API: `http://localhost:5000` | Swagger: `http://localhost:5000/swagger`

### 3. Client

```bash
cd client
npm install
npm run dev
```

Client: `http://localhost:3000`

## Default Credentials

- **Email**: `admin@gatekeeperhq.com`
- **Password**: `Admin123!`

To reset admin password:

```powershell
.\server\scripts\reset-admin-password.ps1
```

## Project Structure

```
GatekeeperHQ/
├── client/                     # Next.js frontend
│   ├── app/                    # App Router pages
│   │   ├── (protected)/        # Auth-guarded routes
│   │   │   ├── dashboard/
│   │   │   ├── users/
│   │   │   ├── roles/
│   │   │   └── tenants/
│   │   ├── login/
│   │   └── invite/[token]/     # Invitation acceptance
│   ├── components/             # UI components
│   ├── lib/
│   │   ├── api/                # API client modules
│   │   └── auth/               # Auth hooks & utilities
│   └── types/
├── server/                     # .NET backend
│   ├── GatekeeperHQ.API/       # Controllers, DTOs, Middleware
│   ├── GatekeeperHQ.Application/  # Services, business logic
│   ├── GatekeeperHQ.Domain/    # Entities, constants
│   ├── GatekeeperHQ.Infrastructure/  # EF Core, JWT, seeding
│   └── scripts/                # Database utilities
├── sdks/
│   └── typescript/             # TypeScript client SDK
├── docs/
│   ├── API.md                  # Full API reference
│   └── INTEGRATION.md          # SDK integration guide
└── docker-compose.yml
```

## API Overview

Full API documentation: [docs/API.md](docs/API.md)

| Endpoint         | Description                       |
| ---------------- | --------------------------------- |
| `/auth/*`        | Login, refresh, current user      |
| `/users/*`       | User CRUD (requires permissions)  |
| `/roles/*`       | Role CRUD + permission management |
| `/permissions`   | List available permissions        |
| `/tenants/*`     | Tenant management                 |
| `/api-keys/*`    | API key management                |
| `/webhooks/*`    | Webhook subscriptions             |
| `/invitations/*` | User invitations                  |

### Authentication Methods

```
# JWT Token
Authorization: Bearer <token>

# API Key
X-API-Key: gk_your_api_key
```

## SDK Usage

```typescript
import { GatekeeperHQClient } from "@gatekeeperhq/sdk";

const client = new GatekeeperHQClient({
	baseUrl: "http://localhost:5000/api/v1",
	apiKey: "gk_your_api_key",
	version: "1",
});

const users = await client.users.getAll();
```

See [docs/INTEGRATION.md](docs/INTEGRATION.md) for full SDK documentation.

## Development

### Server (watch mode)

```bash
cd server
dotnet watch run --project GatekeeperHQ.API
```

### Migrations

```bash
# Create
dotnet ef migrations add MigrationName --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API

# Apply
dotnet ef database update --project GatekeeperHQ.Infrastructure --startup-project GatekeeperHQ.API
```

## License

Copyright (c) 2026 Bálint Deák. All rights reserved. See [LICENSE](LICENSE).
