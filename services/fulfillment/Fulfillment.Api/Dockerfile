FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/fulfillment/Fulfillment.Domain/Fulfillment.Domain.csproj services/fulfillment/Fulfillment.Domain/
COPY services/fulfillment/Fulfillment.Application/Fulfillment.Application.csproj services/fulfillment/Fulfillment.Application/
COPY services/fulfillment/Fulfillment.Infrastructure/Fulfillment.Infrastructure.csproj services/fulfillment/Fulfillment.Infrastructure/
COPY services/fulfillment/Fulfillment.Api/Fulfillment.Api.csproj services/fulfillment/Fulfillment.Api/

RUN dotnet restore services/fulfillment/Fulfillment.Api/Fulfillment.Api.csproj

COPY services/fulfillment services/fulfillment

RUN dotnet publish services/fulfillment/Fulfillment.Api/Fulfillment.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Fulfillment.Api.dll"]
