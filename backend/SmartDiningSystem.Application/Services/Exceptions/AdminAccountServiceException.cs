namespace SmartDiningSystem.Application.Services.Exceptions;

public class AdminAccountServiceException : Exception
{
    public AdminAccountServiceException(
        string message,
        bool isNotFound = false,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        IsNotFound = isNotFound;
        Errors = errors;
    }

    public bool IsNotFound { get; }

    public IDictionary<string, string[]>? Errors { get; }
}
