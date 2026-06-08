using Fulfillment.Domain.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(shipment => shipment.Id);

        builder.Property(shipment => shipment.Id)
            .HasColumnName("id");

        builder.Property(shipment => shipment.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(shipment => shipment.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(shipment => shipment.Carrier)
            .HasColumnName("carrier")
            .HasMaxLength(100);

        builder.Property(shipment => shipment.TrackingNumber)
            .HasColumnName("tracking_number")
            .HasMaxLength(200);

        builder.Property(shipment => shipment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(shipment => shipment.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(shipment => shipment.ShippedAt)
            .HasColumnName("shipped_at");

        builder.Property(shipment => shipment.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.HasIndex(shipment => shipment.OrderId);
        builder.HasIndex(shipment => shipment.Status);
        builder.HasIndex(shipment => shipment.CreatedAt);
    }
}
