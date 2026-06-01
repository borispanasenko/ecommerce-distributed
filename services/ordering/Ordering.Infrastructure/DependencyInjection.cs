using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;
using Ordering.Application.Orders;
using Ordering.Infrastructure.Orders;

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

        return services;
    }

    public static async Task MigrateOrderingDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
