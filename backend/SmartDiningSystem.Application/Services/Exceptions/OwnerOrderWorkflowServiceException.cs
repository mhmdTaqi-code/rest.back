namespace SmartDiningSystem.Application.Services.Exceptions;

public class OwnerOrderWorkflowServiceException : Exception
{
    public OwnerOrderWorkflowServiceException(
        string message,
        int statusCode,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public int StatusCode { get; }
    public IDictionary<string, string[]> Errors { get; }
}
