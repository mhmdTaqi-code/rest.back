namespace SmartDiningSystem.Application.DTOs.Common;

public class ApiErrorResponseDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    public string? TraceId { get; set; }
}
