# Architecture

## Services

```text
Catalog     - product catalog data, variants/SKUs and current prices
Inventory   - stock, reservations and stock allocation
Cart        - shopping carts and cart items
Ordering    - orders, order lifecycle and product snapshots inside orders
Payment     - payment records and payment simulation
Fulfillment - shipments and shipment lifecycle
Frontend    - Angular UI
```

---

## Service ownership

```text
Catalog owns products, brands, categories, variants/SKUs and current prices.
Inventory owns warehouses, locations, stock balances, movements, reservations and allocation.
Cart owns carts, cart items, product variant references and quantities.
Ordering owns orders, order items, order statuses, order totals and product snapshots.
Payment owns payments, payment statuses, provider references and failure reasons.
Fulfillment owns shipments, shipment statuses, carrier information, tracking numbers and shipment timestamps.
```

---

## Main backend flow

```text
Catalog defines product variants, SKUs and current prices.
Inventory stores stock by SKU.
Cart stores product variant IDs and quantities before checkout.
Ordering creates orders from product variant IDs and quantities.
Ordering loads trusted product snapshots from Catalog.
Ordering asks Inventory to allocate stock reservations by SKU.
Payment stores payment records for orders.
Payment calls Ordering when a pending payment succeeds.
Ordering marks the order as Paid.
Ordering keeps Inventory reservation allocated when the order is marked as Paid.
Fulfillment validates paid orders through Ordering.
Fulfillment creates shipment records for paid orders.
Fulfillment calls Ordering when a shipment is shipped.
Ordering accepts only Paid orders for the Shipped transition.
Ordering commits Inventory reservation when the paid order is marked as Shipped.
Ordering marks the order as Shipped.
```

---

## Cart flow

```text
Cart is created.
Product variant is added to Cart with quantity.
Cart item quantity can be updated.
Cart item can be removed.
Cart can be cleared.
Cart does not reserve stock.
Cart does not store trusted product prices, names, SKUs or currencies.
```

---

## Cancel flow

```text
Order is created.
Ordering loads product snapshot from Catalog.
Inventory stock reservation is allocated.
Order is cancelled.
Inventory reservation is released.
```

---

## Payment success flow

```text
Order is created.
Inventory stock reservation is allocated.
Payment is created.
Payment is marked as Succeeded.
Payment calls Ordering to mark the order as Paid.
Ordering marks the order as Paid.
Inventory reservation remains allocated until shipment.
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

## Fulfillment flow

```text
Order is paid.
Inventory reservation is still allocated.
Fulfillment validates the paid order through Ordering.
Shipment is created.
Shipment is shipped.
Fulfillment calls Ordering to mark the order as Shipped.
Ordering commits Inventory reservation during mark-shipped.
Ordering marks the order as Shipped.
```

---

## Current Inventory commit boundary

```text
Inventory reservation commit happens during Ordering mark-shipped.
Fulfillment triggers mark-shipped through Ordering.
Fulfillment does not call Inventory directly.
Ordering owns inventory_reservation_id references stored on order items.
```

---

## Boundary rules

```text
Services own their own databases.
Services must not read or write another service database directly.
Cross-service operations go through APIs.

Frontend does not send trusted product prices, product names, SKUs or currencies to Ordering.
Frontend does not choose warehouse or storage location.

Cart stores productVariantId and quantity only.
Cart does not store trusted product prices, product names, SKUs, currencies or stock data.
Cart does not reserve stock.
Cart does not create orders.
Cart does not process payments.

Catalog owns current product data and prices.
Ordering gets product snapshots from Catalog through Catalog API.
Ordering stores product snapshots so old orders do not change when Catalog data changes.

Inventory stores stock by SKU.
Inventory chooses warehouse and storage location during stock allocation.

Payment reference orders by order_id.
Payment does not write OrderingDb directly.
Payment does not write InventoryDb directly.

Fulfillment stores shipment records.
Fulfillment references orders by order_id.
Fulfillment calls Ordering to mark orders as Shipped.
Fulfillment does not write OrderingDb directly.
Fulfillment does not write InventoryDb directly.
Fulfillment does not call Inventory directly.
Fulfillment triggers Inventory commit indirectly through Ordering mark-shipped.
Ordering owns inventory_reservation_id references stored on order items.
```
