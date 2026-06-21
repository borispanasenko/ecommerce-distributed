# Data Classification Model

## Purpose

This document defines the data classification model for the platform.

The objective is to classify data not only by confidentiality, but also by:

* ownership;
* trustworthiness;
* integrity requirements;
* security relevance;
* financial relevance;
* lifecycle impact;
* logging restrictions.

This document acts as the central data inventory of the system.

Related documents:

* domain-invariants.md
* service-authority-matrix.md
* data-trust-model.md
* api-trust-boundaries.md
* authorization-model.md
* security-critical-data-registry.md
* logging-observability-policy.md

---

# Classification Dimensions

## Confidentiality

### Public

May be visible to external users.

Examples:

```text
Product Name
Category Name
Brand Name
Product Price
SKU
```

---

### Internal

Not intended for public exposure.

Examples:

```text
Inventory Reservation Id
Warehouse Code
Stock Movement Records
```

---

### Sensitive

Contains customer or operational information.

Examples:

```text
Customer Name
Customer Email
Tracking Number
```

---

### Secret

Exposure creates immediate security risk.

Examples:

```text
API Keys
JWT Signing Keys
Webhook Secrets
Service Credentials
```

---

# Trust Classification

## Client-Controlled

Provided directly by the caller.

Examples:

```text
productVariantId
quantity
cartId
orderId
paymentId
shipmentId
```

Client-controlled data is never automatically trusted.

---

## User-Controlled

Provided by a legitimate user.

Examples:

```text
customerName
customerEmail
carrier
trackingNumber
```

Must be validated.

---

## External-System-Controlled

Originates from a third-party system.

Examples:

```text
providerReference
paymentWebhookPayload
```

Trusted only after verification.

---

## Service-Controlled

Owned and managed by a service.

Examples:

```text
orderStatus
paymentStatus
shipmentStatus
reservationStatus
```

---

## Database-Authoritative

Persisted authoritative business state.

Examples:

```text
product price
stock balance
reservation quantity
```

---

## Derived

Computed from authoritative data.

Examples:

```text
order total
line total
available quantity
```

Never accepted as input.

---

# Integrity Classification

## Low

Incorrect value has minimal business impact.

---

## Medium

Incorrect value causes workflow disruption.

---

## High

Incorrect value affects important business processes.

---

## Critical

Incorrect value can cause:

* financial loss;
* inventory corruption;
* privilege escalation;
* unauthorized state transitions.

---

# Logging Classification

## Safe

May appear in logs.

Examples:

```text
orderId
paymentId
shipmentId
reservationId
sku
status
```

---

## Masked

May appear only partially.

Examples:

```text
customerEmail
trackingNumber
```

---

## Restricted

Allowed only in restricted operational logs.

Examples:

```text
providerReference
failureReason
```

---

## Never Log

Examples:

```text
access tokens
refresh tokens
API keys
service credentials
private keys
```

---

# Catalog Service Data Inventory

## Product

| Property        | Value                  |
| --------------- | ---------------------- |
| Confidentiality | Public                 |
| Authority       | Catalog                |
| Trust           | Database-Authoritative |
| Integrity       | High                   |
| Logging         | Safe                   |

Fields:

```text
id
name
slug
description
status
```

---

## Product Variant

| Property        | Value                  |
| --------------- | ---------------------- |
| Confidentiality | Public                 |
| Authority       | Catalog                |
| Trust           | Database-Authoritative |
| Integrity       | Critical               |
| Logging         | Safe                   |

Fields:

```text
product_variant_id
sku
name
is_active
```

Reason:

```text
Inventory and Ordering depend on variant identity.
```

---

## Product Price

| Property         | Value                  |
| ---------------- | ---------------------- |
| Confidentiality  | Public                 |
| Authority        | Catalog                |
| Trust            | Database-Authoritative |
| Integrity        | Critical               |
| Financial Impact | Critical               |
| Logging          | Safe                   |

Fields:

```text
price_amount_minor
currency
```

Rules:

```text
Never trust from client.
Always loaded from Catalog authority.
```

---

# Cart Service Data Inventory

## Cart

| Property        | Value              |
| --------------- | ------------------ |
| Confidentiality | Internal           |
| Authority       | Cart               |
| Trust           | Service-Controlled |
| Integrity       | Medium             |
| Logging         | Safe               |

Fields:

```text
cartId
```

---

## Cart Item Quantity

| Property        | Value             |
| --------------- | ----------------- |
| Confidentiality | Internal          |
| Authority       | Cart              |
| Trust           | Client-Controlled |
| Integrity       | High              |
| Logging         | Safe              |

Fields:

```text
quantity
```

Rules:

```text
Validated by Cart.
Not trusted beyond checkout.
```

---

# Ordering Service Data Inventory

## Order Status

| Property         | Value              |
| ---------------- | ------------------ |
| Confidentiality  | Internal           |
| Authority        | Ordering           |
| Trust            | Service-Controlled |
| Integrity        | Critical           |
| Security Impact  | Critical           |
| Financial Impact | Critical           |
| Logging          | Safe               |

