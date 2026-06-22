# Ecommerce Distributed

Small distributed commerce system.

## Services

```text
catalog     - products, brands, categories, variants/SKUs and current prices
inventory   - warehouses, locations, stock, reservations and stock allocation
cart        - shopping carts and cart items
ordering    - orders, product snapshots and inventory reservation references
payment     - payment records and payment simulation
fulfillment - shipments and shipment lifecycle
frontend    - Angular UI
```

## Current status

```text
Catalog     - working backend flow, API, tests, documentation, Docker Compose integration
Inventory   - working backend flow, API, tests, documentation, Docker Compose integration
Cart        - working backend flow, API, tests, documentation, Docker Compose integration, frontend integration
Ordering    - working backend flow, Catalog and Inventory integration, API, tests, documentation, Docker Compose integration
Payment     - working backend flow, Ordering integration, API, tests, documentation, Docker Compose integration
Fulfillment - working backend flow, Ordering integration, API, tests, documentation, Docker Compose integration
Frontend    - working catalog browsing, backend cart integration, checkout and payment flow
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

## Cart flow

```text
Create cart
Get cart details
Add product variant to cart
Increase quantity when adding the same product variant again
Update cart item quantity
Remove cart item
Clear cart
```

## Cart API

```text
GET    /health

POST   /api/carts
GET    /api/carts/{id}

POST   /api/carts/{id}/items
PUT    /api/carts/{id}/items/{productVariantId}
DELETE /api/carts/{id}/items/{productVariantId}

POST   /api/carts/{id}/clear
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
Keep Inventory reservation allocated while order is Paid
Mark paid order as Shipped
Commit Inventory reservation during mark-shipped
```

## Ordering API

```text
GET  /health

GET  /api/orders
GET  /api/orders/{id}
POST /api/orders
POST /api/orders/{id}/cancel
POST /api/orders/{id}/mark-paid
POST /api/orders/{id}/mark-shipped
```

## Payment flow

```text
Create payment for order
List payments
Get payment details
Mark payment as succeeded
Mark linked order as Paid through Ordering
Keep Inventory reservation allocated until the paid order is shipped
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

## Fulfillment flow

```text
Create shipment for paid order
Validate linked order through Ordering
List shipments
Get shipment details
Ship shipment
Mark linked order as Shipped through Ordering
Commit Inventory reservation through Ordering mark-shipped
Cancel pending shipment
Reject shipment changes after terminal status
```

## Fulfillment API

```text
GET  /health

GET  /api/shipments
GET  /api/shipments/{id}
POST /api/shipments
POST /api/shipments/{id}/ship
POST /api/shipments/{id}/cancel
```

## End-to-end flows

```text
Catalog defines product variants, SKUs and current prices.
Inventory stores stock by SKU.
Cart stores product variant IDs and quantities before checkout.
Ordering creates orders from product variant IDs and quantities.
Ordering loads trusted product snapshots from Catalog.
Ordering asks Inventory to allocate stock reservations by SKU.
Ordering releases Inventory reservation when an order is cancelled.
Payment stores simulated payment records for orders.
Payment calls Ordering when a pending payment succeeds.
Ordering marks the order as Paid.
Ordering keeps Inventory reservation allocated while the order is Paid.
Fulfillment validates paid orders through Ordering.
Fulfillment stores shipment records for orders.
Fulfillment calls Ordering when a shipment is shipped.
Ordering commits Inventory reservation during mark-shipped.
Ordering marks the order as Shipped.
```

Current Inventory commit boundary:

```text
Ordering allocates Inventory reservations during order creation.
Ordering releases Inventory reservations when PendingPayment orders are cancelled.
Ordering keeps Inventory reservations allocated when orders are marked as Paid.
Fulfillment triggers mark-shipped through Ordering when shipments are shipped.
Ordering commits Inventory reservations when Paid orders are marked as Shipped.
Fulfillment does not call Inventory directly.
Ordering owns inventory_reservation_id references stored on order items.
```

Completed scenarios:

```text
Cart flow:
Cart is created
Product variant is added to Cart with quantity
Adding the same product variant again increases quantity
Cart item quantity can be updated
Cart item can be removed
Cart can be cleared
Cart does not reserve Inventory stock
Cart does not store trusted product names, prices, SKUs or currencies
```

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
Inventory reservation remains allocated
Inventory reservation is not committed yet
Inventory on-hand stock is not decreased yet
```

```text
Payment failure flow:
Payment is created
Payment is marked as Failed
Payment remains terminal Failed
Payment failure does not call Ordering
No Inventory reservation is committed by Payment failure
```

```text
Shipment flow:
Product exists in Catalog
Stock exists in Inventory
Order is created in Ordering from product variant ID and quantity
Ordering loads product snapshot from Catalog
Inventory stock reservation is allocated
Payment calls Ordering to mark the order as Paid
Inventory reservation remains allocated
Shipment is created in Fulfillment for the Paid order
Fulfillment validates linked order through Ordering
Shipment is shipped
Fulfillment calls Ordering to mark the order as Shipped
Ordering commits Inventory reservation during mark-shipped
Order is marked as Shipped
Inventory on-hand stock is decreased
```

## Local infrastructure

Start databases and services:

```bash
docker compose up -d
```

Docker Compose local service ports:

```text
Catalog API     - http://localhost:5001
Ordering API    - http://localhost:5002
Inventory API   - http://localhost:5003
Payment API     - http://localhost:5004
Cart API        - http://localhost:5005
Fulfillment API - http://localhost:5006
```

Inside Docker Compose, services use internal service names:

```text
Ordering    -> Catalog:  http://catalog-api:8080
Ordering    -> Inventory: http://inventory-api:8080
Payment     -> Ordering: http://ordering-api:8080
Fulfillment -> Ordering: http://ordering-api:8080
```

Cart Service is currently independent from other backend services.

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

Cart database from host:

```text
Host=localhost;Port=5437;Database=cart_db;Username=postgres;Password=postgres
```

Fulfillment database from host:

```text
Host=localhost;Port=5438;Database=fulfillment_db;Username=postgres;Password=postgres
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

