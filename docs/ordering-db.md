# OrderingDb v1

## Purpose

`OrderingDb` stores customer orders and order item snapshots for the e-commerce system.

Ordering Service owns:

* orders;
* order items;
* order statuses;
* order totals;
* product snapshots inside orders.

Ordering Service does **not** own:

* catalog product data;
* product prices as live catalog data;
* stock balances;
* inventory reservations as stock state;
* payments;
* refunds;
* shipments;
* customer accounts.

Catalog Service owns product data and current product prices.

Inventory Service owns stock and reservations.

Payment Service owns payments.

Fulfillment Service owns shipments.

Ordering Service stores product snapshots so existing orders do not change when Catalog data changes.

Other services may reference ordering data by `order_id`, but they must not read or write `OrderingDb` directly.

---

## Design principles

1. Ordering owns the order lifecycle.
2. Catalog owns product descriptions, variants and current prices.
3. Inventory owns stock and reservations.
4. Payment owns payment state.
5. Fulfillment owns shipment lifecycle.
6. Order items store product snapshots.
7. Orders store totals in minor units to avoid decimal precision issues.
8. One order can contain multiple order items.
9. All order items in one order use the same currency.
10. Tables use UUID primary keys.
11. Orders have `created_at` and `updated_at`.
12. Ordering gets trusted product snapshots from Catalog through Catalog API.
13. Ordering asks Inventory to allocate stock reservations by SKU.
14. Clients do not provide trusted product names, prices, SKUs or warehouse locations during checkout.
15. Fulfillment calls Ordering to mark paid orders as shipped.

---

## Tables

## orders

Represents a customer order.

| Column             | Type        | Constraints |
| ------------------ | ----------- | ----------- |
| id                 | uuid        | PK          |
| customer_name      | text        | NOT NULL    |
| customer_email     | text        | NOT NULL    |
| status             | integer     | NOT NULL    |
| total_amount_minor | bigint      | NOT NULL    |
| currency           | char(3)     | NOT NULL    |
| created_at         | timestamptz | NOT NULL    |
| updated_at         | timestamptz | NOT NULL    |

Order statuses:

| Value | Name           | Meaning                              |
| ----- | -------------- | ------------------------------------ |
| 1     | Draft          | Order is being prepared              |
| 2     | PendingPayment | Order was created and awaits payment |
| 3     | Paid           | Order was paid                       |
| 4     | Cancelled      | Order was cancelled                  |
| 5     | Shipped        | Order was shipped                    |

Current implemented statuses:

```text
PendingPayment
Paid
Cancelled
Shipped
```

Notes:

* Orders are currently created as `PendingPayment`.
* Orders can be cancelled while they are in `PendingPayment`.
* Orders can be marked as `Paid` while they are in `PendingPayment`.
* Orders can be marked as `Shipped` while they are in `Paid`.
* `total_amount_minor` stores money in minor units.
* Example: `13800` means `138.00`.

Rules:

* Order must contain at least one item.
* Only `PendingPayment` orders can be cancelled.
* Only `PendingPayment` orders can be marked as `Paid`.
* Only `Paid` orders can be marked as `Shipped`.
* `total_amount_minor` must be greater than or equal to `0`.
* `currency` must have `3` characters.
* All order items must use the same currency.

---

## order_items

Represents one sellable item inside an order.

| Column                   | Type    | Constraints    |
| ------------------------ | ------- | -------------- |
| id                       | uuid    | PK             |
| order_id                 | uuid    | FK → orders.id |
| product_id               | uuid    | NOT NULL       |
| product_variant_id       | uuid    | NOT NULL       |
| sku                      | text    | NOT NULL       |
| product_name             | text    | NOT NULL       |
| variant_name             | text    | NOT NULL       |
| unit_price_amount_minor  | bigint  | NOT NULL       |
| currency                 | char(3) | NOT NULL       |
| quantity                 | integer | NOT NULL       |
| line_total_amount_minor  | bigint  | NOT NULL       |
| inventory_reservation_id | uuid    | NULL           |

Notes:

* `product_id` and `product_variant_id` reference Catalog identities.
* `sku` is copied from Catalog product variant snapshot.
* `product_name`, `variant_name`, `unit_price_amount_minor` and `currency` are snapshots loaded from Catalog.
* `inventory_reservation_id` can reference an Inventory reservation.
* Ordering stores only the Inventory reservation id, not Inventory stock state.

Rules:

* `quantity` must be greater than `0`.
* `unit_price_amount_minor` must be greater than or equal to `0`.
* `line_total_amount_minor` must be greater than or equal to `0`.
* `currency` must have `3` characters.
* `line_total_amount_minor = unit_price_amount_minor * quantity`.

---

## Product snapshot

Order items store a copy of product data:

```text
product_id
product_variant_id
sku
product_name
variant_name
unit_price_amount_minor
currency
```

Reason:

```text
Catalog data can change later.
Existing orders should keep the original product name, variant name, SKU and price.
```

Example:

