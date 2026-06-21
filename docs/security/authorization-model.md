# Authorization Model

## Purpose

This document defines:

* actors;
* responsibilities;
* ownership rules;
* authorization boundaries;
* state transition authority.

Authentication answers:

```text
Who are you?
```

Authorization answers:

```text
What are you allowed to do?
```

This document focuses on authorization.

---

# Actor Types

The system recognizes the following actor categories.

## Anonymous User

Unauthenticated external user.

Capabilities:

```text
Browse public catalog.
Create guest cart.
View public product information.
```

Cannot:

```text
Manage inventory.
Manage products.
Manage payments.
Manage shipments.
Invoke internal endpoints.
```

---

## Customer

Authenticated customer.

Capabilities:

```text
Manage own cart.
Create own orders.
View own orders.
Initiate payment.
```

Cannot:

```text
Modify inventory.
Change order status.
Change payment status.
Change shipment status.
Invoke internal endpoints.
```

---

## Catalog Administrator

Responsible for catalog maintenance.

Capabilities:

```text
Create products.
Update products.
Publish products.
Archive products.
Manage categories.
Manage brands.
Manage product variants.
```

Cannot:

```text
Manipulate inventory balances.
Mark payments succeeded.
Ship orders.
```

---

## Warehouse Operator

Responsible for stock operations.

Capabilities:

```text
Receive stock.
Manage warehouse inventory.
Create stock adjustments.
Review reservations.
```

Cannot:

```text
Mark orders paid.
Ship orders.
Publish products.
```

---

## Fulfillment Operator

Responsible for shipment processing.

Capabilities:

```text
Create shipments.
Cancel shipments.
Ship shipments.
Manage carrier information.
Manage tracking numbers.
```

Cannot:

```text
Modify inventory directly.
Mark orders paid.
Change payment status.
```

---

## System Administrator

Operational authority.

Capabilities:

```text
Manage platform configuration.
Manage users and permissions.
Access administrative functions.
```

Does not automatically become authority for:

```text
payment status
inventory balances
order lifecycle
```

Administrative privilege is not business authority.

---

# Internal Service Actors

Internal services are first-class actors.

---

## Catalog Service

Authority:

```text
Products
Variants
SKUs
Prices
```

---

## Ordering Service

Authority:

```text
Orders
Order lifecycle
Order totals
Order snapshots
```

---

## Inventory Service

Authority:

```text
Stock
Reservations
Stock movements
```

---

## Payment Service

Authority:

```text
Payment attempts
Payment status
Provider references
```

---

## Fulfillment Service

Authority:

```text
Shipments
Shipment lifecycle
```

---

# Ownership Rules

Ownership must always be verified.

Identifiers do not prove ownership.

---

## Cart Ownership

Required checks:

```text
Caller owns cart.
```

The following is NOT sufficient:

```text
cartId
```

---

## Order Ownership

Required checks:

```text
Caller owns order.
```

The following is NOT sufficient:

```text
orderId
```

---

## Payment Ownership

Required checks:

```text
Caller owns order associated with payment.
```

The following is NOT sufficient:

```text
paymentId
```

---

## Shipment Visibility

Customers may only view shipments belonging to their own orders.

The following is NOT sufficient:

```text
shipmentId
```

---

# State Transition Authority

The most important authorization rule:

State transitions may only be executed by their owning authority.

---

## Product Lifecycle

Authority:

```text
Catalog Administrator
Catalog Service
```

Allowed transitions:

```text
Draft -> Active
Active -> Archived
```

---

## Order Lifecycle

Authority:

```text
Ordering Service
```

Allowed transitions:

```text
PendingPayment -> Paid
PendingPayment -> Cancelled
PendingPayment -> Expired
Paid -> Shipped
```

No external actor directly modifies order status.

---

## Payment Lifecycle

Authority:

```text
Payment Service
```

Allowed transitions:

```text
Pending -> Succeeded
Pending -> Failed
```

Customers never control payment status.

---

## Shipment Lifecycle

Authority:

```text
Fulfillment Service
```

Allowed transitions:

```text
Pending -> Shipped
Pending -> Cancelled
```

Customers never control shipment status.

---

## Reservation Lifecycle

Authority:

```text
Inventory Service
```

Allowed transitions:

```text
Active -> Released
Active -> Committed
```

Customers never control reservation status.

---

# Internal Endpoint Authorization

Some endpoints are not business endpoints.

They are workflow endpoints.

---

## Payment -> Ordering

Allowed:

```text
POST /api/orders/{id}/mark-paid
```

Caller:

```text
PaymentService
```

No other caller is authorized.

---

## Fulfillment -> Ordering

Allowed:

```text
POST /api/orders/{id}/mark-shipped
```

Caller:

```text
FulfillmentService
```

No other caller is authorized.

---

## Ordering -> Inventory

Allowed:

```text
POST /api/stock/reservations/allocate
POST /api/stock/reservations/{id}/release
POST /api/stock/reservations/{id}/commit
```

Caller:

```text
OrderingService
```

No customer-facing access.

---

# Future Authentication Mapping

The authorization model is independent from the authentication mechanism.

Future implementations may use:

```text
JWT
OAuth2
OIDC
API Keys
mTLS
Service Tokens
```

Regardless of mechanism, the actor identity must map to one of the actor types defined in this document.

---

# Non-Negotiable Rules

## Rule 1

Identifiers are selectors, not authorization proof.

---

## Rule 2

Business authority is more important than technical access.

---

## Rule 3

Only the owning authority may perform lifecycle transitions.

---

## Rule 4

Internal service endpoints are not customer endpoints.

---

## Rule 5

Ownership must be verified independently of identifiers.

---

## Rule 6

Authentication does not automatically imply authorization.
