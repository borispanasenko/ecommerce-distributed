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

Catalog Service owns product data.

Inventory Service owns stock and reservations.

Ordering Service stores product snapshots so existing orders do not change when Catalog data changes.

Other services may reference ordering data by `order_id`, but they must not read or write `OrderingDb` directly.

---

## Design principles

1. Ordering owns the order lifecycle.
2. Catalog owns product descriptions, variants and current prices.
3. Inventory owns stock and reservations.
4. Order items store product snapshots.
5. Orders store totals in minor units to avoid decimal precision issues.
6. One order can contain multiple order items.
7. All order items in one order use the same currency.
8. Tables use UUID primary keys.
9. Orders have `created_at` and `updated_at`.

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
Cancelled
```

Notes:

* Orders are currently created as `PendingPayment`.
* Orders can be cancelled while they are in `PendingPayment`.
* `total_amount_minor` stores money in minor units.
* Example: `13800` means `138.00`.

Rules:

* Order must contain at least one item.
* Only `PendingPayment` orders can be cancelled.
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
* `sku` is copied from Catalog.
* `product_name`, `variant_name`, `unit_price_amount_minor` and `currency` are snapshots.
* `inventory_reservation_id` can reference an Inventory reservation.

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
sku
product_name
variant_name
unit_price_amount_minor
currency
```

Reason:

```text
Catalog data can change later.
Existing orders should keep the original product name and price.
```

Example:

```text
Catalog product:
Monitor Arm / Black / ARM-BLK / 6900 USD

Order item snapshot:
Monitor Arm / Black / ARM-BLK / 6900 USD
```

---

# Relationships summary

```text
orders 1 ─── * order_items
```

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

* payment processing;
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
* direct reads from InventoryDb.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Ordering Service currently supports:

```text
GET  /api/orders
GET  /api/orders/{id}
POST /api/orders
POST /api/orders/{id}/cancel
```

Current ordering flow:

```text
Create order
Reserve Inventory stock
Store inventory_reservation_id on order item
List orders
Get order details
Cancel order
Release Inventory reservation
```

Current behavior:

```text
Orders are created as PendingPayment.
Creating an order reserves stock through Inventory Service.
Order items store product snapshot data.
Order items store inventory_reservation_id returned by Inventory Service.
Line totals are calculated from unit price and quantity.
Order total is calculated as the sum of line totals.
Orders with empty item lists are rejected.
Orders with mixed item currencies are rejected.
Orders without warehouse_id or location_id are rejected.
If Inventory reservation fails, order creation is rejected.
If order persistence fails after reservation, Ordering tries to release created reservations.
Only PendingPayment orders can be cancelled.
Cancelling an order releases Inventory reservations.
Cancelled orders cannot be cancelled again.
GET /api/orders returns order summaries.
GET /api/orders/{id} returns order details.
```

Current tests:

```text
Create order tests
Inventory reservation integration tests
Order total calculation tests
Validation tests
Order list tests
Order details tests
Cancel order tests
Missing order tests
```

Future work:

```text
Connect Payment service.
Mark order as Paid after successful payment.
Commit Inventory reservation after payment or shipment.
Handle payment failure.
Add order event history.
```
