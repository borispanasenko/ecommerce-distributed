FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/payment/Payment.Api/Payment.Api.csproj services/payment/Payment.Api/
COPY services/payment/Payment.Application/Payment.Application.csproj services/payment/Payment.Application/
COPY services/payment/Payment.Domain/Payment.Domain.csproj services/payment/Payment.Domain/
COPY services/payment/Payment.Infrastructure/Payment.Infrastructure.csproj services/payment/Payment.Infrastructure/

RUN dotnet restore services/payment/Payment.Api/Payment.Api.csproj

COPY services/payment/ services/payment/

RUN dotnet publish services/payment/Payment.Api/Payment.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Payment.Api.dll"]
