using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(booking => booking.Id);

        builder.Property(booking => booking.ReservationTimeUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(booking => booking.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(booking => booking.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(booking => booking.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(booking => booking.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(booking => booking.CheckedInAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(booking => booking.CompletedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(booking => booking.CancelledAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(booking => booking.NoShowMarkedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.HasIndex(booking => booking.UserId);
        builder.HasIndex(booking => booking.RestaurantId);
        builder.HasIndex(booking => booking.RestaurantTableId);
        builder.HasIndex(booking => booking.Status);
        builder.HasIndex(booking => booking.ReservationTimeUtc);
        builder.HasIndex(booking => new { booking.RestaurantTableId, booking.Status, booking.ReservationTimeUtc });

        builder.HasOne(booking => booking.User)
            .WithMany(user => user.Bookings)
            .HasForeignKey(booking => booking.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.Restaurant)
            .WithMany(restaurant => restaurant.Bookings)
            .HasForeignKey(booking => booking.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.RestaurantTable)
            .WithMany(table => table.Bookings)
            .HasForeignKey(booking => booking.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(booking => booking.Items)
            .WithOne(item => item.Booking)
            .HasForeignKey(item => item.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(booking => booking.TableSessions)
            .WithOne(session => session.Booking)
            .HasForeignKey(session => session.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
