namespace SmartDiningSystem.Application.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SmartDiningSystem";
    public string Audience { get; set; } = "SmartDiningSystem.Client";
    public string SecretKey { get; set; } = "CHANGE_ME_WITH_A_LONG_RANDOM_SECRET";
    public int AccessTokenMinutes { get; set; } = 60;
}
