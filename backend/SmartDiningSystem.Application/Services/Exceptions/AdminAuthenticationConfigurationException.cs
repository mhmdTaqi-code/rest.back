namespace SmartDiningSystem.Application.Services.Exceptions;

public class AdminAuthenticationConfigurationException : Exception
{
    public AdminAuthenticationConfigurationException(string message)
        : base(message)
    {
    }
}
