# Ecommerce Distributed

Small distributed commerce system.

## Services

* `catalog` — products, brands, categories, variants/SKUs
* `inventory` — stock and warehouse operations
* `ordering` — orders
* `payment` — payment simulation
* `frontend/angular-app` — Angular UI

## Current Catalog flow

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

## Local run

```bash
docker compose up -d
```

Run Catalog API locally against Docker database:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=catalog_db;Username=postgres;Password=postgres" \
dotnet run --project services/catalog/Catalog.Api/Catalog.Api.csproj
```

## Tests

```bash
dotnet test services/catalog/Catalog.sln
```
