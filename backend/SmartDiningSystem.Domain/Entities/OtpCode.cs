using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Domain.Entities;

public class OtpCode
{
    public Guid Id { get; set; }
    public Guid? UserAccountId { get; set; }
    public Guid? PendingRegistrationId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public UserAccount? UserAccount { get; set; }
    public PendingRegistration? PendingRegistration { get; set; }
}
