namespace SmartDiningSystem.Application.Services.Exceptions;

public class BookingFlowServiceException : Exception
{
    public BookingFlowServiceException(
        string message,
        int statusCode,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IDictionary<string, string[]>? Errors { get; }
}
