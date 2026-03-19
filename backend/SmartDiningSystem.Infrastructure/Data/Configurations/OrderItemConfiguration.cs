using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_OrderItems_Quantity_Positive",
                "\"Quantity\" > 0");

            tableBuilder.HasCheckConstraint(
                "CK_OrderItems_UnitPrice_Positive",
                "\"UnitPrice\" > 0");
        });

        builder.HasKey(orderItem => orderItem.Id);

        builder.Property(orderItem => orderItem.Quantity)
            .IsRequired();

        builder.Property(orderItem => orderItem.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(orderItem => orderItem.OrderId);
        builder.HasIndex(orderItem => orderItem.MenuItemId);

        builder.HasOne(orderItem => orderItem.MenuItem)
            .WithMany(menuItem => menuItem.OrderItems)
            .HasForeignKey(orderItem => orderItem.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
