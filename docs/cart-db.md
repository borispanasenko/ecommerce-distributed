# CartDb v1

## Purpose

`CartDb` stores shopping carts and cart items for the e-commerce system.

Cart Service owns:

* carts;
* cart items;
* product variant references inside carts;
* item quantities.

Cart Service does **not** own:

* catalog product data;
* product names;
* product descriptions;
* product prices;
* SKUs as trusted product data;
* stock balances;
* inventory reservations;
* orders;
* payments;
* customers.

Catalog Service owns product data and current product prices.

Inventory Service owns stock and reservations.

Ordering Service owns orders and product snapshots inside orders.

Payment Service owns payments.

Cart Service stores only product variant references and quantities. It does not store trusted product names, prices, SKUs, currencies or stock data.

Other services may reference cart data by `cart_id` only if there is a clear business reason, but they must not read or write `CartDb` directly.

---

## Design principles

1. Cart owns the pre-checkout shopping cart state.
2. Cart items store `product_variant_id` and `quantity`.
3. Cart does not reserve Inventory stock.
4. Cart does not store trusted product names, prices, SKUs or currencies.
5. Cart does not create orders.
6. Cart does not process payments.
7. Ordering validates product variants and prices through Catalog when creating an order.
8. Inventory allocates stock reservations during order creation, not during cart changes.
9. Tables use UUID primary keys.
10. Carts and cart items have `created_at` and `updated_at`.

---

## Tables

## carts

Represents a shopping cart.

| Column     | Type        | Constraints |
| ---------- | ----------- | ----------- |
| id         | uuid        | PK          |
| created_at | timestamptz | NOT NULL    |
| updated_at | timestamptz | NOT NULL    |

Notes:

* A cart can contain zero or more cart items.
* A cart can be used as a guest cart.
* Customer ownership is currently out of scope.
* Cart expiration is currently out of scope.

Rules:

* `created_at` is set when the cart is created.
* `updated_at` is updated when cart items are added, updated, removed or cleared.

---

## cart_items

Represents one product variant inside a cart.

| Column             | Type        | Constraints   |
| ------------------ | ----------- | ------------- |
| id                 | uuid        | PK            |
| cart_id            | uuid        | FK → carts.id |
| product_variant_id | uuid        | NOT NULL      |
| quantity           | integer     | NOT NULL      |
| created_at         | timestamptz | NOT NULL      |
| updated_at         | timestamptz | NOT NULL      |

Notes:

* `product_variant_id` references a Catalog product variant identity.
* Cart Service does not validate or snapshot product name, SKU, price or currency.
* Adding the same `product_variant_id` again increases quantity.
* Updating a cart item replaces its quantity.
* Removing a cart item deletes it from the cart.

Rules:

* `quantity` must be greater than `0`.
* One cart cannot contain duplicate rows for the same `product_variant_id`.
* The unique identity of a cart item from a user-flow perspective is `(cart_id, product_variant_id)`.

---

## Cart item data

Cart items store:

```text
product_variant_id
quantity
```

Reason:

```text
Cart is pre-checkout state.
Catalog data and prices can change before checkout.
Inventory availability can change before checkout.
Ordering must load trusted Catalog snapshot data when creating an order.
Inventory must allocate stock during order creation.
```

Example:

```text
Cart item:
product_variant_id = 9572fb9d-f059-401e-9041-7fc75f8cb414
quantity = 2

Order item later:
product_id
product_variant_id
sku
product_name
variant_name
unit_price_amount_minor
currency
quantity
inventory_reservation_id
```

---

# Relationships summary

```text
carts 1 ─── * cart_items
```

External references:

```text
cart_items.product_variant_id -> Catalog product variant identity
```

This is a cross-service reference only. Cart Service does not read or write `CatalogDb` directly.

---

# Constraints and indexes

Implemented constraints:

```text
cart_items.quantity > 0
```

Implemented indexes:

```text
cart_items(cart_id, product_variant_id) UNIQUE
```

Recommended lookup indexes:

```text
cart_items.cart_id
cart_items.product_variant_id
```

Recommended future indexes:

```text
carts.updated_at
```

---

# Out of scope for CartDb v1

The following are intentionally excluded:

* customer accounts;
* cart ownership by customer;
* cart expiration;
* cart merge after login;
* saved carts;
* wishlists;
* product snapshots;
* price snapshots;
* stock availability snapshots;
* discounts;
* promotions;
* tax calculation;
* shipping calculation;
* inventory reservations;
* orders;
* payments;
* direct reads from CatalogDb;
* direct reads from InventoryDb;
* direct reads from OrderingDb;
* direct reads from PaymentDb.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Cart Service currently supports:

```text
GET    /health

POST   /api/carts
GET    /api/carts/{id}

POST   /api/carts/{id}/items
PUT    /api/carts/{id}/items/{productVariantId}
DELETE /api/carts/{id}/items/{productVariantId}

POST   /api/carts/{id}/clear
```

Current cart flow:

```text
Create cart
Get cart details
Add product variant to cart
Increase quantity when adding the same product variant again
Update cart item quantity
Remove cart item
Clear cart
```

Current behavior:

```text
Carts can be created without customer identity.
Cart items contain product_variant_id and quantity.
Cart items do not contain product name, variant name, SKU, price or currency.
Cart items do not reserve stock.
Adding the same product_variant_id again increases quantity.
Updating an item replaces quantity.
Removing an item deletes it from the cart.
Clearing cart removes all cart items.
Requests with missing product_variant_id are rejected.
Requests with invalid quantity are rejected.
Requests for missing carts are rejected.
Requests for missing cart items are rejected.
```

Current tests:

```text
Create cart tests
Add item tests
Increase quantity tests
Update item tests
Remove item tests
Clear cart tests
Validation tests
Missing cart tests
Missing cart item tests
```

Future work:

```text
Add customer identity or guest ownership.
Add cart expiration.
Add cart merge after login.
Add checkout handoff from Cart to Ordering.
Add optional read model enrichment from Catalog for display.
Add idempotency for cart mutations.
Add optimistic concurrency for cart updates.
```
