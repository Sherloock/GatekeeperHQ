## ship — iteratively fix errors, run tests, and build

Iteratively fix errors, run tests, and build until the project passes all checks. Use when user provides errors or when fixing compilation/build issues.

## Workflow

1. **Receive errors**: User provides errors (compilation, lint, test, or build errors)
2. **Fix errors**: Analyze and fix all provided errors
3. **Run lint**: Execute lint checks to verify fixes
4. **Build project**: Execute build to verify build passes
5. **Iterate**: If errors remain, repeat steps 2-4 until all pass

## Execution order

1. Fix all provided errors in code
2. Run `npm run lint` in client/ (capture output)
3. If lint fails, fix lint errors and repeat
4. Run `npm run build` in client/ (capture output)
5. If build fails, fix build errors and repeat
6. Run `dotnet build` in server/ (capture output)
7. If build fails, fix build errors and repeat
8. Continue until all builds pass

## Error sources to check

- TypeScript compilation errors (Next.js build)
- ESLint errors (`npm run lint`)
- C# compilation errors (`dotnet build`)
- Runtime errors (if applicable)

## Best practices

- Fix errors systematically (one type at a time if possible)
- Read linter errors using `read_lints` tool
- Verify fixes by running the appropriate command
- If errors persist after multiple iterations, ask user for clarification

## Commands reference

- Frontend lint: `npm run lint` (in client/)
- Frontend build: `npm run build` (in client/)
- Backend build: `dotnet build` (in server/)

## Success criteria

- Frontend build succeeds (`npm run build` exits with code 0)
- Backend build succeeds (`dotnet build` exits with code 0)
- No compilation errors
- No linting errors (or only acceptable warnings)
