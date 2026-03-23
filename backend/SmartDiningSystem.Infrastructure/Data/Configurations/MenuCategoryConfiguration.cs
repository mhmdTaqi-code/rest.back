using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(category => category.Description)
            .HasMaxLength(1000);

        builder.Property(category => category.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(category => category.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(category => category.RestaurantId);

        builder.HasIndex(category => new { category.RestaurantId, category.Name })
            .IsUnique();

        builder.HasOne(category => category.Restaurant)
            .WithMany(restaurant => restaurant.MenuCategories)
            .HasForeignKey(category => category.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(category => category.MenuItems)
            .WithOne(menuItem => menuItem.MenuCategory)
            .HasForeignKey(menuItem => menuItem.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
