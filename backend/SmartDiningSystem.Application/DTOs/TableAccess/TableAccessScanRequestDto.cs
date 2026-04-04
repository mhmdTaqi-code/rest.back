using SmartDiningSystem.Application.DTOs.TableOrdering;

namespace SmartDiningSystem.Application.DTOs.TableAccess;

public class TableAccessScanRequestDto
{
    public Guid TableId { get; set; }
    public IReadOnlyList<SubmitTableOrderItemRequestDto>? Items { get; set; }
}
