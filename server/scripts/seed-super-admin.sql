-- Seed Super Admin User for GatekeeperHQ
-- Run this in Railway PostgreSQL Query tab after deployment
--
-- IMPORTANT: Change the email and password before running!
--
-- The password hash below is for: "ChangeMe123!"
-- To generate a new BCrypt hash, use one of these options:
--   1. Online: https://bcrypt-generator.com/ (use cost factor 10-12)
--   2. PowerShell: Install-Module -Name BCrypt; [BCrypt.Net.BCrypt]::HashPassword("YourPassword")
--   3. C# Interactive: BCrypt.Net.BCrypt.HashPassword("YourPassword")

-- Pre-generated hash for "ChangeMe123!" (cost factor 11)
-- REPLACE THIS with your own hash before running in production!
INSERT INTO "Users" (
    "TenantId",
    "Email",
    "PasswordHash",
    "IsActive",
    "IsSuperAdmin",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    NULL,                                                           -- TenantId (NULL for Super Admin)
    'admin@example.com',                                            -- Email - CHANGE THIS
    '$2a$11$K8xGq5qV8zJhvQwfY5Og5eJ5S5TZrYmXxqQnXqVwXqVwXqVwXqVwX', -- PasswordHash - CHANGE THIS
    true,                                                           -- IsActive
    true,                                                           -- IsSuperAdmin
    NOW(),                                                          -- CreatedAt
    NOW()                                                           -- UpdatedAt
)
ON CONFLICT ("Email") DO NOTHING;

-- Verify the insert
SELECT "Id", "Email", "IsSuperAdmin", "IsActive", "CreatedAt" 
FROM "Users" 
WHERE "IsSuperAdmin" = true;
