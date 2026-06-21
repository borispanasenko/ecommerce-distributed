# Inventory Commit Boundary

## Current Boundary

```text
Inventory reservation commit happens during Ordering mark-shipped.
Fulfillment triggers mark-shipped through Ordering.
Fulfillment does not call Inventory directly.
Ordering owns inventory_reservation_id references stored on order items.
Rule

Inventory commit is not owned by Fulfillment.

Fulfillment owns shipment lifecycle.

Ordering owns the transition from Paid order to Shipped order and coordinates Inventory reservation commit.

Reason

This prevents Fulfillment from bypassing order lifecycle validation and committing stock for orders that are not eligible to be shipped.