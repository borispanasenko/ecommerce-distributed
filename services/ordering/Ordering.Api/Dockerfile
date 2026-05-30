FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/ordering/Ordering.Api/Ordering.Api.csproj services/ordering/Ordering.Api/
COPY services/ordering/Ordering.Application/Ordering.Application.csproj services/ordering/Ordering.Application/
COPY services/ordering/Ordering.Domain/Ordering.Domain.csproj services/ordering/Ordering.Domain/
COPY services/ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj services/ordering/Ordering.Infrastructure/

RUN dotnet restore services/ordering/Ordering.Api/Ordering.Api.csproj

COPY services/ordering/ services/ordering/

RUN dotnet publish services/ordering/Ordering.Api/Ordering.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Ordering.Api.dll"]
