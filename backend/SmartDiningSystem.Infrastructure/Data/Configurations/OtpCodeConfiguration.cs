using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Data.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("OtpCodes", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_OtpCodes_Code_NotEmpty",
                "btrim(\"Code\") <> ''");

            tableBuilder.HasCheckConstraint(
                "CK_OtpCodes_Association_Required",
                "(\"UserAccountId\" IS NOT NULL AND \"PendingRegistrationId\" IS NULL) OR (\"UserAccountId\" IS NULL AND \"PendingRegistrationId\" IS NOT NULL)");
        });

        builder.HasKey(otpCode => otpCode.Id);

        builder.Property(otpCode => otpCode.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(otpCode => otpCode.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(otpCode => otpCode.Purpose)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(otpCode => otpCode.CreatedAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(otpCode => otpCode.ExpiresAtUtc)
            .IsRequired()
            .HasConversion<UtcDateTimeConverter>();

        builder.Property(otpCode => otpCode.UsedAtUtc)
            .HasConversion<NullableUtcDateTimeConverter>();

        builder.Property(otpCode => otpCode.IsUsed)
            .IsRequired();

        builder.HasIndex(otpCode => otpCode.UserAccountId);
        builder.HasIndex(otpCode => otpCode.PendingRegistrationId);
        builder.HasIndex(otpCode => otpCode.PhoneNumber);
        builder.HasIndex(otpCode => new { otpCode.PhoneNumber, otpCode.Code, otpCode.IsUsed });
        builder.HasIndex(otpCode => otpCode.ExpiresAtUtc);

        builder.HasOne(otpCode => otpCode.UserAccount)
            .WithMany(userAccount => userAccount.LoginOtpCodes)
            .HasForeignKey(otpCode => otpCode.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
