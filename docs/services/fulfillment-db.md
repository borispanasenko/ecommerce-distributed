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
4. Fulfillment calls Ordering API to validate linked orders before shipment creation and to mark orders as Shipped when shipments are shipped.
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
* Creating a shipment validates the linked order through Ordering.
* A shipment can be created only for a Paid order.
* If the linked order does not exist, shipment creation is rejected.
* If the linked order is not Paid, shipment creation is rejected.

---

## Shipment lifecycle

Current implemented lifecycle:

```text
Pending -> Shipped
Pending -> Cancelled
```

Current shipment flow:

```text
Create shipment request is received.
Fulfillment checks linked order through Ordering.
Ordering returns order details.
Fulfillment verifies that order status is Paid.
Shipment is created as Pending.
Ship shipment.
Fulfillment calls Ordering mark-shipped.
Ordering commits Inventory reservation during mark-shipped.
Ordering marks paid order as Shipped.
Shipment becomes Shipped.
```

Cancel flow:

```text
Create shipment
Shipment is Pending
Cancel shipment
Shipment becomes Cancelled
Order is not changed
```

Inventory reservation remains allocated until the order is shipped or a future order cancellation/refund flow handles it.

---

## Ordering integration

Fulfillment Service calls Ordering Service when creating and shipping shipments.

```text
GET /api/orders/{orderId}
POST /api/orders/{orderId}/mark-shipped
```

Expected Ordering behavior:

```text
Only Paid orders can be marked as Shipped.
PendingPayment orders cannot be marked as Shipped.
Cancelled orders cannot be marked as Shipped.
Already Shipped orders cannot be marked as Shipped again.
Order details can be loaded by order id.
Only Paid orders can be used for shipment creation.
Mark-shipped commits Inventory reservation before marking the order as Shipped.
```

Fulfillment behavior when Ordering rejects `mark-shipped`:

```text
Shipment remains Pending.
Fulfillment returns Ordering error code and message.
```

Fulfillment behavior when Ordering rejects order validation:

```text
Shipment is not created.
Fulfillment returns Ordering error code and message.
```

---

## Current Inventory commit boundary

```text
Payment success calls Ordering mark-paid.
Ordering marks order as Paid.
Inventory reservation remains allocated while order is Paid.
Fulfillment validates Paid orders through Ordering before creating shipments.
Fulfillment ships shipment.
Fulfillment calls Ordering mark-shipped.
Ordering commits Inventory reservation during mark-shipped.
Ordering marks order as Shipped.
Fulfillment does not call Inventory directly.
```

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
Shipment is not created if linked order validation fails.
Cancelling a shipment does not change order state.
Shipped shipments cannot be shipped again.
Shipped shipments cannot be cancelled.
Cancelled shipments cannot be shipped.
Creating a shipment checks linked order through Ordering.
Shipments can be created only for Paid orders.
Shipment creation is rejected if the linked order does not exist.
Shipment creation is rejected if the linked order is not Paid.
Ordering commits Inventory reservation during mark-shipped.
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
Add shipment address.
Add shipment item lines.
Add shipment event history.
Add carrier integration.
Add partial shipment support.
Add delivery tracking.
Add idempotency for ship and cancel operations.
Add optimistic concurrency for shipment updates.
```
