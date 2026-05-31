# InventoryDb v1

## Purpose

`InventoryDb` stores warehouse stock data.

Inventory Service owns:

```text
warehouses
storage locations
stock balances
stock movements
stock reservations
```

Inventory Service does not own:

```text
product catalog data
prices
orders
payments
customers
shipments as delivery records
```

Catalog defines SKUs. Inventory stores stock by SKU.

---

## Main tables

```text
warehouses
storage_locations
stock_items
stock_movements
stock_reservations
```

---

## warehouses

Represents a physical or logical warehouse.

```text
id
code
name
is_active
created_at
updated_at
```

Rules:

```text
warehouse code is unique
warehouse is created as active
```

---

## storage_locations

Represents a storage location inside a warehouse.

```text
id
warehouse_id
code
is_active
created_at
updated_at
```

Rules:

```text
location code is unique inside one warehouse
location belongs to one warehouse
```

Example:

```text
Warehouse: MAIN
Location: A-01-01
```

---

## stock_items

Represents current stock balance for one SKU in one warehouse location.

```text
id
sku
warehouse_id
location_id
on_hand_quantity
reserved_quantity
created_at
updated_at
```

Calculated value:

```text
available_quantity = on_hand_quantity - reserved_quantity
```

Rules:

```text
on_hand_quantity >= 0
reserved_quantity >= 0
reserved_quantity <= on_hand_quantity
sku + warehouse_id + location_id is unique
```

---

## stock_movements

Represents stock history.

```text
id
sku
warehouse_id
location_id
type
quantity
reason
created_at
```

Movement types:

```text
Receipt
Adjustment
Shipment
Reservation
Release
```

Current implemented movements:

```text
Receipt      - increases on-hand stock
Reservation  - increases reserved stock
Release      - decreases reserved stock
Shipment     - decreases on-hand and reserved stock
```

---

## stock_reservations

Represents stock reserved for a future operation, usually an order.

```text
id
sku
warehouse_id
location_id
quantity
status
reference
created_at
released_at
committed_at
```

Statuses:

```text
Active
Released
Committed
```

Flow:

```text
Active -> Released
Active -> Committed
```

Rules:

```text
reservation quantity must be > 0
reservation requires available stock
released reservation cannot be committed
committed reservation cannot be released
```

---

## Current Inventory flow

```text
Create warehouse
Create storage location
Receive stock
Get stock by SKU
Reserve stock
Release reservation
Commit reservation
Get stock movements
```

Example:

```text
Receive 75 units

OnHand   = 75
Reserved = 0
Available = 75

Reserve 10 units

OnHand   = 75
Reserved = 10
Available = 65

Release 10 units

OnHand   = 75
Reserved = 0
Available = 75

Reserve 15 units and commit

OnHand   = 60
Reserved = 0
Available = 60
```

---

## Current API

```text
GET  /health

GET  /api/warehouses
POST /api/warehouses

GET  /api/locations
POST /api/locations

POST /api/stock/receipts
GET  /api/stock/{sku}
GET  /api/stock/movements

POST /api/stock/reservations
POST /api/stock/reservations/{id}/release
POST /api/stock/reservations/{id}/commit
```

---

## Current tests

```text
Warehouse tests
Location tests
Stock receipt tests
Stock summary tests
Stock movement tests
Reservation tests
Release tests
Commit tests
```
