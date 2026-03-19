using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(user => user.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(user => user.IsPhoneVerified)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(user => user.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(user => user.UpdatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.Username)
            .IsUnique();

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique();

        builder.HasMany(user => user.OwnedRestaurants)
            .WithOne(restaurant => restaurant.Owner)
            .HasForeignKey(restaurant => restaurant.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.Orders)
            .WithOne(order => order.User)
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.LoginOtpCodes)
            .WithOne(otpCode => otpCode.UserAccount)
            .HasForeignKey(otpCode => otpCode.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
