using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class PublicTableMenuService : IPublicTableMenuService
{
    private readonly AppDbContext _dbContext;
    private readonly IRestaurantTableAccessService _restaurantTableAccessService;

    public PublicTableMenuService(
        AppDbContext dbContext,
        IRestaurantTableAccessService restaurantTableAccessService)
    {
        _dbContext = dbContext;
        _restaurantTableAccessService = restaurantTableAccessService;
    }

    public async Task<PublicTableMenuResponseDto> GetPublicMenuAsync(
        Guid restaurantId,
        Guid tableId,
        CancellationToken cancellationToken)
    {
        var table = await _restaurantTableAccessService.ResolveTableAsync(restaurantId, tableId, cancellationToken);

        var categories = await _dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => category.RestaurantId == table.RestaurantId && category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new PublicTableMenuCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                DisplayOrder = category.DisplayOrder,
                Items = category.MenuItems
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.Name)
                    .Select(item => new PublicTableMenuItemDto
                    {
                        Id = item.Id,
                        MenuCategoryId = item.MenuCategoryId!.Value,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        ImageUrl = item.ImageUrl,
                        DisplayOrder = item.DisplayOrder,
                        IsAvailable = item.IsAvailable
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new PublicTableMenuResponseDto
        {
            Table = table,
            Categories = categories
        };
    }
}
