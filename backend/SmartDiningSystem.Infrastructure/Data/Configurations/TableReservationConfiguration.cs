using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class TableReservationConfiguration : IEntityTypeConfiguration<TableReservation>
{
    public void Configure(EntityTypeBuilder<TableReservation> builder)
    {
        builder.ToTable("TableReservations", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_TableReservations_GuestCount_Positive",
                "\"GuestCount\" > 0");

            tableBuilder.HasCheckConstraint(
                "CK_TableReservations_DepositAmount_Minimum",
                "\"DepositAmount\" >= 5000");

            tableBuilder.HasCheckConstraint(
                "CK_TableReservations_EndAfterStart",
                "\"ReservationEndUtc\" > \"ReservationStartUtc\"");
        });

        builder.HasKey(reservation => reservation.Id);

        builder.Property(reservation => reservation.DepositAmount)
            .HasPrecision(18, 2);

        builder.Property(reservation => reservation.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(reservation => reservation.CancellationReason)
            .HasMaxLength(500);

        builder.Property(reservation => reservation.ReservationStartUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(reservation => reservation.ReservationEndUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(reservation => reservation.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(reservation => reservation.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(reservation => reservation.DepositPaidAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.ConfirmedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.CheckedInAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.CancelledAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.GracePeriodEndsAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.NoShowMarkedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(reservation => reservation.DepositForfeitedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.HasIndex(reservation => reservation.UserId);
        builder.HasIndex(reservation => reservation.RestaurantId);
        builder.HasIndex(reservation => reservation.RestaurantTableId);
        builder.HasIndex(reservation => reservation.Status);
        builder.HasIndex(reservation => reservation.ReservationStartUtc);
        builder.HasIndex(reservation => reservation.ReservationEndUtc);
        builder.HasIndex(reservation => new
        {
            reservation.RestaurantTableId,
            reservation.Status,
            reservation.ReservationStartUtc,
            reservation.ReservationEndUtc
        });

        builder.HasOne(reservation => reservation.User)
            .WithMany(user => user.TableReservations)
            .HasForeignKey(reservation => reservation.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.Restaurant)
            .WithMany(restaurant => restaurant.Reservations)
            .HasForeignKey(reservation => reservation.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(reservation => reservation.RestaurantTable)
            .WithMany(table => table.Reservations)
            .HasForeignKey(reservation => reservation.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
