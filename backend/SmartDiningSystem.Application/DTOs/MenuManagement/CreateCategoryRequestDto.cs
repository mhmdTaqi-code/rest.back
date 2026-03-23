namespace SmartDiningSystem.Application.DTOs.MenuManagement;

public class CreateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}
