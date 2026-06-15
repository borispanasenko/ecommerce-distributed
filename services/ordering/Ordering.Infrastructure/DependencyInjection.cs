using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;
using Ordering.Application.Orders;
using Ordering.Infrastructure.Orders;
using Ordering.Application.Catalog;
using Ordering.Infrastructure.Catalog;
using Ordering.Application.Inventory;
using Ordering.Infrastructure.Inventory;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<OrderingDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IOrderingService, EfOrderingService>();

        var inventoryApiBaseUrl = configuration["InventoryApi:BaseUrl"];

        if (string.IsNullOrWhiteSpace(inventoryApiBaseUrl))
        {
            throw new InvalidOperationException(
                "Inventory API base URL 'InventoryApi:BaseUrl' is not configured.");
        }

        services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
        {
            var baseUrl = configuration["CatalogApi:BaseUrl"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("CatalogApi:BaseUrl is not configured.");
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHttpClient<IInventoryClient, HttpInventoryClient>(client =>
        {
            client.BaseAddress = new Uri(inventoryApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    public static async Task MigrateOrderingDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
