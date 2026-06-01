using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", table =>
        {
            table.HasCheckConstraint(
                "ck_order_items_unit_price_amount_minor_non_negative",
                "unit_price_amount_minor >= 0");

            table.HasCheckConstraint(
                "ck_order_items_line_total_amount_minor_non_negative",
                "line_total_amount_minor >= 0");

            table.HasCheckConstraint(
                "ck_order_items_quantity_positive",
                "quantity > 0");

            table.HasCheckConstraint(
                "ck_order_items_currency_length",
                "char_length(currency) = 3");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired();

        builder.Property(x => x.Sku)
            .HasColumnName("sku")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.VariantName)
            .HasColumnName("variant_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.UnitPriceAmountMinor)
            .HasColumnName("unit_price_amount_minor")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(x => x.LineTotalAmountMinor)
            .HasColumnName("line_total_amount_minor")
            .IsRequired();

        builder.Property(x => x.InventoryReservationId)
            .HasColumnName("inventory_reservation_id");

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.Sku);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ProductVariantId);
        builder.HasIndex(x => x.InventoryReservationId);
    }
}
