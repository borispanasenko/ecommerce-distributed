# Architecture

## Services

```text
Catalog   - product catalog data
Inventory - stock and reservations
Ordering  - orders and order lifecycle
Payment   - payment records and payment simulation
Frontend  - Angular UI
```

---

## Service ownership

```text
Catalog owns products, brands, categories, variants/SKUs and prices.
Inventory owns warehouses, locations, stock balances, movements and reservations.
Ordering owns orders, order items, order statuses and product snapshots.
Payment owns payments, payment statuses, provider references and failure reasons.
```

---

## Main backend flow

```text
Catalog defines SKUs.
Inventory stores stock by SKU.
Ordering creates orders from product snapshot data.
Ordering reserves Inventory stock when an order is created.
Payment stores payment records for orders.
Payment calls Ordering when a pending payment succeeds.
Ordering marks the order as Paid.
Ordering commits Inventory reservation when order is marked as Paid.
```

---

## Cancel flow

```text
Order is created.
Inventory stock is reserved.
Order is cancelled.
Inventory reservation is released.
```

---

## Payment failure flow

```text
Payment is created.
Payment is marked as Failed.
Payment failure does not call Ordering.
Inventory reservation is not committed by Payment failure.
```

---

## Boundary rules

```text
Services own their own databases.
Services must not read or write another service database directly.
Cross-service operations go through APIs.
Orders store product snapshots.
Inventory stores stock by SKU.
Payments reference orders by order_id.
Payment does not write OrderingDb directly.
Payment does not write InventoryDb directly.
```
