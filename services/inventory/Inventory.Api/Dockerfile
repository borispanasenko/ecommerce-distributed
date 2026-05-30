FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/inventory/Inventory.Api/Inventory.Api.csproj services/inventory/Inventory.Api/
COPY services/inventory/Inventory.Application/Inventory.Application.csproj services/inventory/Inventory.Application/
COPY services/inventory/Inventory.Domain/Inventory.Domain.csproj services/inventory/Inventory.Domain/
COPY services/inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj services/inventory/Inventory.Infrastructure/

RUN dotnet restore services/inventory/Inventory.Api/Inventory.Api.csproj

COPY services/inventory/ services/inventory/

RUN dotnet publish services/inventory/Inventory.Api/Inventory.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
