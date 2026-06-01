using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", table =>
        {
            table.HasCheckConstraint(
                "ck_orders_total_amount_minor_non_negative",
                "total_amount_minor >= 0");

            table.HasCheckConstraint(
                "ck_orders_currency_length",
                "char_length(currency) = 3");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(x => x.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.CustomerEmail)
            .HasColumnName("customer_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TotalAmountMinor)
            .HasColumnName("total_amount_minor")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CustomerEmail);
        builder.HasIndex(x => x.CreatedAt);
    }
}
