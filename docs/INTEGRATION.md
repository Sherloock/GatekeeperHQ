# GatekeeperHQ Integration Guide

This guide explains how to integrate GatekeeperHQ into your application.

## Quick Start

### 1. Install the SDK

```bash
npm install @gatekeeperhq/sdk
```

### 2. Initialize the Client

```typescript
import { GatekeeperHQClient } from '@gatekeeperhq/sdk';

const client = new GatekeeperHQClient({
  baseUrl: 'https://api.gatekeeperhq.com',
  version: '1'
});
```

### 3. Authenticate

#### Option A: User Login

```typescript
const response = await client.auth.login({
  email: 'user@example.com',
  password: 'password123'
});

client.setToken(response.token);
```

#### Option B: API Key

```typescript
const client = new GatekeeperHQClient({
  baseUrl: 'https://api.gatekeeperhq.com',
  apiKey: 'gk_your_api_key_here',
  version: '1'
});
```

## Multi-Tenancy

### Using Subdomain

If your GatekeeperHQ instance supports subdomain-based tenant resolution:

```typescript
const client = new GatekeeperHQClient({
  baseUrl: 'https://yourtenant.gatekeeperhq.com',
  apiKey: 'your_api_key',
  version: '1'
});
```

### Using API Key

The API key automatically resolves the tenant:

```typescript
const client = new GatekeeperHQClient({
  baseUrl: 'https://api.gatekeeperhq.com',
  apiKey: 'gk_tenant_specific_key',
  version: '1'
});
```

## Common Patterns

### Token Refresh

```typescript
let token = loginResponse.token;
let refreshToken = loginResponse.refreshToken;

// Before making requests, check if token is expired
// If expired, refresh it
try {
  await client.users.getAll();
} catch (error) {
  if (error.response?.status === 401) {
    const refreshResponse = await client.auth.refreshToken({ refreshToken });
    token = refreshResponse.token;
    refreshToken = refreshResponse.refreshToken;
    client.setToken(token);
  }
}
```

### Error Handling

```typescript
try {
  const user = await client.users.getById(1);
} catch (error) {
  if (error.response) {
    switch (error.response.status) {
      case 401:
        // Unauthorized - token expired or invalid
        break;
      case 403:
        // Forbidden - insufficient permissions
        break;
      case 404:
        // Not found
        break;
      default:
        // Other error
    }
  }
}
```

### Webhook Verification

```typescript
import crypto from 'crypto';

function verifyWebhookSignature(
  payload: string,
  signature: string,
  timestamp: string,
  secret: string
): boolean {
  const message = `${timestamp}.${payload}`;
  const expectedSignature = crypto
    .createHmac('sha256', secret)
    .update(message)
    .digest('hex');

  return crypto.timingSafeEqual(
    Buffer.from(signature),
    Buffer.from(expectedSignature)
  );
}

// In your webhook endpoint
app.post('/webhook', (req, res) => {
  const signature = req.headers['x-webhook-signature'];
  const timestamp = req.headers['x-webhook-timestamp'];
  const payload = JSON.stringify(req.body);

  if (verifyWebhookSignature(payload, signature, timestamp, webhookSecret)) {
    // Process webhook
    const event = req.headers['x-webhook-event'];
    // Handle event
  } else {
    res.status(401).send('Invalid signature');
  }
});
```

## Best Practices

1. **Store tokens securely**: Never commit tokens to version control
2. **Handle token expiration**: Implement automatic token refresh
3. **Verify webhook signatures**: Always verify webhook signatures
4. **Use API keys for service-to-service**: Use API keys instead of user tokens for background jobs
5. **Cache permissions**: Cache user permissions to reduce API calls
6. **Handle rate limits**: Implement retry logic with exponential backoff

## Examples

See the `examples/` directory for complete integration examples:
- `examples/nodejs/` - Node.js integration
- `examples/webhook-receiver/` - Webhook receiver example
