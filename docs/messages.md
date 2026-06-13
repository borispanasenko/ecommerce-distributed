# Messages

## Current state

The system currently uses synchronous HTTP calls for cross-service operations.

No message broker is implemented yet.

Messages are not part of the v1 implementation.

---

## Current synchronous calls

```text
Ordering -> Catalog
- get active product variant snapshot

Ordering -> Inventory
- allocate stock reservation
- release reservation
- commit reservation during mark-shipped

Payment -> Ordering
- mark order as Paid

Fulfillment -> Ordering
- get order details
- mark order as Shipped
```

---

## Ordering -> Catalog

Ordering calls Catalog when creating an order.

```text
GET /api/products/variants/{variantId}/snapshot
```

Purpose:

```text
Load trusted product snapshot data for order items.
```

Current behavior:

```text
Client sends productVariantId and quantity.
Ordering loads productId, productVariantId, SKU, product name, variant name, price and currency from Catalog.
Ordering stores the snapshot in order_items.
Ordering rejects order creation if Catalog snapshot lookup fails.
```

Future message candidates:

```text
ProductVariantChanged
ProductVariantArchived
ProductPriceChanged
```

---

## Ordering -> Inventory

Ordering calls Inventory when creating orders, cancelling pending orders and marking paid orders as shipped.

```text
POST /api/stock/reservations/allocate
POST /api/stock/reservations/{reservationId}/release
POST /api/stock/reservations/{reservationId}/commit
```

Purpose:

```text
Allocate stock during order creation.
Release stock reservation when an order is cancelled.
Commit stock reservation when a paid order is marked as Shipped.
```

Current behavior:

```text
Ordering allocates Inventory stock reservation during order creation.
Inventory chooses warehouse and storage location during allocation.
Ordering stores inventory_reservation_id on order items.
Ordering releases Inventory reservations when PendingPayment orders are cancelled.
Ordering keeps Inventory reservations allocated when orders are marked as Paid.
Ordering commits Inventory reservations when Paid orders are marked as Shipped.
```

Future message candidates:

```text
StockReservationAllocated
StockReservationAllocationFailed
StockReservationReleased
StockReservationCommitted
```

---

## Payment -> Ordering

Payment calls Ordering when a pending payment succeeds.

```text
POST /api/orders/{orderId}/mark-paid
```

Purpose:

```text
Mark order as Paid after successful payment.
```

Current behavior:

```text
Payment stores payment records.
Payment marks pending payment as Succeeded.
Payment calls Ordering to mark the linked order as Paid.
Ordering accepts only PendingPayment orders.
Ordering rejects Cancelled, Paid and Shipped orders.
Ordering marks the linked order as Paid and keeps Inventory reservations allocated until shipment.
Payment remains Pending if Ordering rejects mark-paid.
```

Future message candidates:

```text
PaymentSucceeded
PaymentFailed
OrderPaid
```

---

## Fulfillment -> Ordering

Fulfillment calls Ordering when creating and shipping shipments.

```text
GET  /api/orders/{orderId}
POST /api/orders/{orderId}/mark-shipped
```

Purpose:

```text
Validate that the linked order exists and is Paid before creating a shipment.
Mark paid order as Shipped after shipment is shipped.
```

Current behavior:

```text
Fulfillment stores shipment records.
Fulfillment checks the linked order through Ordering before creating a shipment.
Fulfillment creates shipments only for Paid orders.
Fulfillment rejects shipment creation if the order does not exist.
Fulfillment rejects shipment creation if the order is not Paid.
Fulfillment ships pending shipments.
Fulfillment calls Ordering to mark the linked order as Shipped.
Ordering accepts only Paid orders for mark-shipped.
Ordering commits Inventory reservations during mark-shipped.
Ordering rejects PendingPayment, Cancelled and already Shipped orders.
Fulfillment keeps shipment Pending if Ordering rejects mark-shipped.
```

Boundary note:

```text
Fulfillment does not call Inventory directly.
Inventory reservation commit is performed by Ordering during mark-shipped.
Ordering owns order items and inventory_reservation_id references.
```

Future message candidates:

```text
ShipmentCreated
ShipmentCancelled
ShipmentShipped
OrderShipped
```

---

## Future message candidates

```text
ProductVariantChanged
ProductVariantArchived
ProductPriceChanged
OrderCreated
StockReservationAllocated
StockReservationAllocationFailed
StockReservationReleased
StockReservationCommitted
PaymentSucceeded
PaymentFailed
OrderPaid
OrderCancelled
ShipmentCreated
ShipmentCancelled
ShipmentShipped
OrderShipped
```

---

## Notes

```text
Messages are not part of v1 implementation.
Current flows are implemented through HTTP APIs.
A message broker may be added later if async workflows are needed.
```
