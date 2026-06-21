# InventoryDb v1

## Purpose

`InventoryDb` stores warehouse stock data for the e-commerce system.

Inventory Service owns:

* warehouses;
* storage locations;
* stock balances;
* stock movements;
* stock reservations.

Inventory Service does **not** own:

* product catalog data;
* product descriptions;
* product prices;
* carts;
* orders;
* payments;
* customers;
* delivery records.

Catalog Service defines SKUs. Inventory Service stores stock by `sku`.

Other services may reference inventory data by `sku`, `warehouse_id`, `location_id` or `inventory_reservation_id`, but they must not read or write `InventoryDb` directly.

---

## Design principles

1. Inventory owns stock and reservations.
2. Catalog owns product and SKU definitions.
3. Ordering may reference Inventory reservations, but does not own stock.
4. Stock is tracked per SKU, warehouse and storage location.
5. `on_hand_quantity` represents physical stock.
6. `reserved_quantity` represents stock allocated to future operations.
7. `available_quantity` is calculated as `on_hand_quantity - reserved_quantity`.
8. Stock changes are recorded as movements.
9. Tables use UUID primary keys.
10. All main stateful entities have `created_at` and `updated_at`.

---

## Tables

## warehouses

Represents a physical or logical warehouse.

| Column     | Type        | Constraints      |
| ---------- | ----------- | ---------------- |
| id         | uuid        | PK               |
| code       | text        | NOT NULL, UNIQUE |
| name       | text        | NOT NULL         |
| is_active  | boolean     | NOT NULL         |
| created_at | timestamptz | NOT NULL         |
| updated_at | timestamptz | NOT NULL         |

Notes:

* `code` is the stable business identifier for a warehouse.
* Warehouses are created as active.

Rules:

* Warehouse code must be unique.
* Locations, stock items, movements and reservations reference warehouses.

---

## storage_locations

Represents a storage location inside a warehouse.

| Column       | Type        | Constraints        |
| ------------ | ----------- | ------------------ |
| id           | uuid        | PK                 |
| warehouse_id | uuid        | FK → warehouses.id |
| code         | text        | NOT NULL           |
| is_active    | boolean     | NOT NULL           |
| created_at   | timestamptz | NOT NULL           |
| updated_at   | timestamptz | NOT NULL           |

Unique key:

```text
(warehouse_id, code)
```

Example:

```text
Warehouse: MAIN
Location: A-01-01
```

Rules:

* A location belongs to one warehouse.
* Location code must be unique inside one warehouse.
* A location can contain multiple SKUs.
* Deleting a warehouse should be restricted if locations exist.

---

## stock_items

Represents current stock balance for one SKU in one warehouse location.

| Column            | Type        | Constraints               |
| ----------------- | ----------- | ------------------------- |
| id                | uuid        | PK                        |
| sku               | text        | NOT NULL                  |
| warehouse_id      | uuid        | FK → warehouses.id        |
| location_id       | uuid        | FK → storage_locations.id |
| on_hand_quantity  | bigint      | NOT NULL                  |
| reserved_quantity | bigint      | NOT NULL                  |
| created_at        | timestamptz | NOT NULL                  |
| updated_at        | timestamptz | NOT NULL                  |

Unique key:

```text
(sku, warehouse_id, location_id)
```

Calculated value:

```text
available_quantity = on_hand_quantity - reserved_quantity
```

Rules:

* `on_hand_quantity` must be greater than or equal to `0`.
* `reserved_quantity` must be greater than or equal to `0`.
* `reserved_quantity` must not be greater than `on_hand_quantity`.
* Inventory stock is tracked by `sku`, not by `product_id`.

---

## stock_movements

Represents stock history.

| Column       | Type        | Constraints               |
| ------------ | ----------- | ------------------------- |
| id           | uuid        | PK                        |
| sku          | text        | NOT NULL                  |
| warehouse_id | uuid        | FK → warehouses.id        |
| location_id  | uuid        | FK → storage_locations.id |
| type         | integer     | NOT NULL                  |
| quantity     | bigint      | NOT NULL                  |
| reason       | text        | NULL                      |
| created_at   | timestamptz | NOT NULL                  |

Movement types:

