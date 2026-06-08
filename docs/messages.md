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
- commit reservation

Payment -> Ordering
- mark order as Paid

Fulfillment -> Ordering
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

Ordering calls Inventory when creating, cancelling and marking orders as paid.

```text
POST /api/stock/reservations/allocate
POST /api/stock/reservations/{reservationId}/release
POST /api/stock/reservations/{reservationId}/commit
```

Purpose:

```text
Allocate stock during order creation.
Release stock reservation when an order is cancelled.
Commit stock reservation when an order is marked as Paid.
```

Current behavior:

```text
Ordering allocates Inventory stock reservation during order creation.
Inventory chooses warehouse and storage location during allocation.
Ordering stores inventory_reservation_id on order items.
Ordering releases Inventory reservations when PendingPayment orders are cancelled.
Ordering commits Inventory reservations when PendingPayment orders are marked as Paid.
```

Current MVP simplification:

```text
Inventory reservation commit currently happens during Ordering mark-paid.
In a fuller commerce flow, Inventory commit should move closer to fulfillment/shipment.
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
Ordering commits Inventory reservations during mark-paid.
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

Fulfillment calls Ordering when a shipment is shipped.

```text
POST /api/orders/{orderId}/mark-shipped
```

Purpose:

```text
Mark paid order as Shipped after shipment is shipped.
```

Current behavior:

```text
Fulfillment stores shipment records.
Fulfillment ships pending shipments.
Fulfillment calls Ordering to mark the linked order as Shipped.
Ordering accepts only Paid orders.
Ordering rejects PendingPayment, Cancelled and already Shipped orders.
Fulfillment keeps shipment Pending if Ordering rejects mark-shipped.
```

Current MVP simplification:

```text
Fulfillment currently does not commit Inventory reservations.
Inventory reservation commit currently happens during Ordering mark-paid.
In a fuller commerce flow, Inventory commit may move closer to this fulfillment/shipment step.
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
