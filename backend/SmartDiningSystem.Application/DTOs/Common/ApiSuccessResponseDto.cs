namespace SmartDiningSystem.Application.DTOs.Common;

public class ApiSuccessResponseDto<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
