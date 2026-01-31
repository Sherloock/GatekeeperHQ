---
name: react-query-patterns
description: Use when adding/updating react-query queries/mutations, cache keys, invalidation, or optimistic updates.
---

## Checklist

- Centralize query keys (no hardcoded arrays/strings): prefer `lib/constants/*` as `QUERY_KEYS`.
- Keep request functions typed and small; validate network payloads at boundaries if needed.
- Invalidate the smallest scope that is correct (avoid blanket invalidation).
- Show user feedback with toasts (include toast ids).

## Patterns

- Queries:
  - Provide stable keys and stable parameters.
  - Use `select` on the server to keep payloads small.
- Mutations:
  - Prefer optimistic updates only when rollback is safe and well-scoped.
  - On success, invalidate the exact keys impacted.

## Repo references

- API functions: `lib/api/*.ts`
- Auth hooks: `lib/auth/useAuth.tsx`
