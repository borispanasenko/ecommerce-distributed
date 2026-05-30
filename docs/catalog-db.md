# CatalogDb v1

## Purpose

`CatalogDb` stores product catalog data for the e-commerce system.

Catalog Service owns:

* products;
* product variants / SKUs;
* categories;
* brands;
* product images;
* current product prices.

Catalog Service does **not** own:

* inventory quantities;
* stock reservations;
* carts;
* orders;
* payments;
* shipments;
* customers.

Other services may reference catalog data by `product_id`, `product_variant_id` or `sku`, but they must not read or write `CatalogDb` directly.

---

## Design principles

1. Catalog owns descriptive product data.
2. Inventory owns stock and reservations.
3. Ordering stores product snapshots inside orders.
4. Prices are stored in minor units to avoid decimal precision issues.
5. Product variants represent sellable units.
6. Categories support hierarchy through `parent_id`.
7. Tables use UUID primary keys.
8. Public-facing identifiers use unique slugs where appropriate.
9. All main entities have `created_at` and `updated_at`.

---

## Tables

## brands

Represents product brands or manufacturers.

| Column     | Type        | Constraints      |
| ---------- | ----------- | ---------------- |
| id         | uuid        | PK               |
| name       | text        | NOT NULL         |
| slug       | text        | NOT NULL, UNIQUE |
| created_at | timestamptz | NOT NULL         |
| updated_at | timestamptz | NOT NULL         |

Notes:

* `brand_id` in `products` is nullable because not every product must have a brand.
* For v1, `brands` is enough. No separate `manufacturers` table.

---

## categories

Represents product categories. Supports nested categories.

| Column      | Type        | Constraints              |
| ----------- | ----------- | ------------------------ |
| id          | uuid        | PK                       |
| parent_id   | uuid        | FK → categories.id, NULL |
| name        | text        | NOT NULL                 |
| slug        | text        | NOT NULL, UNIQUE         |
| description | text        | NULL                     |
| is_active   | boolean     | NOT NULL                 |
| sort_order  | integer     | NOT NULL                 |
| created_at  | timestamptz | NOT NULL                 |
| updated_at  | timestamptz | NOT NULL                 |

Relationships:

* One category can have many child categories.
* One category can be assigned to many products through `product_categories`.

Rules:

* `parent_id` can be null for root categories.
* Category deletion should be restricted if products are assigned to it.

---

## products

Represents the main product card.

| Column      | Type        | Constraints          |
| ----------- | ----------- | -------------------- |
| id          | uuid        | PK                   |
| brand_id    | uuid        | FK → brands.id, NULL |
| name        | text        | NOT NULL             |
| slug        | text        | NOT NULL, UNIQUE     |
| description | text        | NULL                 |
| status      | integer     | NOT NULL             |
| created_at  | timestamptz | NOT NULL             |
| updated_at  | timestamptz | NOT NULL             |

Product statuses:

| Value | Name     | Meaning                                                |
| ----- | -------- | ------------------------------------------------------ |
| 0     | Draft    | Product is not visible to customers                    |
| 1     | Active   | Product is visible and sellable if variants are active |
| 2     | Archived | Product is no longer sold                              |

Notes:

* `products` does not store stock quantity.
* `products` does not store order-specific data.
* `products` can have multiple variants.

---

## product_categories

Many-to-many relationship between products and categories.

| Column      | Type | Constraints            |
| ----------- | ---- | ---------------------- |
| product_id  | uuid | PK, FK → products.id   |
| category_id | uuid | PK, FK → categories.id |

Primary key:

```text
(product_id, category_id)
```

Rules:

* A product can belong to multiple categories.
* A category can contain multiple products.
* Deleting a product can cascade-delete rows from `product_categories`.
* Deleting a category should be restricted if assigned products exist.

---

## product_variants

Represents a concrete sellable SKU.

Examples:

* T-shirt / Black / M;
* T-shirt / Black / L;
* Laptop / 16GB RAM / 512GB SSD.

| Column             | Type        | Constraints      |
| ------------------ | ----------- | ---------------- |
| id                 | uuid        | PK               |
| product_id         | uuid        | FK → products.id |
| sku                | text        | NOT NULL, UNIQUE |
| name               | text        | NOT NULL         |
| price_amount_minor | bigint      | NOT NULL         |
| currency           | char(3)     | NOT NULL         |
| is_active          | boolean     | NOT NULL         |
| created_at         | timestamptz | NOT NULL         |
| updated_at         | timestamptz | NOT NULL         |

Notes:

* `price_amount_minor` stores money in minor units.

  * Example: `1999` means `19.99`.
* `currency` uses ISO-like currency codes such as `USD`, `EUR`, `UAH`.
* Inventory Service should track stock by `product_variant_id` or `sku`, not by `product_id`.

Rules:

* A product must have at least one variant to be sellable.
* A variant can be inactive even if the product is active.
* `sku` is the stable business identifier for a sellable item.

---

## product_images

Stores product and variant images.

| Column     | Type        | Constraints                    |
| ---------- | ----------- | ------------------------------ |
| id         | uuid        | PK                             |
| product_id | uuid        | FK → products.id               |
| variant_id | uuid        | FK → product_variants.id, NULL |
| url        | text        | NOT NULL                       |
| alt_text   | text        | NULL                           |
| sort_order | integer     | NOT NULL                       |
| is_primary | boolean     | NOT NULL                       |
| created_at | timestamptz | NOT NULL                       |

Notes:

* If `variant_id` is null, the image belongs to the general product.
* If `variant_id` is not null, the image is specific to one variant.
* `sort_order` controls display order.

Rules:

* A product can have many images.
* A variant can have variant-specific images.
* At application level, only one primary image should exist per product/variant image group.

---

# Relationships summary

```text
brands 1 ─── * products

categories 1 ─── * categories
categories * ─── * products via product_categories

products 1 ─── * product_variants
products 1 ─── * product_images

product_variants 1 ─── * product_images
```

---

# Constraints and indexes

Recommended unique indexes:

```text
brands.slug UNIQUE
categories.slug UNIQUE
products.slug UNIQUE
product_variants.sku UNIQUE
```

Recommended lookup indexes:

```text
products.brand_id
product_categories.category_id
product_variants.product_id
product_images.product_id
product_images.variant_id
categories.parent_id
```

Recommended checks:

```text
product_variants.price_amount_minor >= 0
length(product_variants.currency) = 3
categories.sort_order >= 0
product_images.sort_order >= 0
```

---

# Out of scope for CatalogDb v1

The following are intentionally excluded:

* stock quantities;
* inventory reservations;
* carts;
* orders;
* customer data;
* payments;
* delivery;
* reviews;
* discounts;
* promotions;
* price history;
* product search index;
* product attributes system;
* localization;
* suppliers;
* accounting.

These may be added later only if there is a clear business reason.

---

# First implementation milestone

Catalog Service v1 should support:

```text
GET /products
GET /products/{id}
GET /categories
```

The first version should:

* read data from `catalog_db`;
* use EF Core migrations;
* include seed data;
* expose product variants and categories;
* not depend on other services.
