# Messages

## Current state

The system currently uses synchronous HTTP calls for cross-service operations.

No message broker is implemented yet.

---

## Current synchronous calls

```text
Ordering -> Inventory
- reserve stock
- release reservation
- commit reservation

Payment -> Ordering
- mark order as Paid
```

---

## Future message candidates

```text
OrderCreated
StockReserved
StockReservationReleased
StockReservationCommitted
PaymentSucceeded
PaymentFailed
OrderPaid
OrderCancelled
```

---

## Notes

```text
Messages are not part of v1 implementation.
Current flows are implemented through HTTP APIs.
A message broker may be added later if async workflows are needed.
```
