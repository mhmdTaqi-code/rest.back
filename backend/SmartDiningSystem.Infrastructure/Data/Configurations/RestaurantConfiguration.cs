using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurants", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_RejectionReason_Required_WhenRejected",
                "\"ApprovalStatus\" <> 'Rejected' OR (\"RejectionReason\" IS NOT NULL AND btrim(\"RejectionReason\") <> '')");

            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_ApprovedAtUtc_OnlyWhenApproved",
                "\"ApprovalStatus\" = 'Approved' OR \"ApprovedAtUtc\" IS NULL");

            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_RejectedAtUtc_OnlyWhenRejected",
                "\"ApprovalStatus\" = 'Rejected' OR \"RejectedAtUtc\" IS NULL");

            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_Latitude_Range",
                "\"Latitude\" IS NULL OR (\"Latitude\" >= -90 AND \"Latitude\" <= 90)");

            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_Longitude_Range",
                "\"Longitude\" IS NULL OR (\"Longitude\" >= -180 AND \"Longitude\" <= 180)");

            tableBuilder.HasCheckConstraint(
                "CK_Restaurants_Coordinates_Paired",
                "(\"Latitude\" IS NULL AND \"Longitude\" IS NULL) OR (\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL)");
        });

        builder.HasKey(restaurant => restaurant.Id);

        builder.Property(restaurant => restaurant.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(restaurant => restaurant.Description)
            .HasMaxLength(1000);

        builder.Property(restaurant => restaurant.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(restaurant => restaurant.Latitude)
            .HasColumnType("double precision");

        builder.Property(restaurant => restaurant.Longitude)
            .HasColumnType("double precision");

        builder.Property(restaurant => restaurant.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(restaurant => restaurant.ContactPhone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(restaurant => restaurant.ApprovalStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(restaurant => restaurant.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(restaurant => restaurant.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(restaurant => restaurant.ApprovedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(restaurant => restaurant.RejectedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.HasIndex(restaurant => restaurant.OwnerId);
        builder.HasIndex(restaurant => restaurant.ApprovalStatus);

        builder.HasMany(restaurant => restaurant.MenuCategories)
            .WithOne(category => category.Restaurant)
            .HasForeignKey(category => category.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.MenuItems)
            .WithOne(menuItem => menuItem.Restaurant)
            .HasForeignKey(menuItem => menuItem.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Tables)
            .WithOne(table => table.Restaurant)
            .HasForeignKey(table => table.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.TableCarts)
            .WithOne(cart => cart.Restaurant)
            .HasForeignKey(cart => cart.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Orders)
            .WithOne(order => order.Restaurant)
            .HasForeignKey(order => order.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Ratings)
            .WithOne(rating => rating.Restaurant)
            .HasForeignKey(rating => rating.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Bookings)
            .WithOne(booking => booking.Restaurant)
            .HasForeignKey(booking => booking.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.TableSessions)
            .WithOne(session => session.Restaurant)
            .HasForeignKey(session => session.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
