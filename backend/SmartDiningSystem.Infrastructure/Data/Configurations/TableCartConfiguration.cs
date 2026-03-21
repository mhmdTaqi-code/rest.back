using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class TableCartConfiguration : IEntityTypeConfiguration<TableCart>
{
    public void Configure(EntityTypeBuilder<TableCart> builder)
    {
        builder.ToTable("TableCarts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(cart => cart.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(cart => cart.UserId);
        builder.HasIndex(cart => cart.RestaurantId);
        builder.HasIndex(cart => cart.RestaurantTableId);

        builder.HasIndex(cart => new { cart.UserId, cart.RestaurantTableId })
            .IsUnique();

        builder.HasOne(cart => cart.User)
            .WithMany(user => user.TableCarts)
            .HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cart => cart.Restaurant)
            .WithMany(restaurant => restaurant.TableCarts)
            .HasForeignKey(cart => cart.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cart => cart.RestaurantTable)
            .WithMany(table => table.TableCarts)
            .HasForeignKey(cart => cart.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(cart => cart.Items)
            .WithOne(item => item.TableCart)
            .HasForeignKey(item => item.TableCartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
