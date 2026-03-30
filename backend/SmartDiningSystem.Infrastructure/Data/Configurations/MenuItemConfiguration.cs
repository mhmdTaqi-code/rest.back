using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_MenuItems_Price_Positive",
                "\"Price\" >= 0");
        });

        builder.HasKey(menuItem => menuItem.Id);

        builder.Property(menuItem => menuItem.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(menuItem => menuItem.Description)
            .HasMaxLength(1000);

        builder.Property(menuItem => menuItem.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(menuItem => menuItem.ImageUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(menuItem => menuItem.IsHighlighted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(menuItem => menuItem.HighlightTag)
            .HasMaxLength(50);

        builder.Property(menuItem => menuItem.IsAvailable)
            .IsRequired();

        builder.Property(menuItem => menuItem.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(menuItem => menuItem.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(menuItem => menuItem.RestaurantId);
        builder.HasIndex(menuItem => menuItem.MenuCategoryId);

        builder.HasOne(menuItem => menuItem.MenuCategory)
            .WithMany(category => category.MenuItems)
            .HasForeignKey(menuItem => menuItem.MenuCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(menuItem => menuItem.TableCartItems)
            .WithOne(item => item.MenuItem)
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
