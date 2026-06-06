FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/cart/Cart.Domain/Cart.Domain.csproj services/cart/Cart.Domain/
COPY services/cart/Cart.Application/Cart.Application.csproj services/cart/Cart.Application/
COPY services/cart/Cart.Infrastructure/Cart.Infrastructure.csproj services/cart/Cart.Infrastructure/
COPY services/cart/Cart.Api/Cart.Api.csproj services/cart/Cart.Api/

RUN dotnet restore services/cart/Cart.Api/Cart.Api.csproj

COPY services/cart services/cart

RUN dotnet publish services/cart/Cart.Api/Cart.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Cart.Api.dll"]
