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
        });

        builder.HasKey(restaurant => restaurant.Id);

        builder.Property(restaurant => restaurant.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(restaurant => restaurant.Description)
            .HasMaxLength(1000);

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

        builder.HasMany(restaurant => restaurant.MenuItems)
            .WithOne(menuItem => menuItem.Restaurant)
            .HasForeignKey(menuItem => menuItem.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Tables)
            .WithOne(table => table.Restaurant)
            .HasForeignKey(table => table.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(restaurant => restaurant.Orders)
            .WithOne(order => order.Restaurant)
            .HasForeignKey(order => order.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
