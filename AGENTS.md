# AGENTS.md

## Project Context

This repository is a small ecommerce distributed system built with .NET 8 microservices, PostgreSQL and Docker Compose.

The current architecture uses synchronous HTTP between services. RabbitMQ, outbox/inbox and choreography-based sagas are planned later.

## Core Rules

PostgreSQL owns business facts.

HTTP commands change facts for now.

Redis may accelerate temporary state, but must not own critical business facts.

Future RabbitMQ messages will carry facts, not own facts.

Services must not write another service's database directly.

## Service Ownership

Ordering owns order lifecycle.

Inventory owns stock and reservation state.

Payment owns payment state.

Fulfillment owns shipment lifecycle.

## Reliability Rules

Do not add automatic retries globally.

Add retries only for retry-safe or idempotent commands.

Do not retry create/allocation calls unless they have an idempotency key.

Map infrastructure failures to controlled result failures.

Do not retry business errors such as validation failures, 400, 404 and invalid state transitions.

See `docs/reliability.md` for detailed retry and idempotency rules.

## Lifecycle Change Checklist

When changing cross-service lifecycle behavior, check whether the change needs:

* domain model update
* application service update
* infrastructure/client update
* endpoint update
* unit tests
* smoke script update
* `docs/reliability.md` update
* service database docs update
* `docs/local-development.md` update

## Documentation

Update docs in the same commit as behavior changes when the docs describe that behavior.

Use `docs/reliability.md` for cross-service reliability and idempotency rules.

Use service database docs for persisted state, status meanings and service-owned tables.

Use `docs/local-development.md` for smoke script usage.

## Git Hygiene

Keep commits small and focused.

Run relevant unit tests before committing.

Run relevant smoke scripts for cross-service lifecycle changes.

Before committing, check:

```bash
git diff --check
git status --short
```
