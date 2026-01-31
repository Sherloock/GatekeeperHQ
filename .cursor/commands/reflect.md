## reflect — analyze what went wrong and generate recommendations for .cursor/rules/

# Task Reflection and Rule Generation

Analyze the previous task/interaction and provide:

## 1. What Went Wrong

- Identify mistakes, misunderstandings, or suboptimal approaches
- Be specific about what didn't work and why
- Include code issues, logic errors, or misaligned solutions

## 2. What Wasn't Obvious Previously

- Context or requirements that emerged during execution
- Assumptions that proved incorrect
- Edge cases, constraints, or dependencies discovered late
- Missing information that would have helped earlier

## 3. Recommended Rules for .cursor/rules/

**CRITICAL: Only generate general, concise rules that apply broadly across the codebase.**

- **Prefer recommendations over rules**: If a specific rule would be too narrow or one-off, provide a recommendation instead
- **General applicability**: Rules must be reusable across multiple contexts, not tied to a single component or specific scenario
- **Conciseness**: Keep rules brief and focused on essential principles
- **Avoid over-specificity**: If a rule only applies to one file or one edge case, it's not general enough—provide a recommendation instead

**When to create a rule vs. recommendation:**

- ✅ **Rule**: Applies to multiple files/components, establishes a pattern, prevents recurring mistakes
- ❌ **Recommendation**: One-off fix, specific to a single component, too narrow in scope

**Format for rules:**

```
// [Category/Context]
- When [situation/trigger], always [specific action]
- Before [task type], verify [specific requirement]
- For [technology/framework], prefer [pattern/approach] because [reason]
- Never [anti-pattern] without [condition/safeguard]
```

**Output Structure:**

- Provide rule content in MDC format with proper frontmatter (description, globs)
- Suggest a descriptive filename (e.g., `130-general-pattern-name.mdc`)
- Include rationale for why this rule prevents future issues
- Keep rules general, concise, and broadly applicable

## 4. Action Items

- **For general rules**: Ask "Should I save this as `.cursor/rules/[suggested-name].mdc`?"
- **For recommendations**: Present as guidance without creating a rule file
- If approved, create the rule file immediately
- Update relevant documentation if the rule affects existing patterns
- **Remember**: Only create rules that are general and concise enough to be useful across the codebase
