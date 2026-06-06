# Ecommerce Distributed

Small distributed commerce system.

## Services

```text
catalog   - products, brands, categories, variants/SKUs and current prices
inventory - warehouses, locations, stock, reservations and stock allocation
ordering  - orders, product snapshots and inventory reservation references
payment   - payment records and payment simulation
frontend  - Angular UI
```

## Current status

```text
Catalog   - working backend flow, API, tests, documentation
Inventory - working backend flow, API, tests, documentation
Ordering  - working backend flow, Catalog and Inventory integration, API, tests, documentation
Payment   - working backend flow, Ordering integration, API, tests, documentation
Frontend  - working catalog browsing, cart, checkout and payment flow
```

## Catalog flow

```text
Create brand
Create category
Create product as Draft
Add product variant/SKU
Publish product
List active products
Get product details
Get active product variant snapshot
Archive product
```

## Catalog API

```text
GET  /health

GET  /api/brands
POST /api/brands

GET  /api/categories
POST /api/categories

GET  /api/products
GET  /api/products/{id}
GET  /api/products/variants/{variantId}/snapshot
POST /api/products
POST /api/products/{id}/variants
POST /api/products/{id}/publish
POST /api/products/{id}/archive
```

## Inventory flow

```text
Create warehouse
Create storage location
Receive stock for SKU
Get stock by SKU
Reserve stock explicitly
Allocate stock reservation by SKU
Release reservation
Commit reservation
Get stock movements
```

## Inventory API

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

## Ordering flow

```text
Create order from product variant IDs and quantities
Load product snapshots from Catalog
Allocate Inventory stock reservations by SKU
Store inventoryReservationId on order item
Calculate line totals
Calculate order total
List orders
Get order details
Cancel order
Release Inventory reservation
Mark order as Paid
Commit Inventory reservation
```

## Ordering API

```text
GET  /health

GET  /api/orders
GET  /api/orders/{id}
POST /api/orders
POST /api/orders/{id}/cancel
POST /api/orders/{id}/mark-paid
```

## Payment flow

```text
Create payment for order
List payments
Get payment details
Mark payment as succeeded
Mark linked order as Paid through Ordering
Commit Inventory reservation through Ordering
Mark payment as failed
Reject status changes after payment completion
```

## Payment API

```text
GET  /health

GET  /api/payments
GET  /api/payments/{id}
POST /api/payments
POST /api/payments/{id}/succeed
POST /api/payments/{id}/fail
```

## End-to-end flows

```text
Catalog defines product variants, SKUs and current prices.
Inventory stores stock by SKU.
Ordering creates orders from product variant IDs and quantities.
Ordering loads trusted product snapshots from Catalog.
Ordering asks Inventory to allocate stock reservations by SKU.
Ordering releases Inventory reservation when an order is cancelled.
Payment stores simulated payment records for orders.
Payment calls Ordering when a pending payment succeeds.
Ordering marks the order as Paid.
Ordering commits Inventory reservation when order is marked as Paid.
```

Current MVP simplification:

```text
Payment success currently leads to Inventory reservation commit through Ordering.
In a fuller commerce flow, Inventory commit should move closer to fulfillment/shipment.
```

Completed scenarios:

```text
Cancel flow:
Product exists in Catalog
Stock exists in Inventory
Order is created in Ordering from product variant ID and quantity
Ordering loads product snapshot from Catalog
Inventory stock reservation is allocated
Order stores inventoryReservationId
Order is cancelled
Inventory reservation is released
```

```text
Payment success flow:
Product exists in Catalog
Stock exists in Inventory
Order is created in Ordering from product variant ID and quantity
Ordering loads product snapshot from Catalog
Inventory stock reservation is allocated
Order stores inventoryReservationId
Payment is created for the order
Payment is marked as Succeeded
Payment calls Ordering to mark the order as Paid
Order is marked as Paid
Ordering commits Inventory reservation
Inventory on-hand stock is decreased
```

```text
Payment failure flow:
Payment is created
Payment is marked as Failed
Payment remains terminal Failed
Payment failure does not call Ordering
No Inventory reservation is committed by Payment failure
```

## Local infrastructure

Start databases and services:

```bash
docker compose up -d
```

Docker Compose local service ports:

```text
Catalog API   - http://localhost:5001
Ordering API  - http://localhost:5002
Inventory API - http://localhost:5003
Payment API   - http://localhost:5004
```

Inside Docker Compose, services use internal service names:

```text
Ordering -> Catalog:   http://catalog-api:8080
Ordering -> Inventory: http://inventory-api:8080
Payment  -> Ordering:  http://ordering-api:8080
```

Catalog database from host:

```text
Host=localhost;Port=5433;Database=catalog_db;Username=postgres;Password=postgres
```

Ordering database from host:

```text
Host=localhost;Port=5434;Database=ordering_db;Username=postgres;Password=postgres
```

Inventory database from host:

```text
Host=localhost;Port=5435;Database=inventory_db;Username=postgres;Password=postgres
```

Payment database from host:

```text
Host=localhost;Port=5436;Database=payment_db;Username=postgres;Password=postgres
```

## Run Catalog API locally

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=catalog_db;Username=postgres;Password=postgres" \
dotnet run --project services/catalog/Catalog.Api/Catalog.Api.csproj
```

## Run Inventory API locally

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5435;Database=inventory_db;Username=postgres;Password=postgres" \
dotnet run --project services/inventory/Inventory.Api/Inventory.Api.csproj
```

## Run Ordering API locally

Ordering API requires Catalog API for product snapshots and Inventory API for stock reservations.

Run Catalog API and Inventory API first.

Then run Ordering API:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=ordering_db;Username=postgres;Password=postgres" \
CatalogApi__BaseUrl="http://localhost:5001" \
InventoryApi__BaseUrl="http://localhost:5245" \
dotnet run --project services/ordering/Ordering.Api/Ordering.Api.csproj
```

## Run Payment API locally

Payment API requires Ordering API for successful payment flow.

Run Catalog API, Inventory API and Ordering API first.

Then run Payment API:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5436;Database=payment_db;Username=postgres;Password=postgres" \
OrderingApi__BaseUrl="http://localhost:5172" \
dotnet run --project services/payment/Payment.Api/Payment.Api.csproj
```

## Tests

Catalog:

```bash
dotnet test services/catalog/Catalog.sln
```

Inventory:

```bash
dotnet test services/inventory/Inventory.sln
```

Ordering:

```bash
dotnet test services/ordering/Ordering.sln
```

Payment:

```bash
dotnet test services/payment/Payment.sln
```

## Manual API checks

Catalog:

```text
services/catalog/Catalog.Api/Catalog.Api.http
services/catalog/Catalog.Api/Catalog.Api.readonly.http
```

Inventory:

```text
services/inventory/Inventory.Api/Inventory.Api.http
services/inventory/Inventory.Api/Inventory.Api.readonly.http
```

Ordering:

```text
services/ordering/Ordering.Api/Ordering.Api.http
services/ordering/Ordering.Api/Ordering.Api.readonly.http
```

Payment:

```text
services/payment/Payment.Api/Payment.Api.http
services/payment/Payment.Api/Payment.Api.readonly.http
```

Files ending with `.readonly.http` contain only safe read-only requests.

Write `.http` files may change local development data, but flows are designed to finish in terminal states and avoid active dangling reservations.

## Documentation

```text
docs/catalog-db.md
docs/inventory-db.md
docs/ordering-db.md
docs/payment-db.md
docs/architecture.md
docs/local-development.md
docs/messages.md
```

## Service boundaries

Catalog owns:

```text
products
brands
categories
variants/SKUs
product images
current product prices
```

Inventory owns:

```text
warehouses
storage locations
stock balances
stock movements
stock reservations
stock allocation
```

Ordering owns:

```text
orders
order items
order statuses
order totals
product snapshots inside orders
Inventory reservation references inside order items
```

Payment owns:

```text
payments
payment statuses
payment provider references
payment failure reasons
```

## Boundary rules

Catalog does not store stock.

Inventory does not store product descriptions or prices.

Ordering does not store live product data.

Ordering stores product snapshots so old orders do not change when Catalog data changes.

Frontend does not send trusted product prices, product names, SKUs or currencies to Ordering.

Frontend does not choose warehouse or storage location.

Ordering gets product snapshots from Catalog through Catalog API.

Inventory stores stock by SKU.

Inventory chooses warehouse and storage location during stock allocation.

Ordering may reference Catalog data by `product_id`, `product_variant_id` and `sku`.

Ordering may reference Inventory reservations by `inventory_reservation_id`.

Ordering creates Inventory reservations when orders are created.

Ordering releases Inventory reservations when orders are cancelled.

Ordering currently commits Inventory reservations when orders are marked as Paid.

Payment does not store order details.

Payment references orders by `order_id`.

Payment calls Ordering when a pending payment succeeds.

Payment does not write OrderingDb directly.

Payment does not write InventoryDb directly.