Fields:

```text
status
```

---

## Order Snapshot

| Property         | Value                  |
| ---------------- | ---------------------- |
| Confidentiality  | Internal               |
| Authority        | Ordering               |
| Trust            | Database-Authoritative |
| Integrity        | Critical               |
| Financial Impact | Critical               |
| Logging          | Restricted             |

Fields:

```text
product_name
variant_name
sku
unit_price_amount_minor
currency
```

Rules:

```text
Immutable after order creation.
```

---

## Order Total

| Property         | Value      |
| ---------------- | ---------- |
| Confidentiality  | Sensitive  |
| Authority        | Ordering   |
| Trust            | Derived    |
| Integrity        | Critical   |
| Financial Impact | Critical   |
| Logging          | Restricted |

Fields:

```text
total_amount_minor
```

Rules:

```text
Server-calculated only.
```

---

## Customer Information

| Property        | Value           |
| --------------- | --------------- |
| Confidentiality | Sensitive       |
| Authority       | Ordering        |
| Trust           | User-Controlled |
| Integrity       | Medium          |
| Logging         | Masked          |

Fields:

```text
customer_name
customer_email
```

---

# Inventory Service Data Inventory

## Stock Balance

| Property         | Value                  |
| ---------------- | ---------------------- |
| Confidentiality  | Internal               |
| Authority        | Inventory              |
| Trust            | Database-Authoritative |
| Integrity        | Critical               |
| Financial Impact | High                   |
| Logging          | Safe                   |

Fields:

```text
on_hand_quantity
reserved_quantity
```

---

## Available Quantity

| Property        | Value     |
| --------------- | --------- |
| Confidentiality | Internal  |
| Authority       | Inventory |
| Trust           | Derived   |
| Integrity       | Critical  |
| Logging         | Safe      |

Formula:

```text
available_quantity =
on_hand_quantity -
reserved_quantity
```

---

## Reservation

| Property         | Value              |
| ---------------- | ------------------ |
| Confidentiality  | Internal           |
| Authority        | Inventory          |
| Trust            | Service-Controlled |
| Integrity        | Critical           |
| Security Impact  | High               |
| Financial Impact | High               |
| Logging          | Safe               |

Fields:

```text
reservation_id
status
quantity
```

---

# Payment Service Data Inventory

## Payment

| Property         | Value              |
| ---------------- | ------------------ |
| Confidentiality  | Internal           |
| Authority        | Payment            |
| Trust            | Service-Controlled |
| Integrity        | Critical           |
| Financial Impact | Critical           |
| Logging          | Safe               |

Fields:

```text
payment_id
status
amount_minor
currency
```

---

## Provider Reference

| Property        | Value                      |
| --------------- | -------------------------- |
| Confidentiality | Internal                   |
| Authority       | Payment                    |
| Trust           | External-System-Controlled |
| Integrity       | Critical                   |
| Security Impact | Critical                   |
| Logging         | Restricted                 |

Fields:

```text
provider_reference
```

---

# Fulfillment Service Data Inventory

## Shipment

| Property        | Value              |
| --------------- | ------------------ |
| Confidentiality | Internal           |
| Authority       | Fulfillment        |
| Trust           | Service-Controlled |
| Integrity       | Critical           |
| Logging         | Safe               |

Fields:

```text
shipment_id
status
```

---

## Tracking Information

| Property        | Value           |
| --------------- | --------------- |
| Confidentiality | Sensitive       |
| Authority       | Fulfillment     |
| Trust           | User-Controlled |
| Integrity       | High            |
| Logging         | Masked          |

Fields:

```text
carrier
tracking_number
```

---

# Security-Critical Data Classes

The following data classes are automatically considered critical:

## Financial Data

```text
price_amount_minor
unit_price_amount_minor
line_total_amount_minor
total_amount_minor
amount_minor
```

---

## Authorization Data

```text
roles
permissions
ownership relations
```

---

## Lifecycle State Data

```text
product status
order status
payment status
shipment status
reservation status
```

---

## Inventory State Data

```text
stock balances
reservation quantities
reservation states
```

---

## Authentication Data

```text
JWT claims
API keys
service credentials
```

---

# Classification Decision Rules

## Rule 1

If data affects money movement:

```text
Integrity = Critical
```

---

## Rule 2

If data affects authorization:

```text
Integrity = Critical
```

---

## Rule 3

If data controls lifecycle transitions:

```text
Integrity = Critical
```

---

## Rule 4

Derived values are never accepted as input.

Examples:

```text
order total
line total
available quantity
```

---

## Rule 5

Identifiers are selectors, not proof.

Examples:

```text
cartId
orderId
paymentId
shipmentId
reservationId
```

---

## Rule 6

Cross-service references do not imply ownership.

Example:

```text
Inventory stores SKU.
Catalog owns SKU definition.
```

---

## Rule 7

Authority is singular.

Every significant business datum must have exactly one authoritative owner.
