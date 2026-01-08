# Security Setup Guide

This guide explains how to configure security settings for the GatekeeperHQ API.

## Environment Variables

For production deployments, it's **strongly recommended** to use environment variables instead of storing secrets in `appsettings.json`.

### Required Environment Variables

1. **JWT_SECRET_KEY** (Required)
   - Minimum 32 characters long
   - Should be a cryptographically secure random string
   - Example: Generate using `openssl rand -base64 32`
   - **Never commit this to version control**

2. **JWT_ISSUER** (Required)
   - The issuer of the JWT tokens
   - Example: `GatekeeperHQ` or your domain

3. **JWT_AUDIENCE** (Required)
   - The intended audience for JWT tokens
   - Example: `GatekeeperHQ` or your domain

4. **DATABASE_CONNECTION_STRING** (Required for production)
   - PostgreSQL connection string
   - Format: `Host=hostname;Port=5432;Database=dbname;Username=user;Password=password`
   - **Never commit this to version control**

### Optional Environment Variables

- **JWT_EXPIRATION_MINUTES**: Token expiration time in minutes (default: 30)

## Configuration Files

### Development (`appsettings.Development.json`)

For local development, you can use `appsettings.Development.json` which is already in `.gitignore`. However, it's still recommended to use User Secrets for sensitive data.

### Production (`appsettings.Production.json`)

The `appsettings.Production.json` file is excluded from version control. Create this file on your production server with the following structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "SecretKey": "",
    "Issuer": "",
    "Audience": "",
    "ExpirationMinutes": 30
  }
}
```

**Important**: Leave these values empty and use environment variables instead, or fill them only on the production server (never commit).

## Using .NET User Secrets (Development)

For local development, you can use .NET User Secrets to store sensitive configuration:

```bash
cd server/GatekeeperHQ.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "YourSecretKeyHere"
dotnet user-secrets set "Jwt:Issuer" "GatekeeperHQ"
dotnet user-secrets set "Jwt:Audience" "GatekeeperHQ"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=gatekeeperhq;Username=postgres;Password=postgres"
```

## Rate Limiting

Rate limiting is enabled by default and configured in `appsettings.json`:

- **Login endpoint**: 5 requests per 15 minutes per IP
- **All other endpoints**: 100 requests per minute per IP

To disable rate limiting, set `RateLimiting:EnableRateLimiting` to `false` in your configuration.

## Security Headers

The following security headers are automatically added to all responses:

- `X-Content-Type-Options: nosniff` - Prevents MIME type sniffing
- `X-Frame-Options: DENY` - Prevents clickjacking
- `X-XSS-Protection: 1; mode=block` - Enables XSS protection
- `Referrer-Policy: strict-origin-when-cross-origin` - Controls referrer information
- `Content-Security-Policy` - Restricts resource loading
- `Strict-Transport-Security` - Enforces HTTPS (production only)

## HTTPS Enforcement

HTTPS redirection and HSTS are automatically enabled in production environments. Ensure your production server is configured with a valid SSL certificate.

## CORS Configuration

CORS is configured to allow only specific origins. Update the `Cors:AllowedOrigins` array in your configuration file or set it via environment variables for production.

## Password Policy

The following password requirements are enforced:

- Minimum 8 characters
- Maximum 128 characters
- At least one uppercase letter (A-Z)
- At least one lowercase letter (a-z)
- At least one number (0-9)
- At least one special character (@$!%*?&)

## Best Practices

1. **Never commit secrets** to version control
2. **Use environment variables** in production
3. **Rotate JWT secret keys** periodically
4. **Use strong passwords** for database connections
5. **Enable HTTPS** in production
6. **Monitor rate limiting** logs for suspicious activity
7. **Keep dependencies updated** for security patches
8. **Regularly review** security headers configuration

## Troubleshooting

### "JWT SecretKey not configured" Error

- Ensure `JWT_SECRET_KEY` environment variable is set, OR
- Set `Jwt:SecretKey` in `appsettings.json` or `appsettings.Production.json`

### Rate Limiting Too Restrictive

- Adjust `RateLimiting:PermitLimit` and `RateLimiting:Window` in configuration
- Or disable rate limiting by setting `RateLimiting:EnableRateLimiting` to `false`

### CORS Errors

- Verify your frontend origin is included in `Cors:AllowedOrigins`
- Check that the origin matches exactly (including protocol and port)