## Run Cart API locally

Cart API can be started independently.

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5437;Database=cart_db;Username=postgres;Password=postgres" \
dotnet run --project services/cart/Cart.Api/Cart.Api.csproj
```

## Run Ordering API locally

Ordering API requires Catalog API for product snapshots and Inventory API for stock reservations.

Run Catalog API and Inventory API first.

Then run Ordering API:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=ordering_db;Username=postgres;Password=postgres" \
CatalogApi__BaseUrl="http://localhost:5072" \
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

## Run Fulfillment API locally

Fulfillment API requires Ordering API for shipment creation validation and shipment shipping flow.

Run Ordering API first.

Then run Fulfillment API:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5438;Database=fulfillment_db;Username=postgres;Password=postgres" \
OrderingApi__BaseUrl="http://localhost:5172" \
dotnet run --project services/fulfillment/Fulfillment.Api/Fulfillment.Api.csproj
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

Cart:

```bash
dotnet test services/cart/Cart.sln
```

Ordering:

```bash
dotnet test services/ordering/Ordering.sln
```

Payment:

```bash
dotnet test services/payment/Payment.sln
```

Fulfillment:

```bash
dotnet test services/fulfillment/Fulfillment.sln
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

Cart:

```text
services/cart/Cart.Api/Cart.Api.http
services/cart/Cart.Api/Cart.Api.readonly.http
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

Fulfillment:

```text
services/fulfillment/Fulfillment.Api/Fulfillment.Api.http
services/fulfillment/Fulfillment.Api/Fulfillment.Api.readonly.http
```

Files ending with `.readonly.http` contain only safe read-only requests.

Write `.http` files may change local development data, but flows are designed to finish in terminal states and avoid active dangling reservations.

## Documentation

- Architecture overview & index: [docs/architecture/index.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/index.md)
  - [System Overview](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/system-overview.md)
  - [Main Flows](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/main-flows.md)
  - [Inventory Commit Boundary](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/inventory-commit-boundary.md)
  - [Service Authority Matrix](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/service-authority-matrix.md)
  - [Domain Invariants](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/architecture/domain-invariants.md)
- Security documentation:
  - [Data Trust Model](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/data-trust-model.md)
  - [API Trust Boundaries](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/api-trust-boundaries.md)
  - [Authorization Model](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/authorization-model.md)
  - [Security-Critical Data Registry](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/security-critical-data-registry.md)
  - [Logging and Observability Policy](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/logging-observability-policy.md)
  - [Data Classification Model](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/security/data-classification.md)
- Local development and environment notes: [docs/local-development.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/local-development.md)
- Reliability and idempotency rules: [docs/reliability.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/reliability.md)
- Service-owned data model docs:
  - [docs/services/catalog-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/catalog-db.md)
  - [docs/services/inventory-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/inventory-db.md)
  - [docs/services/cart-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/cart-db.md)
  - [docs/services/ordering-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/ordering-db.md)
  - [docs/services/payment-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/payment-db.md)
  - [docs/services/fulfillment-db.md](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/docs/services/fulfillment-db.md)
- API Specs & BDD Specifications:
  - **Catalog**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/catalog/catalog-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/catalog/catalog-spec.md)
  - **Inventory**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/inventory/inventory-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/inventory/inventory-spec.md)
  - **Cart**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/cart/cart-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/cart/cart-spec.md)
  - **Ordering**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/ordering/ordering-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/ordering/ordering-spec.md)
  - **Payment**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/payment/payment-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/payment/payment-spec.md)
  - **Fulfillment**: [Contract](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/fulfillment/fulfillment-api.yaml) | [BDD Spec](/run/media/borispanasenko/T7_Shield/ecommerce-distributed/specs/fulfillment/fulfillment-spec.md)


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

Cart owns:

```text
carts
cart items
product variant references
quantities
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

Fulfillment owns:

```text
shipments
shipment statuses
carrier information
tracking numbers
shipment timestamps
```

## Boundary rules

Catalog does not store stock.

Inventory does not store product descriptions or prices.

Cart stores productVariantId and quantity only.

Cart does not store trusted product prices, product names, SKUs, currencies or stock data.

Cart does not reserve stock.

Cart does not create orders.

Cart does not process payments.

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

Ordering releases Inventory reservations when PendingPayment orders are cancelled.

Ordering keeps Inventory reservations allocated when orders are marked as Paid.

Ordering commits Inventory reservations when Paid orders are marked as Shipped.

Payment does not store order details.

Payment references orders by `order_id`.

Payment calls Ordering when a pending payment succeeds.

Payment does not write OrderingDb directly.

Payment does not write InventoryDb directly.

Fulfillment does not store order details.

Fulfillment references orders by `order_id`.

Fulfillment calls Ordering when a shipment is shipped.

Fulfillment validates linked orders through Ordering before shipment creation.

Fulfillment does not write OrderingDb directly.

Fulfillment does not write InventoryDb directly.

Fulfillment does not call Inventory directly.

Fulfillment does not commit Inventory reservations directly.

Inventory still allocates stock reservations during order creation, not during cart changes.

Ordering still loads trusted product snapshots from Catalog when creating an order.
