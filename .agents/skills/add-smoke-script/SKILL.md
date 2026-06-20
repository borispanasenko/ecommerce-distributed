---
name: add-smoke-script
description: Use when adding or updating a shell smoke script for cross-service behavior in this repository.
---

# Add Smoke Script

Use this skill when adding or updating a smoke script under `scripts/smoke`.

## Rules

Smoke scripts should use:

```bash
set -euo pipefail
```

Require common tools explicitly, usually `curl` and `jq`.

Support environment variable overrides for service URLs and test data.

Print important IDs and state transitions.

Assert expected business states, not just HTTP success.

Prefer flows that finish in terminal states.

Avoid leaving dangling Inventory reservations.

## Documentation

When adding a new smoke script, update `docs/local-development.md`.

Document:

- script command
- behavior covered
- important side effects
- required services

## Verification

Run the new or changed smoke script.

Run related smoke scripts if the change affects shared lifecycle behavior.

Before committing, check:

```bash
git diff --check
git status --short
```
