# GatekeeperHQ API Documentation

## Base URL

```
http://localhost:5000/api/v1
```

## Authentication

GatekeeperHQ supports two authentication methods:

### 1. JWT Token Authentication

Include the JWT token in the Authorization header:

```
Authorization: Bearer <token>
```

### 2. API Key Authentication

Include the API key in the X-API-Key header:

```
X-API-Key: gk_your_api_key_here
```

## Multi-Tenancy

Tenants can be resolved via:

1. **Subdomain**: `tenant1.gatekeeperhq.com`
2. **API Key Header**: `X-API-Key: <tenant_api_key>`
3. **JWT Claim**: `tenant_id` claim in JWT token
4. **Query Parameter**: `?tenantId=1` (development only)

## Endpoints

### Authentication

#### POST /auth/login
Login and receive JWT token and refresh token.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64_refresh_token",
  "userId": 1,
  "email": "user@example.com",
  "permissions": ["users.view", "users.create"]
}
```

#### POST /auth/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "base64_refresh_token"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "new_base64_refresh_token"
}
```

#### GET /auth/me
Get current authenticated user information.

**Response:**
```json
{
  "id": 1,
  "email": "user@example.com",
  "isActive": true,
  "roles": ["Admin"],
  "permissions": ["users.view", "users.create"]
}
```

### Users

All user endpoints require authentication and appropriate permissions.

#### GET /users
List all users (requires `users.view` permission).

#### GET /users/{id}
Get user by ID (requires `users.view` permission).

#### POST /users
Create new user (requires `users.create` permission).

**Request:**
```json
{
  "email": "newuser@example.com",
  "password": "password123",
  "isActive": true,
  "roleIds": [1, 2]
}
```

#### PUT /users/{id}
Update user (requires `users.edit` permission).

#### DELETE /users/{id}
Delete user (requires `users.delete` permission).

### Roles

#### GET /roles
List all roles (requires `roles.view` permission).

#### GET /roles/{id}
Get role by ID (requires `roles.view` permission).

#### POST /roles
Create new role (requires `roles.manage` permission).

**Request:**
```json
{
  "name": "Editor",
  "description": "Can edit content",
  "permissionIds": [1, 2, 3]
}
```

#### PUT /roles/{id}
Update role (requires `roles.manage` permission).

#### DELETE /roles/{id}
Delete role (requires `roles.manage` permission).

#### POST /roles/{id}/permissions
Add permission to role (requires `roles.manage` permission).

**Request:**
```json
{
  "permissionId": 1
}
```

#### DELETE /roles/{id}/permissions/{permissionId}
Remove permission from role (requires `roles.manage` permission).

### Webhooks

#### GET /webhooks
List all webhooks.

#### POST /webhooks
Create webhook.

**Request:**
```json
{
  "url": "https://example.com/webhook",
  "secret": "optional_secret",
  "events": ["user.created", "user.updated"],
  "isActive": true
}
```

#### PUT /webhooks/{id}
Update webhook.

#### DELETE /webhooks/{id}
Delete webhook.

### API Keys

#### GET /api-keys
List all API keys.

#### POST /api-keys
Create API key.

**Request:**
```json
{
  "name": "Production API Key",
  "permissions": ["users.view", "users.create"],
  "isActive": true,
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

**Response:**
```json
{
  "id": 1,
  "name": "Production API Key",
  "key": "gk_abc123...", // Only returned on creation
  "permissions": ["users.view", "users.create"],
  "isActive": true,
  "expiresAt": "2025-12-31T23:59:59Z",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### Health Checks

#### GET /health
Basic health check.

#### GET /health/ready
Readiness probe (checks database and Redis).

#### GET /health/live
Liveness probe (checks if application is running).

## Error Responses

All errors follow this format:

```json
{
  "message": "Error description"
}
```

### Status Codes

- `200` - Success
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `409` - Conflict
- `429` - Too Many Requests
- `500` - Internal Server Error

## Rate Limiting

Rate limiting is enabled by default:
- Login endpoint: 5 requests per 15 minutes
- All other endpoints: 100 requests per minute

Rate limit headers:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Remaining requests
- `X-RateLimit-Reset`: Time when limit resets

## Webhook Events

Webhooks are triggered for the following events:

- `user.created` - User created
- `user.updated` - User updated
- `user.deleted` - User deleted
- `role.created` - Role created
- `role.updated` - Role updated
- `role.deleted` - Role deleted
- `permission.granted` - Permission granted to role
- `permission.revoked` - Permission revoked from role

### Webhook Payload

Webhooks are sent as POST requests with the following headers:

- `X-Webhook-Event`: Event name
- `X-Webhook-Signature`: HMAC-SHA256 signature
- `X-Webhook-Timestamp`: Unix timestamp

The signature is computed as: `HMAC-SHA256(secret, timestamp + "." + payload)`

## Versioning

The API is versioned via URL path: `/api/v1/...`

You can also specify version via:
- Query parameter: `?version=1.0`
- Header: `X-API-Version: 1.0`
