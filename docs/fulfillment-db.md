# FulfillmentDb v1

## Purpose

`FulfillmentDb` stores shipments and shipment lifecycle state for the e-commerce system.

Fulfillment Service owns:

* shipments;
* shipment statuses;
* carrier information;
* tracking numbers;
* shipment timestamps.

Fulfillment Service does **not** own:

* catalog product data;
* stock balances;
* inventory reservations;
* orders;
* payments;
* customers;
* invoices;
* refunds.

Catalog Service owns product data.

Inventory Service owns stock and reservations.

Ordering Service owns orders and order lifecycle.

Payment Service owns payments.

Fulfillment Service references orders by `order_id`. It must not read or write `OrderingDb` directly.

Other services may reference fulfillment data by `shipment_id`, but they must not read or write `FulfillmentDb` directly.

---

## Design principles

1. Fulfillment owns shipment lifecycle.
2. Ordering owns order lifecycle.
3. Fulfillment references orders by `order_id`.
4. Fulfillment calls Ordering API when a shipment is shipped.
5. Fulfillment does not write `OrderingDb` directly.
6. Fulfillment does not write `InventoryDb` directly.
7. Tables use UUID primary keys.
8. Shipments have `created_at` and `updated_at`.
9. Shipment terminal states are explicit.
10. A shipment can be cancelled only before it is shipped.

---

## Tables

## shipments

Represents a shipment for an order.

| Column          | Type        | Constraints |
| --------------- | ----------- | ----------- |
| id              | uuid        | PK          |
| order_id        | uuid        | NOT NULL    |
| status          | integer     | NOT NULL    |
| carrier         | text        | NULL        |
| tracking_number | text        | NULL        |
| created_at      | timestamptz | NOT NULL    |
| updated_at      | timestamptz | NOT NULL    |
| shipped_at      | timestamptz | NULL        |
| cancelled_at    | timestamptz | NULL        |

Shipment statuses:

| Value | Name      | Meaning                                  |
| ----- | --------- | ---------------------------------------- |
| 1     | Pending   | Shipment was created but not shipped yet |
| 2     | Shipped   | Shipment was shipped                     |
| 3     | Cancelled | Shipment was cancelled before shipping   |

Notes:

* `order_id` references an Ordering order identity.
* `carrier` is optional.
* `tracking_number` is optional.
* `shipped_at` is set when shipment becomes `Shipped`.
* `cancelled_at` is set when shipment becomes `Cancelled`.
* Fulfillment Service does not own the order itself.

Rules:

* `order_id` is required.
* New shipments are created as `Pending`.
* Only `Pending` shipments can be shipped.
* Only `Pending` shipments can be cancelled.
* Shipped shipments cannot be cancelled.
* Cancelled shipments cannot be shipped.
* Shipping a shipment calls Ordering API to mark the linked order as `Shipped`.
* If Ordering rejects `mark-shipped`, the shipment remains `Pending`.

---

## Shipment lifecycle

Current implemented lifecycle:

```text
Pending -> Shipped
Pending -> Cancelled
```

Current shipment flow:

```text
Create shipment
Shipment is Pending
Ship shipment
Fulfillment calls Ordering mark-shipped
Ordering marks paid order as Shipped
Shipment becomes Shipped
```

Cancel flow:

```text
Create shipment
Shipment is Pending
Cancel shipment
Shipment becomes Cancelled
Order is not changed
```

---

## Ordering integration

Fulfillment Service calls Ordering Service when a shipment is shipped:

```text
POST /api/orders/{orderId}/mark-shipped
```

Expected Ordering behavior:

```text
Only Paid orders can be marked as Shipped.
PendingPayment orders cannot be marked as Shipped.
Cancelled orders cannot be marked as Shipped.
Already Shipped orders cannot be marked as Shipped again.
```

Fulfillment behavior when Ordering rejects `mark-shipped`:

```text
Shipment remains Pending.
Fulfillment returns Ordering error code and message.
```

---

## Current MVP simplification

Current system behavior:

```text
Payment success calls Ordering mark-paid.
Ordering marks order as Paid.
Ordering commits Inventory reservation during mark-paid.
Fulfillment later marks shipment as Shipped.
Ordering marks order as Shipped.
Shipping does not currently commit Inventory reservation.
```

Target future behavior:

```text
Payment success marks order as Paid.
Inventory reservation remains allocated.
Fulfillment ships shipment.
Inventory reservation is committed around fulfillment/shipment.
Order is marked as Shipped.
```

The current implementation keeps Inventory commit in Ordering `mark-paid` to avoid breaking the existing payment flow while Fulfillment Service is introduced.

---

# Relationships summary

```text
shipments -> orders by order_id
```

This is a cross-service reference only.

Fulfillment Service must not read or write `OrderingDb` directly.

---

# Constraints and indexes

Recommended lookup indexes:

```text
shipments.order_id
shipments.status
shipments.created_at
```

Recommended checks:

```text
shipments.status in known shipment status values
```

Recommended future constraints:

```text
tracking_number length limit
carrier length limit
one active shipment per order, if the business model requires it
```

---

# Out of scope for FulfillmentDb v1

The following are intentionally excluded:

* direct reads from OrderingDb;
* direct reads from InventoryDb;
* direct reads from PaymentDb;
* direct reads from CatalogDb;
* warehouse picking;
* packing workflow;
* shipment labels;
* shipment rates;
* shipping address;
* carrier API integration;
* delivery status tracking;
* returns;
* refunds;
* partial shipments;
* split shipments;
* multi-package shipments;
* shipment event history;
* inventory commit ownership.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Fulfillment Service currently supports:

```text
GET  /health

GET  /api/shipments
GET  /api/shipments/{id}
POST /api/shipments
POST /api/shipments/{id}/ship
POST /api/shipments/{id}/cancel
```

Current behavior:

```text
Shipments are created as Pending.
Shipments require order_id.
Carrier is optional.
Tracking number is optional.
Only Pending shipments can be shipped.
Only Pending shipments can be cancelled.
Shipping a shipment calls Ordering to mark order as Shipped.
If Ordering rejects mark-shipped, shipment remains Pending.
Cancelling a shipment does not change order state.
Shipped shipments cannot be shipped again.
Shipped shipments cannot be cancelled.
Cancelled shipments cannot be shipped.
```

Current tests:

```text
Create shipment tests
Validation tests
List shipment tests
Get shipment details tests
Ship shipment tests
Ordering mark-shipped integration tests
Ordering rejection tests
Cancel shipment tests
Terminal status tests
Missing shipment tests
```

Future work:

```text
Validate order status before shipment creation.
Add shipment address.
Add shipment item lines.
Add shipment event history.
Add carrier integration.
Add partial shipment support.
Add delivery tracking.
Move Inventory commit from Ordering mark-paid to Fulfillment shipment step.
Add idempotency for ship and cancel operations.
Add optimistic concurrency for shipment updates.
```
