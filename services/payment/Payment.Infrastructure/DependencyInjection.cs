using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;
using Payment.Application.Payments;
using Payment.Infrastructure.Payments;
using Payment.Application.Ordering;
using Payment.Infrastructure.Ordering;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");
        }

        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPaymentService, EfPaymentService>();

        var orderingApiBaseUrl = configuration["OrderingApi:BaseUrl"];

        if (string.IsNullOrWhiteSpace(orderingApiBaseUrl))
        {
            throw new InvalidOperationException(
                "Ordering API base URL 'OrderingApi:BaseUrl' is not configured.");
        }

        services.AddHttpClient<IOrderingClient, HttpOrderingClient>(client =>
        {
            client.BaseAddress = new Uri(orderingApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    public static async Task MigratePaymentDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
