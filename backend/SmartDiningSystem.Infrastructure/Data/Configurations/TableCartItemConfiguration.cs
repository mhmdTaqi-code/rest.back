using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class TableCartItemConfiguration : IEntityTypeConfiguration<TableCartItem>
{
    public void Configure(EntityTypeBuilder<TableCartItem> builder)
    {
        builder.ToTable("TableCartItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_TableCartItems_Quantity_Positive",
                "\"Quantity\" > 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.HasIndex(item => item.TableCartId);
        builder.HasIndex(item => item.MenuItemId);

        builder.HasIndex(item => new { item.TableCartId, item.MenuItemId })
            .IsUnique();

        builder.HasOne(item => item.MenuItem)
            .WithMany(menuItem => menuItem.TableCartItems)
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
