using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");

        builder.HasKey(table => table.Id);

        builder.Property(table => table.TableNumber)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(table => table.TableToken)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(table => table.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(table => table.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(table => table.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(table => table.RestaurantId);

        builder.HasIndex(table => table.TableToken)
            .IsUnique();

        builder.HasIndex(table => new { table.RestaurantId, table.TableNumber })
            .IsUnique();

        builder.HasOne(table => table.Restaurant)
            .WithMany(restaurant => restaurant.Tables)
            .HasForeignKey(table => table.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(table => table.TableCarts)
            .WithOne(cart => cart.RestaurantTable)
            .HasForeignKey(cart => cart.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(table => table.Reservations)
            .WithOne(reservation => reservation.RestaurantTable)
            .HasForeignKey(reservation => reservation.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
