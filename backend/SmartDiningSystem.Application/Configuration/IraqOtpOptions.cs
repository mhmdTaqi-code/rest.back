namespace SmartDiningSystem.Application.Configuration;

public class IraqOtpOptions
{
    public const string SectionName = "IraqOtp";

    public string BaseUrl { get; set; } = "https://api.otpiq.com";
    public string SendSmsEndpoint { get; set; } = "/api/sms";
    public string ApiKey { get; set; } = "PLACEHOLDER_ONLY";
}
