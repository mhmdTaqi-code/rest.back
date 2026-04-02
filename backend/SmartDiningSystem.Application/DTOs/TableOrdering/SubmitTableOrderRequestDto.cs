namespace SmartDiningSystem.Application.DTOs.TableOrdering;

public class SubmitTableOrderRequestDto
{
    public IReadOnlyList<SubmitTableOrderItemRequestDto> Items { get; set; } = [];
}
