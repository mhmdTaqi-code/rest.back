using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class BookingItemConfiguration : IEntityTypeConfiguration<BookingItem>
{
    public void Configure(EntityTypeBuilder<BookingItem> builder)
    {
        builder.ToTable("BookingItems", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_BookingItems_Quantity_Positive", "\"Quantity\" > 0");
            tableBuilder.HasCheckConstraint("CK_BookingItems_UnitPrice_Positive", "\"UnitPrice\" > 0");
            tableBuilder.HasCheckConstraint("CK_BookingItems_LineTotal_Positive", "\"LineTotal\" > 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(item => item.BookingId);
        builder.HasIndex(item => item.MenuItemId);

        builder.HasOne(item => item.MenuItem)
            .WithMany()
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
