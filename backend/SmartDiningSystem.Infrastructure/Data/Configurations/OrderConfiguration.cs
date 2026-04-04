using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(order => order.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.RestaurantId);
        builder.HasIndex(order => order.RestaurantTableId);
        builder.HasIndex(order => order.TableSessionId);
        builder.HasIndex(order => order.Status);

        builder.HasOne(order => order.RestaurantTable)
            .WithMany(table => table.Orders)
            .HasForeignKey(order => order.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.TableSession)
            .WithMany(session => session.Orders)
            .HasForeignKey(order => order.TableSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.OrderItems)
            .WithOne(orderItem => orderItem.Order)
            .HasForeignKey(orderItem => orderItem.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
