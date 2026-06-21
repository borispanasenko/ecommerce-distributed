# Logging and Observability Policy

## Purpose

This document defines which information may appear in:

* application logs;
* audit logs;
* metrics;
* traces;
* error reports;
* operational dashboards.

The goal is to support troubleshooting and observability without exposing sensitive, security-critical, or business-critical data.

Observability systems are not sources of truth.

Logs, metrics, traces, and dashboards must never be used as authoritative business state.

---

# Core Principles

## Principle 1

Observability data is a copy.

Authoritative data remains in service databases and business systems.

---

## Principle 2

If data is not required for troubleshooting, do not emit it.

---

## Principle 3

Identifiers are usually safer than full payloads.

Prefer:

```text
orderId
paymentId
shipmentId
reservationId
productVariantId
```

instead of entire request bodies.

---

## Principle 4

Secrets must never appear in logs.

---

## Principle 5

Observability systems are assumed to have broader access than production databases.

Log accordingly.

---

# Data Classification For Observability

## Safe To Log

Examples:

```text
orderId
paymentId
shipmentId
reservationId
productId
productVariantId
sku
warehouseId
locationId
status transitions
correlationId
traceId
service name
operation name
duration
error code
```

These fields may appear in:

```text
logs
metrics
traces
dashboards
```

---

## Log With Care

Examples:

```text
customerName
customerEmail
trackingNumber
carrier
failureReason
providerReference
```

Allowed:

```text
audit logs
restricted operational logs
```

Prefer:

```text
masking
partial redaction
```

Examples:

```text
john@example.com
→
j***@example.com
```

```text
TRACK123456789
→
TRACK*********
```

---

## Redact

Examples:

```text
JWT payloads
provider webhook payloads
customer addresses
future billing information
future personal identifiers
```

Allowed only in highly restricted audit systems.

Must not appear in normal application logs.

---

## Never Log

Examples:

```text
access tokens
refresh tokens
API keys
service secrets
webhook signing secrets
passwords
private keys
mTLS certificates
```

Never emit.

Never store.

Never serialize.

Never include in exception messages.

---

# Logging Rules By Domain

## Catalog

Safe:

```text
productId
productVariantId
sku
status transition
publish/archive events
```

Avoid logging:

```text
full product payloads
bulk catalog imports
```

---

## Cart

Safe:

```text
cartId
productVariantId
quantity
```

Avoid:

```text
full cart snapshots
```

Large cart payloads should not be repeatedly emitted.

---

## Ordering

Safe:

```text
orderId
item count
status
reservation ids
```

Do not log:

```text
full order payload
customer details
```

unless explicitly required.

---

## Inventory

Safe:

```text
sku
reservationId
warehouseId
locationId
movement type
```

Use caution:

```text
onHandQuantity
reservedQuantity
availableQuantity
```

These values may be logged for diagnostics but should not be excessively exposed.

---

## Payment

Safe:

```text
paymentId
orderId
status
provider
```

Restricted:

```text
providerReference
failureReason
```

Never log:

```text
future payment credentials
provider secrets
```

---

## Fulfillment

Safe:

```text
shipmentId
orderId
status
```

Restricted:

```text
trackingNumber
carrier
```

---

# Structured Logging

All logs should be structured.

Preferred:

```json
{
  "service": "Ordering",
  "operation": "CreateOrder",
  "orderId": "...",
  "status": "PendingPayment",
  "durationMs": 42
}
```

Avoid:

```text
Created order 123 for John Doe john@example.com with total 13800
```

---

# Audit Logging

Audit logs are separate from operational logs.

Audit logs exist to answer:

```text
Who changed what?
When?
Why?
From where?
```

---

## Mandatory Audit Events

Catalog:

```text
Product created
Product published
Product archived
Variant created
Price changed
```

Inventory:

```text
Stock receipt
Stock adjustment
Reservation created
Reservation released
Reservation committed
```

Ordering:

```text
Order created
Order cancelled
Order expired
Order paid
Order shipped
```

Payment:

```text
Payment created
Payment succeeded
Payment failed
```

Fulfillment:

```text
Shipment created
Shipment cancelled
Shipment shipped
```

---

## Audit Event Structure

Future audit records should contain:

```text
eventId
timestamp
actorType
actorId
service
operation
entityType
entityId
previousState
newState
reason
correlationId
```

---

# Metrics Policy

Metrics must not contain:

```text
email
customer name
tracking number
provider reference
addresses
```

Metrics should contain:

```text
counts
durations
error rates
state transition rates
inventory levels (aggregated)
```

Examples:

```text
orders_created_total
orders_paid_total
orders_expired_total

payments_succeeded_total
payments_failed_total

inventory_reservations_active
inventory_commit_failures_total
```

---

# Tracing Policy

Distributed traces may contain:

```text
orderId
paymentId
shipmentId
reservationId
correlationId
```

Traces must not contain:

```text
secrets
tokens
keys
credentials
```

Large payload bodies should not be attached to spans.

---

# Error Handling Policy

Error responses returned to clients must not expose:

```text
stack traces
connection strings
SQL queries
credentials
internal service URLs
```

Allowed:

```text
error code
error message
correlation id
```

Example:

```json
{
  "error": "ORDER_NOT_FOUND",
  "message": "Order not found",
  "correlationId": "..."
}
```

---

# Correlation And Traceability

Every externally initiated request should eventually have:

```text
correlationId
traceId
```

These identifiers should be propagated across:

```text
Catalog
Ordering
Inventory
Payment
Fulfillment
```

This allows incident investigation without exposing business data.

---

# Non-Negotiable Rules

## Rule 1

Never log secrets.

---

## Rule 2

Prefer identifiers over payloads.

---

## Rule 3

Operational logs and audit logs serve different purposes.

---

## Rule 4

Customer data should be minimized and masked.

---

## Rule 5

Observability systems are not sources of truth.

---

## Rule 6

Every critical state transition should eventually become auditable.

---

## Rule 7

Every cross-service workflow should be traceable through correlation identifiers.
