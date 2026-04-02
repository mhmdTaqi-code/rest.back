using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class TableSessionConfiguration : IEntityTypeConfiguration<TableSession>
{
    public void Configure(EntityTypeBuilder<TableSession> builder)
    {
        builder.ToTable("TableSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(session => session.OpenedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(session => session.ClosedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.HasIndex(session => session.RestaurantId);
        builder.HasIndex(session => session.RestaurantTableId);
        builder.HasIndex(session => session.BookingId);
        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.Status);

        builder.HasOne(session => session.Restaurant)
            .WithMany(restaurant => restaurant.TableSessions)
            .HasForeignKey(session => session.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.RestaurantTable)
            .WithMany(table => table.TableSessions)
            .HasForeignKey(session => session.RestaurantTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.User)
            .WithMany(user => user.TableSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(session => session.Orders)
            .WithOne(order => order.TableSession)
            .HasForeignKey(order => order.TableSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
