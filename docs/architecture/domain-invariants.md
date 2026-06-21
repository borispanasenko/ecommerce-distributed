# Domain Invariants

## Purpose

This document defines business invariants that must remain true at all times.

An invariant is a rule that must never be violated.

These rules are independent of:

- implementation details;
- database technology;
- messaging technology;
- deployment model;
- programming language.

Any system change that can violate an invariant must be considered a defect.

---

# Invariant Categories

The system defines invariants for:

- Catalog
- Cart
- Ordering
- Inventory
- Payment
- Fulfillment
- Cross-Service Workflows

---

# Catalog Invariants

## CATALOG-001

A product must have at least one active variant before becoming Active.

Reason:

A product without a sellable variant cannot be purchased.

---

## CATALOG-002

A product slug must uniquely identify a product.

Reason:

Public URLs depend on slug uniqueness.

---

## CATALOG-003

A SKU uniquely identifies a sellable product variant.

Reason:

Inventory tracking depends on SKU uniqueness.

---

## CATALOG-004

Product price must be non-negative.

Reason:

Negative pricing is invalid.

---

## CATALOG-005

Catalog is authoritative for current product prices.

Reason:

All financial calculations originate from Catalog prices.

---

# Cart Invariants

## CART-001

Cart item quantity must be greater than zero.

Reason:

Zero-quantity cart items have no business meaning.

---

## CART-002

A cart must not contain duplicate product_variant_id entries.

Reason:

Quantity aggregation must remain deterministic.

---

## CART-003

Cart does not own product price.

Reason:

Catalog prices may change before checkout.

---

## CART-004

Cart does not reserve inventory.

Reason:

Inventory reservation occurs only during order creation.

---

# Ordering Invariants

## ORDER-001

An order must contain at least one order item.

Reason:

Empty orders have no business meaning.

---

## ORDER-002

Order total equals the sum of all order line totals.

Formula:

total_amount_minor
=
Σ(line_total_amount_minor)

---

## ORDER-003

Order line total equals unit price multiplied by quantity.

Formula:

line_total_amount_minor
=
unit_price_amount_minor × quantity

---

## ORDER-004

Order item quantity must be greater than zero.

---

## ORDER-005

All order items within an order must use the same currency.

Reason:

Mixed-currency orders are not supported.

---

## ORDER-006

Order snapshot data must never change after order creation.

Protected Fields:

- product_name
- variant_name
- sku
- unit_price_amount_minor
- currency

Reason:

Historical order correctness.

---

## ORDER-007

Order status transitions must follow the allowed lifecycle.

Allowed:

PendingPayment → Paid

PendingPayment → Cancelled

PendingPayment → Expired

Paid → Shipped

All other transitions are invalid.

---

## ORDER-008

Only Paid orders may become Shipped.

Reason:

Shipment of unpaid orders is prohibited.

---

## ORDER-009

Cancelled orders cannot become Paid.

---

## ORDER-010

Expired orders cannot become Paid.

---

# Inventory Invariants

## INVENTORY-001

Stock quantities must never be negative.

Protected Fields:

- on_hand_quantity
- reserved_quantity

---

## INVENTORY-002

Reserved quantity must never exceed on-hand quantity.

Formula:

reserved_quantity
≤
on_hand_quantity

---

## INVENTORY-003

Available quantity is derived.

Formula:

available_quantity
=
on_hand_quantity
-
reserved_quantity

---

## INVENTORY-004

Available quantity must never be negative.

---

## INVENTORY-005

Reservation quantity must be greater than zero.

---

## INVENTORY-006

Only Active reservations may be Released.

---

## INVENTORY-007

Only Active reservations may be Committed.

---

## INVENTORY-008

Released reservations cannot be Committed.

---

## INVENTORY-009

Committed reservations cannot be Released.

---

## INVENTORY-010

Every stock-changing operation produces a stock movement record.

Reason:

Auditability.

---

# Payment Invariants

## PAYMENT-001

Payment amount must be greater than zero.

---

## PAYMENT-002

Payment currency must match the associated order currency.

---

## PAYMENT-003

Payment status transitions must follow lifecycle.

Allowed:

Pending → Succeeded

Pending → Failed

Pending → Cancelled

---

## PAYMENT-004

Succeeded payments are immutable.

---

## PAYMENT-005

Failed payments are immutable.

---

## PAYMENT-006

Payment success must never directly modify inventory.

Reason:

Inventory ownership belongs to Inventory and Ordering.

---

# Fulfillment Invariants

## FULFILLMENT-001

A shipment must reference an existing order.

---

## FULFILLMENT-002

A shipment may be created only for a Paid order.

---

## FULFILLMENT-003

Shipment lifecycle must follow:

Pending → Shipped

Pending → Cancelled

---

## FULFILLMENT-004

Cancelled shipments cannot become Shipped.

---

## FULFILLMENT-005

Shipped shipments cannot become Cancelled.

---

# Cross-Service Invariants

## CROSS-001

Catalog owns current price.

Ordering owns historical order price.

Reason:

Historical orders must not change when Catalog changes.

---

## CROSS-002

Clients never provide trusted prices.

Reason:

Price integrity.

---

## CROSS-003

Clients never provide trusted order totals.

Reason:

Financial integrity.

---

## CROSS-004

Clients never provide trusted payment amounts.

Reason:

Financial integrity.

Future payment creation should derive amount from order total.

---

## CROSS-005

Clients never provide trusted inventory information.

Reason:

Inventory authority belongs to Inventory Service.

---

## CROSS-006

Identifiers are selectors, not authorization proof.

Examples:

- orderId
- cartId
- paymentId
- shipmentId

Reason:

Authorization must be verified independently.

---

## CROSS-007

Ordering owns inventory reservation lifecycle coordination.

Ordering may:

- allocate reservation;
- release reservation;
- request reservation commit.

Fulfillment must not call Inventory directly.

Payment must not call Inventory directly.

---

## CROSS-008

Inventory reservation commit occurs only during order shipment.

Current Flow:

Paid Order
→ Shipment Shipped
→ Ordering mark-shipped
→ Inventory Commit

---

## CROSS-009

Payment success does not imply shipment.

Reason:

Paid and shipped are distinct business states.

---

## CROSS-010

Shipment creation does not imply inventory commit.

Reason:

Inventory commit occurs only when shipment is actually shipped.

---

# Audit Invariants

## AUDIT-001

Every critical state transition should eventually become auditable.

Examples:

- Product Published
- Product Archived
- Reservation Allocated
- Reservation Released
- Reservation Committed
- Order Paid
- Order Shipped
- Payment Succeeded
- Shipment Shipped

---

## AUDIT-002

Audit records must never become the authoritative source of business state.

Reason:

Audit data is evidence, not ownership.

---

# Non-Negotiable Rules

## Rule 1

Money is integrity-critical.

## Rule 2

Inventory is integrity-critical.

## Rule 3

Status transitions are integrity-critical.

## Rule 4

Ownership is explicit.

## Rule 5

Authority is singular.

## Rule 6

Snapshots preserve history.

## Rule 7

Cross-service references do not imply ownership.