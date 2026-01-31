## gcp — commit + push (current branch)

Generate instructions for committing and pushing the current branch.

Include BOTH:

1. Cursor Command Palette steps (Windows):

- Ctrl+Shift+P → `Git: Stage All Changes`
- Ctrl+Shift+P → `Git: Commit` (ask for a commit message OR provide a placeholder)
- Ctrl+Shift+P → `Git: Push` (or `Git: Push to...` if needed)

2. Equivalent git CLI commands:

```bash
git add -A
git commit -m "<prefix: short lowercase summary>"
git push -u origin HEAD
```

Notes:

- Use the repo's commit message conventions: lowercase summary, prefixes like `fix:`, `feat:`, `chore:`, etc.
- If there are multiple reasonable prefixes, output the best default and mention alternatives briefly.
