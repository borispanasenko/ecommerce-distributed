# Reliability Strategy

This document describes retry and idempotency rules for service-to-service communication.

The current system still uses synchronous HTTP calls between services. Before adding RabbitMQ, outbox/inbox or full choreography-based sagas, lifecycle commands should be safe under retries, timeouts and partial failures.

## Goals

```text
Avoid double stock changes.
Avoid invalid state after partial failure.
Allow safe retry of service-to-service commands.
Keep ownership boundaries clear.
Avoid adding global idempotency infrastructure too early.
```

## Current communication model

```text
Ordering -> Catalog
Ordering -> Inventory
Payment -> Ordering
Fulfillment -> Ordering
```

Current critical lifecycle commands:

```text
Payment -> Ordering mark-paid
Fulfillment -> Ordering mark-shipped
Ordering -> Inventory release reservation
Ordering -> Inventory commit reservation
```

## State-based idempotency

The current strategy is state-based idempotency.

A command can safely return success when the requested target state has already been reached.

This is useful when:

```text
The first call succeeded.
The response was lost because of timeout or network failure.
The caller retries the same command.
```

The retry should not create duplicate side effects.

## Inventory reservation commands

Inventory owns stock and reservation state.

Reservation lifecycle:

```text
Active -> Released
Active -> Committed
```

Retry policy:

```text
Release Active reservation      -> success, release reserved stock
Release already Released        -> success, no-op
Release already Committed       -> failure

Commit Active reservation       -> success, commit reserved stock
Commit already Committed        -> success, no-op
Commit already Released         -> failure
```

No-op success must not create a second stock movement.

No-op success must not change `on_hand_quantity` or `reserved_quantity`.

## Ordering lifecycle commands

Ordering owns order lifecycle.

Order lifecycle:

```text
PendingPayment -> Paid -> Shipped
PendingPayment -> Cancelled
PendingPayment -> Expired
```

Retry policy:

```text
Mark-paid PendingPayment order  -> success, status becomes Paid
Mark-paid already Paid order    -> success, no-op
Mark-paid already Shipped order -> success, no-op
Mark-paid Cancelled order       -> failure

Expire PendingPayment order     -> success, Inventory reservation is released
Expire already Expired          -> success, no-op
Expire Paid order               -> failure
Expire Shipped order            -> failure
Expire Cancelled order          -> failure

Mark-shipped Paid order         -> success, Inventory reservation is committed
Mark-shipped already Shipped    -> success, no-op
Mark-shipped PendingPayment     -> failure
Mark-shipped Cancelled          -> failure
```

`Mark-paid already Shipped` can be treated as success because a Shipped order has already passed through the Paid lifecycle state.

`Mark-shipped already Shipped` can be treated as success because the target state has already been reached.

Expired means the order was not paid in time and its Inventory reservation has been released.

## Automatic order expiration

Ordering API runs a background worker that periodically expires old unpaid orders.

The worker scans PostgreSQL for `PendingPayment` orders older than the configured payment timeout and calls the same retry-safe expiration command used by the manual expire endpoint.

```text
OrderExpiration.Enabled
OrderExpiration.PaymentTimeoutMinutes
OrderExpiration.ScanIntervalSeconds
OrderExpiration.BatchSize
```

The worker does not own business state. PostgreSQL remains the source of truth for order status.

If Inventory reservation release fails, the order remains `PendingPayment` and can be retried by a later worker scan.

Redis TTL is not used as the source of truth for order expiration.

## Payment commands

Payment owns payment state.

For now, public/manual payment commands may keep strict transition behavior:

```text
Succeed Pending payment         -> success
Succeed already Succeeded       -> failure for now
Fail Pending payment            -> success
Fail already Failed             -> failure for now
```

Future payment provider webhook handling may need idempotency keys or provider reference based idempotency.

## Fulfillment commands

Fulfillment owns shipment lifecycle.

For now, public/manual shipment commands may keep strict transition behavior:

```text
Ship Pending shipment           -> success
Ship already Shipped shipment   -> failure for now
Cancel Pending shipment         -> success
Cancel already Cancelled        -> failure for now
```

The important current retry case is handled through Ordering:

```text
If Fulfillment calls Ordering mark-shipped and the response is lost,
a retry can call mark-shipped again.
Ordering should return success if the order is already Shipped.
```

## HTTP client resilience

All service-to-service HTTP clients use explicit timeouts.

Network failures, downstream unavailability, request timeouts and invalid JSON responses are mapped to controlled client result failures instead of leaking raw HTTP exceptions into application services.

Automatic retries are not enabled globally.

Retries should only be added for commands that are already retry-safe or idempotent.

Current retry-safe commands:

```text
Ordering expire order
Payment -> Ordering mark-paid
Fulfillment -> Ordering mark-shipped
Ordering -> Inventory release reservation
Ordering -> Inventory commit reservation
```

Current commands without automatic retry:

```text
Ordering -> Inventory allocate reservation
Create order
Create payment
Create shipment
```

Inventory allocation is not retried automatically because allocation is not idempotent yet.

## Future work

```text
Add selective retry policies for retry-safe HTTP commands. Tune timeout values per downstream dependency if needed. Add idempotency keys for non-idempotent external commands where needed. Add outbox/inbox before publishing business events through RabbitMQ. Add choreography-based saga after lifecycle commands are retry-safe.
```

## Guiding rule

```text
PostgreSQL owns facts.
HTTP commands change facts for now.
Future RabbitMQ messages will move facts.
Redis may accelerate or expire temporary state, but must not own critical facts.
```
