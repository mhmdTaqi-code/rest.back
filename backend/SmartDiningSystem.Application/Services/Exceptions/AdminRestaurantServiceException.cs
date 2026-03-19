using Microsoft.AspNetCore.Http;

namespace SmartDiningSystem.Application.Services.Exceptions;

public class AdminRestaurantServiceException : Exception
{
    public AdminRestaurantServiceException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public int StatusCode { get; }
    public IDictionary<string, string[]> Errors { get; }
}
