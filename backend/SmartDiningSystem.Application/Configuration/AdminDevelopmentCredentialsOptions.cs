namespace SmartDiningSystem.Application.Configuration;

public class AdminDevelopmentCredentialsOptions
{
    public const string SectionName = "AdminDevelopmentCredentials";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
