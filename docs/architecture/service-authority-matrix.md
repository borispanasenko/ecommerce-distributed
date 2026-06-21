# Service Authority Matrix

## Purpose

This document defines which service is authoritative for each major category of data.

The goal is to prevent accidental cross-service ownership violations.

A service may reference data owned by another service, but reference does not imply ownership.

---

# Core Rule

Each piece of business data must have exactly one authoritative owner.

Other services may:

* reference it;
* cache it;
* snapshot it;
* derive from it;
* request changes through the owning service.

Other services must not directly mutate it.

---

# Authority Matrix

| Data                       | Authoritative Service | Consumers                              | Notes                                                          |
| -------------------------- | --------------------- | -------------------------------------- | -------------------------------------------------------------- |
| Product                    | Catalog               | Cart, Ordering, Inventory, Fulfillment | Catalog owns product identity and visibility.                  |
| Product Variant            | Catalog               | Cart, Ordering, Inventory              | Sellable unit.                                                 |
| SKU                        | Catalog               | Inventory, Ordering                    | Inventory tracks stock by SKU but does not define SKU meaning. |
| Current Price              | Catalog               | Ordering                               | Ordering loads price during order creation.                    |
| Product Snapshot           | Ordering              | Payment, Fulfillment                   | Snapshot becomes authoritative for the order.                  |
| Cart                       | Cart                  | Ordering                               | Cart is pre-checkout state only.                               |
| Cart Item Quantity         | Cart                  | Ordering                               | User-controlled but Cart-owned before checkout.                |
| Order                      | Ordering              | Payment, Fulfillment                   | Ordering owns order lifecycle.                                 |
| Order Status               | Ordering              | Payment, Fulfillment                   | Payment/Fulfillment may request transitions only.              |
| Order Total                | Ordering              | Payment                                | Calculated from order item snapshots.                          |
| Stock Balance              | Inventory             | Ordering                               | Inventory owns on-hand/reserved/available quantities.          |
| Reservation                | Inventory             | Ordering                               | Ordering stores reservation IDs only.                          |
| Payment                    | Payment               | Ordering                               | Payment owns payment attempts and payment status.              |
| Payment Provider Reference | Payment               | Ordering                               | External-system-controlled after verification.                 |
| Shipment                   | Fulfillment           | Ordering                               | Fulfillment owns shipment lifecycle.                           |
| Shipment Status            | Fulfillment           | Ordering                               | Shipment state is separate from order state.                   |

---

# Service Responsibilities

## Catalog Service

Authoritative for:

* brands;
* categories;
* products;
* product variants;
* SKUs;
* current prices;
* product visibility;
* product images.

May not own:

* stock;
* reservations;
* carts;
* orders;
* payments;
* shipments.

---

## Cart Service

Authoritative for:

* cart identity;
* cart items;
* cart item quantities before checkout.

May not own:

* product names;
* product prices;
* SKUs as trusted data;
* stock availability;
* reservations;
* orders;
* payments.

Cart data is temporary pre-checkout intent, not financial authority.

---

## Ordering Service

Authoritative for:

* orders;
* order lifecycle;
* order item snapshots;
* order totals;
* order status;
* inventory reservation references stored on order items.

May not own:

* live product catalog data;
* live stock balances;
* payment status;
* shipment status.

Ordering owns the boundary between:

* payment success and order paid;
* shipment success and inventory commit;
* order lifecycle and inventory reservation handling.

---

## Inventory Service

Authoritative for:

* warehouses;
* storage locations;
* stock balances;
* stock movements;
* stock reservations;
* reservation release;
* reservation commit.

May not own:

* product descriptions;
* product prices;
* carts;
* orders;
* payments;
* shipments.

Inventory may use SKU as a reference, but Catalog defines SKU identity.

---

## Payment Service

Authoritative for:

* payment records;
* payment attempts;
* payment status;
* provider;
* provider reference;
* failure reason.

May not own:

* order status;
* inventory reservations;
* shipment state.

Payment may request Ordering to mark an order as paid only after payment success is trusted.

---

## Fulfillment Service

Authoritative for:

* shipments;
* shipment lifecycle;
* carrier metadata;
* tracking numbers;
* shipped/cancelled timestamps.

May not own:

* order lifecycle;
* inventory commit;
* payment state.

Fulfillment may request Ordering to mark an order as shipped.

---

# Mutation Authority

| Data               | May Mutate       |
| ------------------ | ---------------- |
| Product            | Catalog only     |
| Product Price      | Catalog only     |
| Cart               | Cart only        |
| Order              | Ordering only    |
| Order Status       | Ordering only    |
| Stock Balance      | Inventory only   |
| Reservation Status | Inventory only   |
| Payment Status     | Payment only     |
| Shipment Status    | Fulfillment only |

---

# Cross-Service Rules

## Rule 1

Services must not read or write another service database directly.

## Rule 2

Cross-service references are not ownership.

## Rule 3

Snapshots must clearly identify their original authority.

## Rule 4

Derived values must be recomputed from authoritative data.

## Rule 5

State transitions must be requested through the owning service.

## Rule 6

A service may reject a transition even if another service requested it.

Example:

Fulfillment may request `mark-shipped`, but Ordering must reject it unless the order is `Paid`.

---

# Important Authority Boundaries

## Price Boundary

Catalog owns current price.

Ordering owns order snapshot price.

Payment must not trust client-provided amount.

---

## Inventory Commit Boundary

Inventory owns reservation commit.

Ordering owns the decision to commit reservation as part of order shipment.

Fulfillment must not call Inventory directly.

---

## Payment Boundary

Payment owns payment status.

Ordering owns order status.

Payment success does not directly mutate Inventory.

---

## Shipment Boundary

Fulfillment owns shipment status.

Ordering owns order shipped status.

A shipped shipment and a shipped order are related but not the same source of truth.

---

# Open Questions

The following authority decisions should be resolved before production hardening:

1. Who owns customer identity?
2. Who owns cart ownership?
3. Who owns order ownership?
4. Who owns payment initiation rules?
5. Who owns shipment visibility to customers?
6. Will audit events have a dedicated owner?
7. Will customer addresses belong to Ordering, Fulfillment, or a future Customer/Profile service?
