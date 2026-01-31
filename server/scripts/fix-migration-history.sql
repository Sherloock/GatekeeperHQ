-- Fix Migration History
-- Run this SQL script to mark the InitialCreate migration as applied
-- Use this when tables already exist but migration history is missing

-- Create migration history table if it doesn't exist
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Insert migration record if it doesn't exist
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260112144947_InitialCreate', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
