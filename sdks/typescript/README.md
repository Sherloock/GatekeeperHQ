# GatekeeperHQ TypeScript SDK

TypeScript/JavaScript SDK for the GatekeeperHQ Authorization Service.

## Installation

```bash
npm install @gatekeeperhq/sdk
```

## Usage

### Basic Setup

```typescript
import { GatekeeperHQClient } from '@gatekeeperhq/sdk';

const client = new GatekeeperHQClient({
  baseUrl: 'http://localhost:5000',
  version: '1'
});
```

### Authentication

```typescript
// Login
const loginResponse = await client.auth.login({
  email: 'user@example.com',
  password: 'password123'
});

// Set token for subsequent requests
client.setToken(loginResponse.token);

// Get current user
const me = await client.auth.getMe();

// Refresh token
const refreshResponse = await client.auth.refreshToken({
  refreshToken: loginResponse.refreshToken
});
client.setToken(refreshResponse.token);
```

### Using API Key

```typescript
const client = new GatekeeperHQClient({
  baseUrl: 'http://localhost:5000',
  apiKey: 'gk_your_api_key_here',
  version: '1'
});
```

### Users

```typescript
// Get all users
const users = await client.users.getAll();

// Get user by ID
const user = await client.users.getById(1);

// Create user
const newUser = await client.users.create({
  email: 'newuser@example.com',
  password: 'password123',
  isActive: true,
  roleIds: [1]
});

// Update user
const updated = await client.users.update(1, {
  email: 'updated@example.com',
  isActive: false
});

// Delete user
await client.users.delete(1);
```

### Roles

```typescript
// Get all roles
const roles = await client.roles.getAll();

// Create role
const role = await client.roles.create({
  name: 'Editor',
  description: 'Can edit content',
  permissionIds: [1, 2, 3]
});

// Add permission to role
await client.roles.addPermission(role.id, 4);
```

### Webhooks

```typescript
// Create webhook
const webhook = await client.webhooks.create({
  url: 'https://example.com/webhook',
  events: ['user.created', 'user.updated'],
  isActive: true
});
```

## Error Handling

The SDK uses axios and will throw errors for failed requests:

```typescript
try {
  await client.users.getById(999);
} catch (error) {
  if (error.response?.status === 404) {
    console.log('User not found');
  }
}
```

## License

MIT
