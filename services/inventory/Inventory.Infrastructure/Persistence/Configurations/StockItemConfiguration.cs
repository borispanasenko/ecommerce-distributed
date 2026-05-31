using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items", table =>
        {
            table.HasCheckConstraint(
                "ck_stock_items_on_hand_quantity_non_negative",
                "on_hand_quantity >= 0");

            table.HasCheckConstraint(
                "ck_stock_items_reserved_quantity_non_negative",
                "reserved_quantity >= 0");

            table.HasCheckConstraint(
                "ck_stock_items_reserved_not_greater_than_on_hand",
                "reserved_quantity <= on_hand_quantity");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.WarehouseId)
            .HasColumnName("warehouse_id")
            .IsRequired();

        builder.Property(x => x.LocationId)
            .HasColumnName("location_id")
            .IsRequired();

        builder.Property(x => x.OnHandQuantity)
            .HasColumnName("on_hand_quantity")
            .IsRequired();

        builder.Property(x => x.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired();

        builder.Ignore(x => x.AvailableQuantity);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.Warehouse)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.StockItems)
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Sku);

        builder.HasIndex(x => new { x.Sku, x.WarehouseId, x.LocationId })
            .IsUnique();
    }
}
