# Local Development

## Start Docker Compose stack

```bash
docker compose up -d
```

---

## Docker Compose local ports

```text
Catalog API     - http://localhost:5001
Ordering API    - http://localhost:5002
Inventory API   - http://localhost:5003
Payment API     - http://localhost:5004
Cart API        - http://localhost:5005
Fulfillment API - http://localhost:5006

Catalog DB      - localhost:5433
Ordering DB     - localhost:5434
Inventory DB    - localhost:5435
Payment DB      - localhost:5436
Cart DB         - localhost:5437
Fulfillment DB  - localhost:5438
```

Inside Docker Compose, services use internal service names:

```text
Ordering    -> Catalog:   http://catalog-api:8080
Ordering    -> Inventory: http://inventory-api:8080
Payment     -> Ordering:  http://ordering-api:8080
Fulfillment -> Ordering:  http://ordering-api:8080
```

Cart Service is currently independent from other backend services.

---

## Run services with dotnet

When running services directly with `dotnet run`, start them in this order:

```text
Catalog API
Inventory API
Cart API
Ordering API
Payment API
Fulfillment API
```

Cart API can be started independently.

Ordering API requires Catalog API for product snapshots and Inventory API for stock reservations.

Payment API requires Ordering API for successful payment flow.

Fulfillment API requires Ordering API for shipment creation validation and shipment shipping flow.

Catalog:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=catalog_db;Username=postgres;Password=postgres" \
dotnet run --project services/catalog/Catalog.Api/Catalog.Api.csproj
```

Inventory:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5435;Database=inventory_db;Username=postgres;Password=postgres" \
dotnet run --project services/inventory/Inventory.Api/Inventory.Api.csproj
```

Cart:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5437;Database=cart_db;Username=postgres;Password=postgres" \
dotnet run --project services/cart/Cart.Api/Cart.Api.csproj
```

Ordering:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=ordering_db;Username=postgres;Password=postgres" \
CatalogApi__BaseUrl="http://localhost:5001" \
InventoryApi__BaseUrl="http://localhost:5245" \
dotnet run --project services/ordering/Ordering.Api/Ordering.Api.csproj
```

Payment:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5436;Database=payment_db;Username=postgres;Password=postgres" \
OrderingApi__BaseUrl="http://localhost:5172" \
dotnet run --project services/payment/Payment.Api/Payment.Api.csproj
```

Fulfillment:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5438;Database=fulfillment_db;Username=postgres;Password=postgres" \
OrderingApi__BaseUrl="http://localhost:5172" \
dotnet run --project services/fulfillment/Fulfillment.Api/Fulfillment.Api.csproj
```

If a service is started with a different local port from `launchSettings.json`, update the corresponding cross-service base URL, such as `CatalogApi__BaseUrl`, `InventoryApi__BaseUrl` or `OrderingApi__BaseUrl`.

---

## Test commands

```bash
dotnet test services/catalog/Catalog.sln
dotnet test services/inventory/Inventory.sln
dotnet test services/cart/Cart.sln
dotnet test services/ordering/Ordering.sln
dotnet test services/payment/Payment.sln
dotnet test services/fulfillment/Fulfillment.sln
```

---

## Smoke scripts

Smoke scripts are executable local checks for multi-service flows.
They require Docker Compose services to be running and they mutate local development databases by creating orders, payments, shipments and changing inventory stock.
Use them after rebuilding services or changing cross-service lifecycle behavior.

```bash
./scripts/smoke/ordering-retry-smoke.sh
./scripts/smoke/checkout-payment-fulfillment-smoke.sh
./scripts/smoke/inventory-reservation-idempotency-smoke.sh
```

The scripts support environment variable overrides:

```bash
SKU=ARM-BLK \
PRODUCT_VARIANT_ID=9572fb9d-f059-401e-9041-7fc75f8cb414 \
./scripts/smoke/ordering-retry-smoke.sh
```

Current smoke coverage:

```text
ordering-retry-smoke.sh
  Checks retry-safe Ordering mark-paid and mark-shipped behavior.
  Verifies that Inventory stock is committed only once.

checkout-payment-fulfillment-smoke.sh
  Checks the full Checkout -> Payment -> Fulfillment flow.
  Verifies that Payment marks the order as Paid, Fulfillment ships it,
  and Inventory stock is committed on shipment.

inventory-reservation-idempotency-smoke.sh
  Checks Inventory reservation terminal-state behavior.
  Verifies release retry, commit retry and invalid opposite terminal transitions.
```

`.http` files are still useful for manual API exploration.

Smoke scripts are preferred for repeatable cross-service checks with dynamic IDs and assertions.

---

## Manual checks

```text
Use *.Api.http files for write smoke flows.
Use *.Api.readonly.http files for safe read-only checks.
```

Write `.http` files may change local development data, but flows are designed to finish in terminal states and avoid active dangling reservations.

Catalog manual checks:

```text
services/catalog/Catalog.Api/Catalog.Api.http
services/catalog/Catalog.Api/Catalog.Api.readonly.http
```

Inventory manual checks:

```text
services/inventory/Inventory.Api/Inventory.Api.http
services/inventory/Inventory.Api/Inventory.Api.readonly.http
```

Cart manual checks:

```text
services/cart/Cart.Api/Cart.Api.http
services/cart/Cart.Api/Cart.Api.readonly.http
```

Ordering manual checks:

```text
services/ordering/Ordering.Api/Ordering.Api.http
services/ordering/Ordering.Api/Ordering.Api.readonly.http
```

Payment manual checks:

```text
services/payment/Payment.Api/Payment.Api.http
services/payment/Payment.Api/Payment.Api.readonly.http
```

Fulfillment manual checks:

```text
services/fulfillment/Fulfillment.Api/Fulfillment.Api.http
services/fulfillment/Fulfillment.Api/Fulfillment.Api.readonly.http
```

---

## Current cart backend flow

```text
Client creates a cart.
Client adds productVariantId and quantity to the cart.
Client can update cart item quantity.
Client can remove cart items.
Client can clear the cart.
Cart stores productVariantId and quantity only.
Cart does not store trusted product names, prices, SKUs, currencies or stock data.
```

---

## Current checkout backend flow

```text
Client sends productVariantId and quantity.
Ordering loads product snapshot from Catalog.
Ordering asks Inventory to allocate stock reservation by SKU.
Ordering stores product snapshot and inventoryReservationId.
Payment can mark the order as Paid.
Ordering keeps Inventory reservation allocated while the order is Paid.
```

---

## Current fulfillment backend flow

```text
Client or operator creates shipment for a Paid order.
Fulfillment validates linked order through Ordering.
Fulfillment stores shipment as Pending.
Client or operator ships shipment.
Fulfillment calls Ordering to mark order as Shipped.
Ordering accepts only Paid -> Shipped transition.
Ordering commits Inventory reservation during mark-shipped.
Fulfillment marks shipment as Shipped.
```

---

## Current Inventory commit boundary

```text
Inventory reservation is allocated during order creation.
Payment success marks the order as Paid.
Inventory reservation remains allocated while the order is Paid.
Fulfillment shipping triggers Ordering mark-shipped.
Ordering commits Inventory reservation during mark-shipped.
Fulfillment does not call Inventory directly.
Ordering owns inventoryReservationId references stored on order items.
```
