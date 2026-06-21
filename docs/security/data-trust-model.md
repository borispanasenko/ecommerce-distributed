# Data Trust Model

## Purpose

This document defines which parts of the system are authoritative for which data.

The goal is to prevent developers from accidentally trusting data from the wrong source.

This model is not primarily about confidentiality. A field may be public and still be integrity-critical.

Example:

`priceAmountMinor` may be visible to clients, but clients must never be trusted to provide it for checkout, payment, discount, or order total calculations.

## Core Principle

Every field must have an explicit source of truth.

A value received from a client, another service, webhook, request path, query string, or DTO is not automatically trusted.

Identifiers are selectors, not authorization proof.

Examples:

* `productVariantId` selects a Catalog variant, but does not prove that the variant is active or sellable.
* `orderId` selects an order, but does not prove that the caller owns it.
* `paymentId` selects a payment, but does not prove that the caller may complete it.
* `inventoryReservationId` selects a reservation, but must never be accepted from public clients as authority.

## Trust Levels

The project uses the following trust levels.

| Trust Level                | Meaning                                                                                               |
| -------------------------- | ----------------------------------------------------------------------------------------------------- |
| Client-Controlled          | Fully controlled by the caller. Must be validated and treated as untrusted.                           |
| User-Controlled            | User input that may be stored, but must not drive security or financial decisions without validation. |
| External-System-Controlled | Comes from a provider or external integration. Trusted only after verification.                       |
| Service-Controlled         | Produced by a trusted internal service.                                                               |
| Server-Controlled          | Assigned or changed by server-side application logic.                                                 |
| Database-Authoritative     | Current durable source of truth.                                                                      |
| Derived/Computed           | Recomputed from authoritative data. Must not be accepted from clients.                                |

## Service Authority Map

| Service     | Authoritative For                                                                        | Not Authoritative For                                                    |
| ----------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Catalog     | Products, variants, SKUs, categories, brands, current product prices, product visibility | Stock, carts, orders, payments, shipments                                |
| Cart        | Cart identity, cart items, requested quantities before checkout                          | Prices, SKUs as trusted data, product names, stock, reservations, orders |
| Ordering    | Orders, order status, order item snapshots, order totals, order lifecycle                | Live catalog data, stock state, payment state, shipment state            |
| Inventory   | Warehouses, locations, stock balances, reservations, stock movements                     | Product descriptions, prices, orders, payments, shipments                |
| Payment     | Payment attempts, payment status, provider references, failure reasons                   | Order lifecycle, inventory commit, shipments                             |
| Fulfillment | Shipments, shipment status, carrier/tracking metadata                                    | Orders, payments, stock reservations, inventory commit                   |

## Cross-Service Trust Boundaries

### Client → API

Client input is untrusted by default.

Clients may provide selectors and user input, such as:

* `productVariantId`
* `cartId`
* `orderId`
* `quantity`
* `customerName`
* `customerEmail`

Clients must not provide authoritative values for:

* prices
* totals
* order status
* payment status
* shipment status
* stock quantities
* reservations
* ownership
* roles or permissions

### API → Service Logic

API DTOs are transport objects, not domain authority.

Service logic must decide:

* which fields to accept;
* which fields to validate;
* which fields to ignore;
* which fields to recompute;
* which fields to load from another authoritative service.

### Service → Database

Each service database is authoritative only for its own bounded context.

Services must not read or write another service database directly.

Cross-service data must be obtained through service APIs or trusted messaging.

### Ordering → Catalog

Ordering may request product variant snapshots from Catalog.

Catalog is authoritative for:

* `productId`
* `productVariantId`
* `sku`
* `productName`
* `variantName`
* `priceAmountMinor`
* `currency`

Ordering stores these values as order item snapshots.

After the order is created, the order snapshot becomes authoritative for that order, even if Catalog later changes.

### Ordering → Inventory

Ordering may request stock allocation from Inventory.

Inventory is authoritative for:

* warehouse selection
* storage location selection
* stock reservation creation
* stock reservation status
* stock release
* stock commit

Ordering may store `inventoryReservationId`, but does not own stock state.

### Payment → Ordering

Payment is authoritative for payment attempts.

Ordering is authoritative for order status.

Payment may request `mark-paid`, but only after the payment result is trusted.

A payment result must not be trusted only because a client called an endpoint.

### Fulfillment → Ordering

Fulfillment is authoritative for shipment lifecycle.

Ordering is authoritative for order lifecycle and inventory commit boundary.

Fulfillment may request `mark-shipped`, but Ordering must validate that the order is in a valid state.

Fulfillment must not commit Inventory reservations directly.

## Money Trust Rules

Money fields are integrity-critical.

The following fields must never be accepted from public clients as authority:

* `priceAmountMinor`
* `unitPriceAmountMinor`
* `lineTotalAmountMinor`
* `totalAmountMinor`
* `amountMinor`
* `currency` when used for payment or order calculation

Order totals must be calculated server-side from authoritative Catalog snapshots and validated quantities.

Payment amounts must be derived from the trusted order total, not from client input.

## Status Trust Rules

Lifecycle status fields are server-controlled.

Clients must not directly control:

* product status
* order status
* payment status
* shipment status
* reservation status

Status transitions must be performed only by the owning service or explicitly trusted caller.

## Inventory Trust Rules

Inventory values are integrity-critical.

Clients must not control:

* `onHandQuantity`
* `reservedQuantity`
* `availableQuantity`
* `warehouseId` for allocation decisions
* `locationId` for allocation decisions
* reservation status
* reservation commit/release

`availableQuantity` is derived:

```text
availableQuantity = onHandQuantity - reservedQuantity
```

It must be computed by Inventory, not stored or accepted from clients.

## Snapshot Trust Rules

Order item snapshots are created by Ordering from Catalog data.

Clients may provide:

* `productVariantId`
* `quantity`

Clients must not provide:

* `sku`
* `productName`
* `variantName`
* `unitPriceAmountMinor`
* `currency`
* `lineTotalAmountMinor`

## Trust Model Gaps To Resolve

The following areas must be explicitly defined before the system can be considered production-grade:

1. Authentication model.
2. Service-to-service authentication.
3. Authorization model.
4. Ownership checks for carts, orders, payments, and shipments.
5. Endpoint exposure classification.
6. Provider webhook verification.
7. Idempotency for payment, shipment, order, and inventory transitions.
8. Logging redaction rules.
9. Audit/event history for critical state changes.
