# API Trust Boundaries

## Purpose

This document defines who is allowed to call each API operation and what level of trust is assigned to the data received through that operation.

The existence of an endpoint does not imply that every caller is allowed to invoke it.

The purpose of this document is to prevent:

* privilege escalation;
* unauthorized state transitions;
* financial manipulation;
* inventory corruption;
* trust-boundary violations between services.

---

# Caller Types

The system recognizes the following caller categories.

| Caller Type             | Description                       |
| ----------------------- | --------------------------------- |
| AnonymousUser           | Unauthenticated external caller   |
| Customer                | Authenticated customer            |
| Admin                   | Administrative user               |
| CatalogService          | Internal Catalog Service          |
| CartService             | Internal Cart Service             |
| OrderingService         | Internal Ordering Service         |
| InventoryService        | Internal Inventory Service        |
| PaymentService          | Internal Payment Service          |
| FulfillmentService      | Internal Fulfillment Service      |
| BackgroundWorker        | Internal scheduled process        |
| ExternalPaymentProvider | Verified payment provider webhook |

---

# Endpoint Exposure Levels

## Public

Accessible without authentication.

Examples:

* product catalog browsing
* health checks

Public endpoints must never expose internal state mutation capabilities.

---

## User-Owned

Accessible only to authenticated customers.

The caller must pass ownership validation.

Examples:

* view own cart
* modify own cart
* create order from own cart
* view own orders

Object identifiers alone are not sufficient authorization.

---

## Admin-Only

Accessible only to administrative users.

Examples:

* product management
* inventory management
* warehouse management
* stock receipts
* category administration

---

## Internal-Only

Accessible only to trusted services.

Examples:

* mark-paid
* mark-shipped
* inventory reservation release
* inventory reservation commit

These endpoints must never be exposed to public clients.

---

## Provider Webhook

Accessible only to trusted external providers.

Examples:

* payment success notifications
* payment failure notifications

Provider requests require cryptographic verification.

---

# Catalog Service

## Public Read Operations

### GET /api/products

Allowed callers:

```text
AnonymousUser
Customer
```

Trust assumptions:

```text
No trusted input.
Read-only operation.
```

---

### GET /api/products/{productId}

Allowed callers:

```text
AnonymousUser
Customer
```

Input trust:

```text
productId is selector only.
```

---

### GET /api/products/variants/{productVariantId}/snapshot

Allowed callers:

```text
OrderingService
```

This endpoint exists specifically to establish Catalog authority.

Ordering must treat Catalog as authoritative for:

```text
productId
productVariantId
sku
productName
variantName
priceAmountMinor
currency
```

---

## Admin Operations

### POST /api/products

Allowed callers:

```text
Admin
```

Client authority:

```text
name
description
categoryIds
brandId
slug
```

Not authoritative:

```text
status
createdAt
updatedAt
```

These are server-controlled.

---

### POST /api/products/{id}/publish

Allowed callers:

```text
Admin
```

Must not be publicly callable.

---

### POST /api/products/{id}/archive

Allowed callers:

```text
Admin
```

Must not be publicly callable.

---

# Cart Service

## Create Cart

### POST /api/carts

Allowed callers:

```text
AnonymousUser
Customer
```

Generated server-side:

```text
cartId
createdAt
updatedAt
```

---

## Add Cart Item

### POST /api/carts/{cartId}/items

Allowed callers:

```text
Cart Owner
```

Trusted input:

```text
quantity
```

Untrusted selector:

```text
productVariantId
```

Cart must not trust:

```text
price
sku
productName
currency
stock availability
```

---

## Update Cart Item

### PUT /api/carts/{cartId}/items/{productVariantId}

Allowed callers:

```text
Cart Owner
```

Only quantity may be modified.

---

# Ordering Service

## Create Order

### POST /api/orders

Allowed callers:

```text
Customer
```

Client may provide:

```text
customerName
customerEmail
productVariantId
quantity
```

Client must not provide:

```text
sku
price
currency
lineTotal
total
reservationId
status
```

Ordering must:

