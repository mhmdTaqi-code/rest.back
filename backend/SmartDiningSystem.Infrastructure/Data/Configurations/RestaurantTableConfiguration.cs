using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        builder.ToTable("RestaurantTables");

        builder.HasKey(table => table.Id);

        builder.Property(table => table.TableCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(table => table.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(table => table.IsActive)
            .IsRequired();

        builder.HasIndex(table => table.RestaurantId);

        builder.HasIndex(table => new { table.RestaurantId, table.TableCode })
            .IsUnique();
    }
}
