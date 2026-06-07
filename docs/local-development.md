# Local Development

## Start Docker Compose stack

```bash
docker compose up -d
```

---

## Docker Compose local ports

```text
Catalog API   - http://localhost:5001
Ordering API  - http://localhost:5002
Inventory API - http://localhost:5003
Payment API   - http://localhost:5004
Cart API      - http://localhost:5005

Catalog DB    - localhost:5433
Ordering DB   - localhost:5434
Inventory DB  - localhost:5435
Payment DB    - localhost:5436
Cart DB       - localhost:5437
```

Inside Docker Compose, services use internal service names:

```text
Ordering -> Catalog:   http://catalog-api:8080
Ordering -> Inventory: http://inventory-api:8080
Payment  -> Ordering:  http://ordering-api:8080
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
```

Cart API can be started independently.

Ordering API requires Catalog API for product snapshots and Inventory API for stock reservations.

Payment API requires Ordering API for successful payment flow.

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

If a service is started with a different local port from `launchSettings.json`, update the corresponding `CatalogApi__BaseUrl`, `InventoryApi__BaseUrl` or `OrderingApi__BaseUrl`.

---

## Test commands

```bash
dotnet test services/catalog/Catalog.sln
dotnet test services/inventory/Inventory.sln
dotnet test services/cart/Cart.sln
dotnet test services/ordering/Ordering.sln
dotnet test services/payment/Payment.sln
```

---

## Manual checks

```text
Use *.Api.http files for write smoke flows.
Use *.Api.readonly.http files for safe read-only checks.
```

Write `.http` files may change local development data, but flows are designed to finish in terminal states and avoid active dangling reservations.

Cart manual checks:

```text
services/cart/Cart.Api/Cart.Api.http
services/cart/Cart.Api/Cart.Api.readonly.http
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
Ordering commits Inventory reservation when order is marked as Paid.
```