| Value | Name        | Meaning                       |
| ----- | ----------- | ----------------------------- |
| 1     | Receipt     | Stock was received            |
| 2     | Adjustment  | Stock was manually adjusted   |
| 3     | Shipment    | Stock was committed / shipped |
| 4     | Reservation | Stock was reserved            |
| 5     | Release     | Reserved stock was released   |

Current implemented movements:

| Type        | Quantity sign | Effect                                          |
| ----------- | ------------- | ----------------------------------------------- |
| Receipt     | positive      | Increases `on_hand_quantity`                    |
| Reservation | positive      | Increases `reserved_quantity`                   |
| Release     | negative      | Decreases `reserved_quantity`                   |
| Shipment    | negative      | Decreases `on_hand_quantity` and reserved stock |

Rules:

* `quantity` must not be `0`.
* Every stock receipt creates a movement.
* Every reservation creates a movement.
* Every release creates a movement.
* Every commit creates a shipment movement.

---

## stock_reservations

Represents stock reserved for a future operation, usually an order.

| Column       | Type        | Constraints               |
| ------------ | ----------- | ------------------------- |
| id           | uuid        | PK                        |
| sku          | text        | NOT NULL                  |
| warehouse_id | uuid        | FK → warehouses.id        |
| location_id  | uuid        | FK → storage_locations.id |
| quantity     | bigint      | NOT NULL                  |
| status       | integer     | NOT NULL                  |
| reference    | text        | NULL                      |
| created_at   | timestamptz | NOT NULL                  |
| released_at  | timestamptz | NULL                      |
| committed_at | timestamptz | NULL                      |

Reservation statuses:

| Value | Name      | Meaning                             |
| ----- | --------- | ----------------------------------- |
| 1     | Active    | Stock is currently reserved         |
| 2     | Released  | Reservation was released            |
| 3     | Committed | Reservation was committed / shipped |

Flow:

```text
Active -> Released
Active -> Committed
```

Notes:

* `reference` can store an external business reference, for example an order number.
* Ordering may store `inventory_reservation_id` on an order item.

Rules:

* Reservation quantity must be greater than `0`.
* Reservation requires enough available stock.
* Released reservation cannot be committed.
* Committed reservation cannot be released.
* Only active reservations can be released or committed.

---

# Relationships summary

```text
warehouses 1 ─── * storage_locations

warehouses 1 ─── * stock_items
warehouses 1 ─── * stock_movements
warehouses 1 ─── * stock_reservations

storage_locations 1 ─── * stock_items
storage_locations 1 ─── * stock_movements
storage_locations 1 ─── * stock_reservations
```

---

# Constraints and indexes

Recommended unique indexes:

```text
warehouses.code UNIQUE
storage_locations(warehouse_id, code) UNIQUE
stock_items(sku, warehouse_id, location_id) UNIQUE
```

Recommended lookup indexes:

```text
stock_items.sku
stock_items.warehouse_id
stock_items.location_id

stock_movements.sku
stock_movements.warehouse_id
stock_movements.location_id
stock_movements.created_at

stock_reservations.sku
stock_reservations.status
stock_reservations.reference
stock_reservations(sku, warehouse_id, location_id, status)
```

Recommended checks:

```text
stock_items.on_hand_quantity >= 0
stock_items.reserved_quantity >= 0
stock_items.reserved_quantity <= stock_items.on_hand_quantity

stock_movements.quantity <> 0

stock_reservations.quantity > 0
```

---

# Out of scope for InventoryDb v1

The following are intentionally excluded:

* direct product catalog storage;
* product prices;
* cross-service SKU validation;
* warehouse transfers;
* cycle counting;
* lot tracking;
* batch tracking;
* serial numbers;
* expiration dates;
* supplier receipts;
* purchase orders;
* delivery tracking;
* inventory valuation;
* accounting.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Inventory Service currently supports:

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
POST /api/stock/reservations/allocate
POST /api/stock/reservations/{id}/release
POST /api/stock/reservations/{id}/commit
```

Current stock flow:

```text
Receive stock
Reserve stock explicitly
Allocate stock reservation by SKU
Release reservation
Commit reservation
```

Current behavior:

```text
Warehouses are created as active.
Storage locations are created as active.
Stock receipts increase on-hand quantity.
Reservations increase reserved quantity.
Releases decrease reserved quantity.
Commits decrease both on-hand and reserved quantity.
GET /api/stock/{sku} returns total and per-location stock.
GET /api/stock/movements returns stock movement history.
```

Current tests:

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
