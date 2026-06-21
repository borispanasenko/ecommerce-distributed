# Main Flows

## Main Backend Flow

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

## Cart Flow

```text
Cart is created.
Product variant is added to Cart with quantity.
Cart item quantity can be updated.
Cart item can be removed.
Cart can be cleared.
Cart does not reserve stock.
Cart does not store trusted product prices, names, SKUs or currencies.
```

## Cancel Flow

```text
Order is created.
Ordering loads product snapshot from Catalog.
Inventory stock reservation is allocated.
Order is cancelled.
Inventory reservation is released.
```

## Payment Success Flow

```text
Order is created.
Inventory stock reservation is allocated.
Payment is created.
Payment is marked as Succeeded.
Payment calls Ordering to mark the order as Paid.
Ordering marks the order as Paid.
Inventory reservation remains allocated until shipment.
```

## Payment Failure Flow

```text
Payment is created.
Payment is marked as Failed.
Payment failure does not call Ordering.
Inventory reservation is not committed by Payment failure.
```

## Fulfillment Flow

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
