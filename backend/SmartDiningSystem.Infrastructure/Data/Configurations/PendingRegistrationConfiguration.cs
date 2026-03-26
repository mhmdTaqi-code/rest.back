using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("PendingRegistrations");

        builder.HasKey(pendingRegistration => pendingRegistration.Id);

        builder.Property(pendingRegistration => pendingRegistration.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pendingRegistration => pendingRegistration.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(pendingRegistration => pendingRegistration.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pendingRegistration => pendingRegistration.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(pendingRegistration => pendingRegistration.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(pendingRegistration => pendingRegistration.RestaurantName)
            .HasMaxLength(200);

        builder.Property(pendingRegistration => pendingRegistration.RestaurantDescription)
            .HasMaxLength(1000);

        builder.Property(pendingRegistration => pendingRegistration.RestaurantAddress)
            .HasMaxLength(500);

        builder.Property(pendingRegistration => pendingRegistration.RestaurantPhoneNumber)
            .HasMaxLength(20);

        builder.Property(pendingRegistration => pendingRegistration.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(pendingRegistration => pendingRegistration.PhoneNumber)
            .IsUnique();

        builder.HasIndex(pendingRegistration => pendingRegistration.Username)
            .IsUnique();

        builder.HasMany(pendingRegistration => pendingRegistration.OtpCodes)
            .WithOne(otpCode => otpCode.PendingRegistration)
            .HasForeignKey(otpCode => otpCode.PendingRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