```text
Load Catalog snapshot.
Allocate Inventory reservation.
Calculate totals server-side.
```

---

## Mark Paid

### POST /api/orders/{orderId}/mark-paid

Allowed callers:

```text
PaymentService
```

Must never be callable by:

```text
Customer
AnonymousUser
Admin UI
```

Ordering must verify:

```text
Current status is PendingPayment.
```

---

## Mark Shipped

### POST /api/orders/{orderId}/mark-shipped

Allowed callers:

```text
FulfillmentService
```

Ordering must verify:

```text
Current status is Paid.
```

Ordering owns the inventory commit boundary.

---

## Cancel Order

### POST /api/orders/{orderId}/cancel

Allowed callers:

```text
Order Owner
Admin
```

Ownership validation required.

---

## Expire Order

### POST /api/orders/{orderId}/expire

Allowed callers:

```text
BackgroundWorker
```

Must not be customer-controlled.

---

# Inventory Service

Inventory endpoints manipulate integrity-critical state.

Inventory is authoritative for:

```text
onHandQuantity
reservedQuantity
availableQuantity
reservationStatus
```

---

## Stock Receipt

### POST /api/stock/receipts

Allowed callers:

```text
Admin
WarehouseOperator
```

Must never be public.

---

## Allocate Reservation

### POST /api/stock/reservations/allocate

Allowed callers:

```text
OrderingService
```

Inventory chooses:

```text
warehouse
location
reservationId
```

Ordering must not dictate allocation decisions.

---

## Release Reservation

### POST /api/stock/reservations/{id}/release

Allowed callers:

```text
OrderingService
```

Public callers must never release reservations.

---

## Commit Reservation

### POST /api/stock/reservations/{id}/commit

Allowed callers:

```text
OrderingService
```

This operation directly affects stock balances.

Integrity level:

```text
Critical
```

---

# Payment Service

Payment is authoritative for payment attempts.

---

## Create Payment

### POST /api/payments

Current state:

```text
Demo-grade API.
```

Production-grade target:

```text
Customer provides orderId only.
```

Payment Service should obtain:

```text
amount
currency
```

from trusted Ordering data.

---

## Succeed Payment

### POST /api/payments/{paymentId}/succeed

Current state:

```text
Simulation endpoint.
```

Production-grade target:

```text
ExternalPaymentProvider webhook
```

Allowed callers:

```text
ExternalPaymentProvider
```

Required validation:

```text
Signature validation
Replay protection
Idempotency verification
Provider reference verification
```

---

## Fail Payment

### POST /api/payments/{paymentId}/fail

Same requirements as payment success.

---

# Fulfillment Service

Fulfillment owns shipment lifecycle.

---

## Create Shipment

### POST /api/shipments

Allowed callers:

```text
Admin
FulfillmentOperator
BackgroundWorkflow
```

Must validate:

```text
Order exists.
Order status == Paid.
```

---

## Ship Shipment

### POST /api/shipments/{shipmentId}/ship

Allowed callers:

```text
FulfillmentOperator
FulfillmentService
```

Triggers:

```text
Ordering mark-shipped
Inventory commit
```

Integrity level:

```text
Critical
```

---

## Cancel Shipment

### POST /api/shipments/{shipmentId}/cancel

Allowed callers:

```text
FulfillmentOperator
Admin
```

Only pending shipments may be cancelled.

---

# Universal Rules

## Rule 1

Identifiers are selectors, not authorization proof.

---

## Rule 2

Clients never control financial authority.

---

## Rule 3

Clients never control lifecycle status fields.

---

## Rule 4

Clients never control inventory state.

---

## Rule 5

Internal service endpoints must not be exposed publicly.

---

## Rule 6

Cross-service authority must always be explicit.

---

## Rule 7

Every state transition must have an authorized caller.

---

## Open Questions

The following decisions remain unresolved:

1. Authentication mechanism.
2. Service-to-service authentication.
3. Ownership model.
4. Admin role model.
5. Webhook authentication strategy.
6. Idempotency strategy.
7. Audit logging requirements.
