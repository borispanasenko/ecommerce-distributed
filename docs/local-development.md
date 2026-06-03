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

Catalog DB    - localhost:5433
Ordering DB   - localhost:5434
Inventory DB  - localhost:5435
Payment DB    - localhost:5436
```

Inside Docker Compose, services use internal service names:

```text
Ordering -> Inventory: http://inventory-api:8080
Payment  -> Ordering:  http://ordering-api:8080
```

---

## Run services with dotnet

When running services directly with `dotnet run`, start them in this order:

```text
Inventory API
Ordering API
Payment API
```

Catalog can be started independently.

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

Ordering:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=ordering_db;Username=postgres;Password=postgres" \
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

---

## Test commands

```bash
dotnet test services/catalog/Catalog.sln
dotnet test services/inventory/Inventory.sln
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
