using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class RestaurantRatingConfiguration : IEntityTypeConfiguration<RestaurantRating>
{
    public void Configure(EntityTypeBuilder<RestaurantRating> builder)
    {
        builder.ToTable("RestaurantRatings", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_RestaurantRatings_Stars_Range",
                "\"Stars\" >= 1 AND \"Stars\" <= 5");
        });

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.Stars)
            .IsRequired();

        builder.Property(rating => rating.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(rating => rating.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(rating => rating.RestaurantId);
        builder.HasIndex(rating => rating.UserId);
        builder.HasIndex(rating => new { rating.RestaurantId, rating.UserId })
            .IsUnique();

        builder.HasOne(rating => rating.Restaurant)
            .WithMany(restaurant => restaurant.Ratings)
            .HasForeignKey(rating => rating.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rating => rating.User)
            .WithMany(user => user.RestaurantRatings)
            .HasForeignKey(rating => rating.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
