FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/catalog/Catalog.Api/Catalog.Api.csproj services/catalog/Catalog.Api/
COPY services/catalog/Catalog.Application/Catalog.Application.csproj services/catalog/Catalog.Application/
COPY services/catalog/Catalog.Domain/Catalog.Domain.csproj services/catalog/Catalog.Domain/
COPY services/catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj services/catalog/Catalog.Infrastructure/

RUN dotnet restore services/catalog/Catalog.Api/Catalog.Api.csproj

COPY services/catalog/ services/catalog/

RUN dotnet publish services/catalog/Catalog.Api/Catalog.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
