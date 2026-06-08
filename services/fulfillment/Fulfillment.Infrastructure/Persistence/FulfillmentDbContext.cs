using Fulfillment.Domain.Shipments;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Infrastructure.Persistence;

public sealed class FulfillmentDbContext : DbContext
{
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FulfillmentDbContext).Assembly);
    }
}
