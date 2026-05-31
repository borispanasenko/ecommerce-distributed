# Ecommerce Distributed

Small distributed commerce system.

## Services

```text
catalog   - products, brands, categories, variants/SKUs
inventory - warehouses, locations, stock, reservations
ordering  - orders
payment   - payment simulation
frontend  - Angular UI
```

## Current status

```text
Catalog   - working backend flow, API, tests, documentation
Inventory - working backend flow, API, tests, documentation
Ordering  - scaffold
Payment   - scaffold
Frontend  - scaffold
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
Reserve stock
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
POST /api/stock/reservations/{id}/release
POST /api/stock/reservations/{id}/commit
```

## Local infrastructure

Start databases and services:

```bash
docker compose up -d
```

Catalog database from host:

```text
Host=localhost;Port=5433;Database=catalog_db;Username=postgres;Password=postgres
```

Inventory database from host:

```text
Host=localhost;Port=5435;Database=inventory_db;Username=postgres;Password=postgres
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

## Tests

Catalog:

```bash
dotnet test services/catalog/Catalog.sln
```

Inventory:

```bash
dotnet test services/inventory/Inventory.sln
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

Files ending with `.readonly.http` contain only safe read-only requests.

## Documentation

```text
docs/catalog-db.md
docs/inventory-db.md
docs/architecture.md
docs/local-development.md
docs/messages.md
```

## Boundaries

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
```

Catalog does not store stock.

Inventory does not store product descriptions or prices.

Inventory stores stock by SKU.
