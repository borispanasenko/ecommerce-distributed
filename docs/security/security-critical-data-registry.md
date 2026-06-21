# Security-Critical Data Registry

## Purpose

This document identifies data that has elevated security, integrity, operational, or financial significance.

The goal is to make critical data explicit and prevent accidental misuse.

This registry complements:

* data-trust-model.md
* api-trust-boundaries.md

Not all critical data is confidential.

Many fields are publicly visible but remain integrity-critical.

Example:

`priceAmountMinor` is usually visible to customers but must never be trusted from customer input.

---

# Classification Levels

## Critical

Compromise may cause:

* financial loss;
* inventory corruption;
* privilege escalation;
* unauthorized state transitions;
* irreversible business damage.

---

## High

Compromise may cause:

* incorrect business behavior;
* incorrect reporting;
* customer impact;
* operational disruption.

---

## Medium

Compromise affects application behavior but does not directly impact security or financial correctness.

---

## Low

Limited business or security impact.

---

# Financial Data Registry

## Catalog Prices

Fields:

```text
product_variants.price_amount_minor
ProductVariantSnapshotDto.priceAmountMinor
```

Authority:

```text
Catalog Service
```

Classification:

```text
Critical
```

Why:

```text
Incorrect values directly affect customer charges.
```

Trust Rules:

```text
Never trust client-provided prices.
Always load from Catalog authority.
```

---

## Order Unit Price

Fields:

```text
order_items.unit_price_amount_minor
```

Authority:

```text
Ordering snapshot
```

Classification:

```text
Critical
```

Why:

```text
Historical purchase record.
Basis for financial calculations.
```

---

## Order Line Total

Fields:

```text
order_items.line_total_amount_minor
```

Authority:

```text
Ordering
```

Classification:

```text
Critical
```

Trust Rules:

```text
Must always be computed.
```

Never accepted from clients.

---

## Order Total

Fields:

```text
orders.total_amount_minor
OrderDetailsDto.totalAmountMinor
```

Authority:

```text
Ordering
```

Classification:

```text
Critical
```

Trust Rules:

```text
Server-calculated only.
```

---

## Payment Amount

Fields:

```text
payments.amount_minor
CreatePaymentRequest.amountMinor
```

Current State:

```text
Client-supplied in API.
```

Target State:

```text
Derived from trusted order total.
```

Classification:

```text
Critical
```

---

# Authorization Data Registry

## Order Ownership

Fields:

```text
orderId
customer identity
```

Classification:

```text
Critical
```

Risk:

```text
Unauthorized order access.
```

Rule:

```text
orderId is not proof of ownership.
```

---

## Cart Ownership

Fields:

```text
cartId
customer identity
```

Classification:

```text
Critical
```

Risk:

```text
Unauthorized cart modification.
```

---

## Admin Permissions

Fields:

```text
roles
permissions
claims
```

Classification:

```text
Critical
```

Risk:

```text
Privilege escalation.
```

Rule:

```text
Never trust unverified role claims.
```

---

# Authentication Data Registry

## JWT Claims

Future Fields:

```text
sub
role
permissions
aud
iss
exp
```

Classification:

```text
Critical
```

Trust Rules:

```text
Claims trusted only after signature validation.
```

---

## Service Credentials

Examples:

```text
API keys
service tokens
mTLS identities
```

Classification:

```text
Critical
Secret
```

Logging Policy:

```text
Never log.
```

---

# Inventory Data Registry

## On-Hand Quantity

Fields:

```text
stock_items.on_hand_quantity
```

Authority:

```text
Inventory
```

Classification:

```text
Critical
```

Risk:

```text
Overselling.
Inventory corruption.
```

---

## Reserved Quantity

Fields:

```text
stock_items.reserved_quantity
```

Classification:

```text
Critical
```

Risk:

```text
Reservation leakage.
Double allocation.
```

---

## Available Quantity

Fields:

```text
available_quantity
```

Classification:

```text
Critical
```

Type:

```text
Derived
```

Rule:

```text
Never accepted as input.
```

---

## Reservation Status

Fields:

```text
stock_reservations.status
```

Classification:

```text
Critical
```

Risk:

```text
Unauthorized release.
Unauthorized shipment.
Inventory corruption.
```

---

## Inventory Reservation Id

Fields:

```text
inventory_reservation_id
reservationId
```

Classification:

```text
High
```

Rule:

```text
Selector only.
Never treated as authorization proof.
```

---

# Lifecycle State Registry

## Product Status

Fields:

```text
products.status
```

Classification:

```text
High
```

Authority:

```text
Catalog
```

---

## Order Status

Fields:

```text
orders.status
```

Classification:

```text
Critical
```

Risk:

```text
Financial and inventory inconsistencies.
```

---

## Payment Status

Fields:

```text
payments.status
```

Classification:

```text
Critical
```

Risk:

```text
Unauthorized order completion.
```

---

## Shipment Status

Fields:

```text
shipments.status
```

Classification:

```text
Critical
```

Risk:

```text
Unauthorized inventory commit.
Incorrect fulfillment.
```

---

## Reservation Status

Fields:

```text
stock_reservations.status
```

Classification:

```text
Critical
```

---

# External Trust Data Registry

## Payment Provider Reference

Fields:

```text
payments.provider_reference
```

Authority:

```text
External payment provider
```

Classification:

```text
Critical
```

Risk:

```text
Replay attacks.
Duplicate payment processing.
```

Requirements:

```text
Idempotency validation.
Replay protection.
```

---

## Webhook Payloads

Classification:

```text
Critical
```

Trust Rules:

```text
Untrusted until signature verification succeeds.
```

---

# Personally Identifiable Information

## Customer Name

Fields:

```text
orders.customer_name
```

Classification:

```text
Sensitive
```

Logging:

```text
Redact when possible.
```

---

## Customer Email

Fields:

```text
orders.customer_email
```

Classification:

```text
Sensitive
```

Logging:

```text
Redact or partially mask.
```

---

# Audit-Critical Data

The following state transitions must always be auditable:

```text
Product published
Product archived

Order created
Order cancelled
Order expired
Order paid
Order shipped

Reservation allocated
Reservation released
Reservation committed

Payment succeeded
Payment failed

Shipment created
Shipment cancelled
Shipment shipped
```

Future implementation should record:

```text
timestamp
actor
caller type
previous state
new state
correlation id
reason
```

---

# Non-Negotiable Rules

## Rule 1

Money values are integrity-critical.

---

## Rule 2

Status transitions are integrity-critical.

---

## Rule 3

Inventory state is integrity-critical.

---

## Rule 4

Identifiers are selectors, not proof.

---

## Rule 5

External data is untrusted until verified.

---

## Rule 6

Every critical state transition should eventually become auditable.
