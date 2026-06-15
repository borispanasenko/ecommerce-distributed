using Fulfillment.Application.Ordering;
using Fulfillment.Application.Shipments;
using Fulfillment.Infrastructure.Ordering;
using Fulfillment.Infrastructure.Persistence;
using Fulfillment.Infrastructure.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fulfillment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFulfillmentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FulfillmentDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpClient<IOrderingClient, HttpOrderingClient>(client =>
        {
            var baseUrl = configuration["OrderingApi:BaseUrl"]
                ?? throw new InvalidOperationException("OrderingApi:BaseUrl is not configured.");

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddScoped<IFulfillmentService, EfFulfillmentService>();

        return services;
    }

    public static async Task MigrateFulfillmentDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
