# System Overview

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

## Service Ownership Summary

```text
Catalog owns products, brands, categories, variants/SKUs and current prices.

Inventory owns warehouses, locations, stock balances, movements, reservations and allocation.

Cart owns carts, cart items, product variant references and quantities.

Ordering owns orders, order items, order statuses, order totals and product snapshots.

Payment owns payments, payment statuses, provider references and failure reasons.

Fulfillment owns shipments, shipment statuses, carrier information, tracking numbers and shipment timestamps.
```

## Boundary Rules

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

Payment references orders by order_id.

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
