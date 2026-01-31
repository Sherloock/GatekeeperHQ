# GatekeeperHQ API Documentation

## Base URL

```
http://localhost:5000/api/v1
```

## Authentication

### JWT Token

```
Authorization: Bearer <token>
```

### API Key

```
X-API-Key: gk_your_api_key_here
```

## Multi-Tenancy

Tenant resolution order:

1. **Subdomain**: `tenant1.gatekeeperhq.com`
2. **API Key Header**: `X-API-Key: <tenant_api_key>`
3. **JWT Claim**: `tenant_id` claim in token
4. **Query Parameter**: `?tenantId=1` (development only)

---

## Endpoints

### Auth

#### POST /auth/login

Public. Returns JWT token and refresh token.

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
	"permissions": ["users.view", "users.create"],
	"isSuperAdmin": false
}
```

#### POST /auth/refresh

Public. Refresh access token.

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

#### POST /auth/revoke

Authenticated. Revoke a refresh token.

**Request:**

```json
{
	"refreshToken": "base64_refresh_token"
}
```

**Response:** `204 No Content`

#### GET /auth/me

Authenticated. Get current user info.

**Response:**

```json
{
	"id": 1,
	"email": "user@example.com",
	"isActive": true,
	"isSuperAdmin": false,
	"roles": ["Admin"],
	"permissions": ["users.view", "users.create"]
}
```

---

### Users

| Method | Endpoint    | Permission     | Description    |
| ------ | ----------- | -------------- | -------------- |
| GET    | /users      | `users.view`   | List all users |
| GET    | /users/{id} | `users.view`   | Get user by ID |
| POST   | /users      | `users.create` | Create user    |
| PUT    | /users/{id} | `users.edit`   | Update user    |
| DELETE | /users/{id} | `users.delete` | Delete user    |

**POST /users Request:**

```json
{
	"email": "newuser@example.com",
	"password": "password123",
	"isActive": true,
	"roleIds": [1, 2]
}
```

---

### Roles

| Method | Endpoint                         | Permission     | Description            |
| ------ | -------------------------------- | -------------- | ---------------------- |
| GET    | /roles                           | `roles.view`   | List all roles         |
| GET    | /roles/{id}                      | `roles.view`   | Get role by ID         |
| POST   | /roles                           | `roles.manage` | Create role            |
| PUT    | /roles/{id}                      | `roles.manage` | Update role            |
| DELETE | /roles/{id}                      | `roles.manage` | Delete role            |
| POST   | /roles/{id}/permissions          | `roles.manage` | Add permission to role |
| DELETE | /roles/{id}/permissions/{permId} | `roles.manage` | Remove permission      |

**POST /roles Request:**

```json
{
	"name": "Editor",
	"description": "Can edit content",
	"permissionIds": [1, 2, 3]
}
```

---

### Permissions

| Method | Endpoint     | Permission           | Description          |
| ------ | ------------ | -------------------- | -------------------- |
| GET    | /permissions | `permissions.view`   | List all permissions |
| POST   | /permissions | `permissions.create` | Create permission    |

---

### Tenants (Super Admin)

| Method | Endpoint                         | Permission       | Description        |
| ------ | -------------------------------- | ---------------- | ------------------ |
| GET    | /tenants                         | `tenants.view`   | List all tenants   |
| GET    | /tenants/{id}                    | `tenants.view`   | Get tenant by ID   |
| POST   | /tenants                         | `tenants.create` | Create tenant      |
| PUT    | /tenants/{id}                    | `tenants.manage` | Update tenant      |
| DELETE | /tenants/{id}                    | `tenants.manage` | Delete tenant      |
| POST   | /tenants/{id}/regenerate-api-key | `tenants.manage` | Regenerate API key |

**POST /tenants Request:**

```json
{
	"name": "Acme Corp",
	"isActive": true
}
```

**Response:**

```json
{
	"id": 1,
	"name": "Acme Corp",
	"apiKey": "gk_abc123...",
	"isActive": true,
	"createdAt": "2026-01-01T00:00:00Z",
	"updatedAt": "2026-01-01T00:00:00Z"
}
```

---

### Invitations

| Method | Endpoint                        | Permission           | Description         |
| ------ | ------------------------------- | -------------------- | ------------------- |
| GET    | /invitations/tenants/{tenantId} | `invitations.manage` | List tenant invites |
| POST   | /invitations/tenants/{tenantId} | `invitations.manage` | Create invitation   |
| DELETE | /invitations/{id}               | `invitations.manage` | Revoke invitation   |
| GET    | /invitations/{token}/validate   | Public               | Validate token      |
| POST   | /invitations/{token}/accept     | Public               | Accept invitation   |

**POST /invitations/tenants/{tenantId} Request:**

```json
{
	"email": "newuser@example.com",
	"roleId": 1
}
```

**GET /invitations/{token}/validate Response:**

```json
{
	"valid": true,
	"email": "newuser@example.com",
	"tenantName": "Acme Corp",
	"roleName": "User",
	"expiresAt": "2026-02-01T00:00:00Z"
}
```

**POST /invitations/{token}/accept Request:**

```json
{
	"password": "newpassword123"
}
```

---

### API Keys

| Method | Endpoint  | Description    |
| ------ | --------- | -------------- |
| GET    | /api-keys | List API keys  |
| POST   | /api-keys | Create API key |

**POST /api-keys Request:**

```json
{
	"name": "Production API Key",
	"permissions": ["users.view", "users.create"],
	"isActive": true,
	"expiresAt": "2026-12-31T23:59:59Z"
}
```

**Response:**

```json
{
	"id": 1,
	"name": "Production API Key",
	"key": "gk_abc123...",
	"permissions": ["users.view", "users.create"],
	"isActive": true,
	"expiresAt": "2026-12-31T23:59:59Z",
	"createdAt": "2026-01-01T00:00:00Z"
}
```

> Note: The `key` field is only returned on creation.

---

### Webhooks

| Method | Endpoint       | Description    |
| ------ | -------------- | -------------- |
| GET    | /webhooks      | List webhooks  |
| POST   | /webhooks      | Create webhook |
| PUT    | /webhooks/{id} | Update webhook |
| DELETE | /webhooks/{id} | Delete webhook |

**POST /webhooks Request:**

```json
{
	"url": "https://example.com/webhook",
	"secret": "optional_secret",
	"events": ["user.created", "user.updated"],
	"isActive": true
}
```

#### Webhook Events

- `user.created`, `user.updated`, `user.deleted`
- `role.created`, `role.updated`, `role.deleted`
- `permission.granted`, `permission.revoked`

#### Webhook Headers

```
X-Webhook-Event: user.created
X-Webhook-Signature: <HMAC-SHA256 signature>
X-Webhook-Timestamp: <unix timestamp>
```

Signature: `HMAC-SHA256(secret, timestamp + "." + payload)`

---

### Health Checks

| Endpoint      | Description           |
| ------------- | --------------------- |
| /health       | Basic health check    |
| /health/ready | Readiness (checks DB) |
| /health/live  | Liveness              |

---

## Permissions Reference

### Tenant-Level

| Permission           | Description              |
| -------------------- | ------------------------ |
| `users.view`         | View users               |
| `users.create`       | Create users             |
| `users.edit`         | Edit users               |
| `users.delete`       | Delete users             |
| `roles.view`         | View roles               |
| `roles.manage`       | Create/edit/delete roles |
| `permissions.view`   | View permissions         |
| `permissions.create` | Create permissions       |
| `permissions.manage` | Edit/delete permissions  |
| `dashboard.access`   | Access dashboard         |
| `settings.access`    | Access settings          |

### Super Admin

| Permission           | Description         |
| -------------------- | ------------------- |
| `tenants.view`       | View tenants        |
| `tenants.create`     | Create tenants      |
| `tenants.manage`     | Edit/delete tenants |
| `invitations.manage` | Manage invitations  |

---

## Error Responses

```json
{
	"message": "Error description"
}
```

| Status | Description    |
| ------ | -------------- |
| 200    | Success        |
| 201    | Created        |
| 204    | No Content     |
| 400    | Bad Request    |
| 401    | Unauthorized   |
| 403    | Forbidden      |
| 404    | Not Found      |
| 409    | Conflict       |
| 500    | Internal Error |

---

## Versioning

API version via URL path: `/api/v1/...`

Alternative methods:

- Query: `?version=1.0`
- Header: `X-API-Version: 1.0`