```text
Catalog product variant snapshot:
Monitor Arm / Black / ARM-BLK / 6900 USD

Order item snapshot:
Monitor Arm / Black / ARM-BLK / 6900 USD
```

Ordering gets this snapshot from Catalog Service when an order is created.

Clients only send product variant ids and quantities during order creation.

---

# Relationships summary

```text
orders 1 ─── * order_items
```

External references:

```text
order_items.product_id                 -> Catalog product identity
order_items.product_variant_id         -> Catalog product variant identity
order_items.sku                        -> Catalog SKU copied into order snapshot
order_items.inventory_reservation_id   -> Inventory reservation identity
orders.id                              -> referenced by Payment as order_id
orders.id                              -> referenced by Fulfillment as order_id
```

These are cross-service references only.

Ordering Service does not read or write `CatalogDb`, `InventoryDb`, `PaymentDb` or `FulfillmentDb` directly.

---

# Constraints and indexes

Recommended lookup indexes:

```text
orders.status
orders.customer_email
orders.created_at

order_items.order_id
order_items.sku
order_items.product_id
order_items.product_variant_id
order_items.inventory_reservation_id
```

Recommended checks:

```text
orders.total_amount_minor >= 0
length(orders.currency) = 3

order_items.unit_price_amount_minor >= 0
order_items.line_total_amount_minor >= 0
order_items.quantity > 0
length(order_items.currency) = 3
```

---

# Out of scope for OrderingDb v1

The following are intentionally excluded:

* payment state storage;
* refunds;
* invoices;
* shipment tracking;
* customer accounts;
* discounts;
* promotions;
* tax calculation;
* order returns;
* order event log;
* direct reads from CatalogDb;
* direct reads from InventoryDb;
* direct reads from PaymentDb;
* direct reads from FulfillmentDb.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Ordering Service currently supports:

```text
GET  /api/orders
GET  /api/orders/{id}
POST /api/orders
POST /api/orders/{id}/cancel
POST /api/orders/{id}/mark-paid
POST /api/orders/{id}/mark-shipped
```

Current order creation request uses product variant ids and quantities:

```text
customer_name
customer_email
items:
  product_variant_id
  quantity
```

Current ordering flow:

```text
Create order from product variant ids and quantities
Load product snapshots from Catalog Service
Allocate Inventory stock reservations by SKU
Store inventory_reservation_id on order item
Calculate line totals
Calculate order total
List orders
Get order details
Cancel order
Release Inventory reservation
Mark order as Paid
Commit Inventory reservation
Mark paid order as Shipped
```

Current behavior:

```text
Orders are created as PendingPayment.
Create order requests contain product_variant_id and quantity.
Ordering loads trusted product snapshot data from Catalog Service.
Ordering does not trust client-provided product names, prices, SKUs or currencies.
Ordering asks Inventory Service to allocate stock reservations by SKU.
Inventory chooses the warehouse and storage location for allocated reservations.
Order items store product snapshot data.
Order items store inventory_reservation_id returned by Inventory Service.
Line totals are calculated from Catalog snapshot unit price and requested quantity.
Order total is calculated as the sum of line totals.
Orders with empty item lists are rejected.
Orders with missing product_variant_id are rejected.
Orders with invalid quantity are rejected.
Orders with mixed Catalog snapshot currencies are rejected.
If Catalog snapshot lookup fails, order creation is rejected.
If Inventory allocation fails, order creation is rejected.
If order persistence fails after reservation, Ordering tries to release created reservations.
Only PendingPayment orders can be cancelled.
Cancelling an order releases Inventory reservations.
Cancelled orders cannot be cancelled again.
Only PendingPayment orders can be marked as Paid.
Marking an order as Paid commits Inventory reservations.
Paid orders cannot be cancelled.
Paid orders cannot be marked as Paid again.
Only Paid orders can be marked as Shipped.
Marking an order as Shipped changes order status only in the current MVP.
Shipped orders cannot be cancelled.
Shipped orders cannot be marked as Paid again.
Shipped orders cannot be marked as Shipped again.
Payment Service calls Ordering to mark orders as Paid when a pending payment succeeds.
Fulfillment Service calls Ordering to mark orders as Shipped when shipments are shipped.
GET /api/orders returns order summaries.
GET /api/orders/{id} returns order details.
```

Current MVP simplification:

```text
Ordering currently commits Inventory reservations when orders are marked as Paid.
Fulfillment currently marks paid orders as Shipped through Ordering.
In a fuller commerce flow, Inventory commit should move closer to fulfillment/shipment.
```

Current tests:

```text
Create order tests
Catalog snapshot integration tests
Inventory allocation integration tests
Order total calculation tests
Validation tests
Order list tests
Order details tests
Cancel order tests
Mark paid tests
Mark shipped tests
Inventory reservation commit tests
Missing order tests
```

Future work:

```text
Move Inventory commit from payment success to fulfillment/shipment flow.
Handle payment failure effects on order lifecycle.
Add order event history.
Add richer shipment integration.
Add refund flow.
Add customer identity and customer-specific order history.
Add idempotency for order creation and cross-service operations.
```
