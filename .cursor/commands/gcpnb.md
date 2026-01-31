## gcpnb — commit + push (new branch)

Generate instructions for creating a new branch, committing, and pushing it to origin.

Include BOTH:

1. Cursor Command Palette steps (Windows):

- Ctrl+Shift+P → `Git: Create Branch...` (ask for branch name OR provide a placeholder)
- Ctrl+Shift+P → `Git: Stage All Changes`
- Ctrl+Shift+P → `Git: Commit` (ask for a commit message OR provide a placeholder)
- Ctrl+Shift+P → `Git: Publish Branch` (preferred) or `Git: Push`

2. Equivalent git CLI commands:

```bash
git switch -c "<new-branch-name>"
git add -A
git commit -m "<prefix: short lowercase summary>"
git push -u origin HEAD
```

Notes:

- Use the repo's commit message conventions: lowercase summary, prefixes like `fix:`, `feat:`, `chore:`, etc.
