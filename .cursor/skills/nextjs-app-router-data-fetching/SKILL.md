---
name: nextjs-app-router-data-fetching
description: Use when working on Next.js App Router route handlers, caching, search params, or navigation/redirect patterns.
---

## Checklist

- Route handlers live in `app/api/**/route.ts`.
- Validate inputs (Zod) and enforce auth server-side.
- Prefer minimal payloads; avoid over-fetching.

## Search params rule

- If using `useSearchParams()`, wrap it in a Suspense boundary (separate component + `<Suspense fallback={...}>`) to avoid build/runtime issues.

## Caching / performance

- Use code splitting (`next/dynamic`) for non-critical UI where it improves initial load.
- Keep client components lean; move heavy work server-side when possible.

## Repo references

- API client: `lib/api/client.ts`
- Auth utilities: `lib/auth/`
