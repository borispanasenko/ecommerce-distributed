---
name: change-ready-check
description: Use before committing a repository change to verify status, formatting, relevant tests, smoke coverage and staged file modes.
---

# Change Ready Check

Use this skill before committing a focused repository change.

## Checklist

Review the diff:

```bash
git diff
git diff --cached
```

Check formatting and whitespace:

```bash
git diff --check
```

Check repository status:

```bash
git status --short
```

Run relevant unit tests for changed services.

For cross-service lifecycle or reliability changes, run the relevant smoke scripts.

For new source files on external drives, verify Git file modes:

```bash
git diff --cached --summary
git ls-files -s <path>
```

Source files should normally be committed as `100644`, not `100755`.

Commit only focused, related changes together.
