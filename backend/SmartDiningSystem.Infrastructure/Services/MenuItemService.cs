using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.MenuManagement;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class MenuItemService : IMenuItemService
{
    private readonly AppDbContext _dbContext;

    public MenuItemService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetOwnerMenuItemsAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        return await _dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.RestaurantId == restaurant.Id)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Select(MapMenuItem())
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(
        Guid ownerId,
        Guid restaurantId,
        CreateMenuItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var category = await GetOwnedCategoryAsync(restaurant.Id, request.MenuCategoryId, cancellationToken);

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            MenuCategoryId = category.Id,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            Price = request.Price,
            ImageUrl = request.ImageUrl.Trim(),
            IsAvailable = request.IsAvailable,
            DisplayOrder = request.DisplayOrder ?? 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MenuItems.Add(menuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMenuItemDtoAsync(menuItem.Id, restaurant.Id, cancellationToken);
    }

    public async Task<MenuItemDto> UpdateMenuItemAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid menuItemId,
        UpdateMenuItemRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var category = await GetOwnedCategoryAsync(restaurant.Id, request.MenuCategoryId, cancellationToken);
        var menuItem = await GetOwnedMenuItemEntityAsync(restaurant.Id, menuItemId, cancellationToken);

        menuItem.MenuCategoryId = category.Id;
        menuItem.Name = request.Name.Trim();
        menuItem.Description = NormalizeOptionalText(request.Description);
        menuItem.Price = request.Price;
        menuItem.ImageUrl = request.ImageUrl.Trim();
        menuItem.IsAvailable = request.IsAvailable;
        menuItem.DisplayOrder = request.DisplayOrder ?? 0;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMenuItemDtoAsync(menuItem.Id, restaurant.Id, cancellationToken);
    }

    public async Task<MenuItemDto> SetMenuItemHighlightAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid menuItemId,
        SetMenuItemHighlightRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var menuItem = await GetOwnedMenuItemEntityAsync(restaurant.Id, menuItemId, cancellationToken);

        menuItem.IsHighlighted = true;
        menuItem.HighlightTag = NormalizeHighlightTag(request.HighlightTag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMenuItemDtoAsync(menuItem.Id, restaurant.Id, cancellationToken);
    }

    public async Task<MenuItemDto> RemoveMenuItemHighlightAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid menuItemId,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var menuItem = await GetOwnedMenuItemEntityAsync(restaurant.Id, menuItemId, cancellationToken);

        menuItem.IsHighlighted = false;
        menuItem.HighlightTag = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMenuItemDtoAsync(menuItem.Id, restaurant.Id, cancellationToken);
    }

    public async Task<MenuItemDto> ToggleAvailabilityAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid menuItemId,
        ToggleMenuItemAvailabilityRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var menuItem = await GetOwnedMenuItemEntityAsync(restaurant.Id, menuItemId, cancellationToken);

        if (!menuItem.MenuCategoryId.HasValue)
        {
            throw new MenuManagementServiceException(
                "Menu item category relationship is invalid.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["menuItemId"] = ["Menu item must belong to a valid category."]
                });
        }

        await GetOwnedCategoryAsync(restaurant.Id, menuItem.MenuCategoryId.Value, cancellationToken);

        menuItem.IsAvailable = request.IsAvailable;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetMenuItemDtoAsync(menuItem.Id, restaurant.Id, cancellationToken);
    }

    public async Task DeleteMenuItemAsync(Guid ownerId, Guid restaurantId, Guid menuItemId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var menuItem = await GetOwnedMenuItemEntityAsync(restaurant.Id, menuItemId, cancellationToken);

        var usedInOrders = await _dbContext.OrderItems
            .AnyAsync(orderItem => orderItem.MenuItemId == menuItem.Id, cancellationToken);

        if (usedInOrders)
        {
            throw new MenuManagementServiceException(
                "This menu item cannot be deleted because it is referenced by existing orders.",
                StatusCodes.Status409Conflict);
        }

        _dbContext.MenuItems.Remove(menuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Restaurant> GetApprovedOwnerRestaurantAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await _dbContext.Restaurants
            .Include(entity => entity.Ratings)
            .FirstOrDefaultAsync(entity => entity.OwnerId == ownerId && entity.Id == restaurantId, cancellationToken);

        if (restaurant is null)
        {
            throw new MenuManagementServiceException(
                "Restaurant was not found for this owner.",
                StatusCodes.Status404NotFound);
        }

        if (restaurant.ApprovalStatus != RestaurantApprovalStatus.Approved)
        {
            throw new MenuManagementServiceException(
                "Only approved restaurant owners can manage menu data.",
                StatusCodes.Status403Forbidden,
                new Dictionary<string, string[]>
                {
                    ["approvalStatus"] = [restaurant.ApprovalStatus.ToString()]
                });
        }

        return restaurant;
    }

    private async Task<MenuCategory> GetOwnedCategoryAsync(Guid restaurantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _dbContext.MenuCategories
            .FirstOrDefaultAsync(
                entity => entity.Id == categoryId && entity.RestaurantId == restaurantId,
                cancellationToken);

        if (category is null)
        {
            throw new MenuManagementServiceException(
                "Menu category was not found for this restaurant.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["menuCategoryId"] = ["The selected category does not belong to this restaurant."]
                });
        }

        return category;
    }

    private async Task<MenuItem> GetOwnedMenuItemEntityAsync(Guid restaurantId, Guid menuItemId, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems
            .FirstOrDefaultAsync(
                entity => entity.Id == menuItemId && entity.RestaurantId == restaurantId,
                cancellationToken);

        if (menuItem is null)
        {
            throw new MenuManagementServiceException(
                "Menu item was not found for this restaurant.",
                StatusCodes.Status404NotFound);
        }

        return menuItem;
    }

    private async Task<MenuItemDto> GetMenuItemDtoAsync(Guid menuItemId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.Id == menuItemId && item.RestaurantId == restaurantId)
            .Select(MapMenuItem())
            .FirstOrDefaultAsync(cancellationToken);

        if (menuItem is null)
        {
            throw new MenuManagementServiceException(
                "Menu item was not found for this restaurant.",
                StatusCodes.Status404NotFound);
        }

        return menuItem;
    }

    private static System.Linq.Expressions.Expression<Func<MenuItem, MenuItemDto>> MapMenuItem()
    {
        return item => new MenuItemDto
        {
            Id = item.Id,
            RestaurantId = item.RestaurantId,
            AverageRating = item.Restaurant != null
                ? Math.Round(item.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                : 0d,
            TotalRatingsCount = item.Restaurant != null
                ? item.Restaurant.Ratings.Count()
                : 0,
            MenuCategoryId = item.MenuCategoryId ?? Guid.Empty,
            MenuCategoryName = item.MenuCategory != null ? item.MenuCategory.Name : string.Empty,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsHighlighted = item.IsHighlighted,
            HighlightTag = item.HighlightTag,
            IsAvailable = item.IsAvailable,
            DisplayOrder = item.DisplayOrder
        };
    }

    private static string NormalizeHighlightTag(string highlightTag)
    {
        return highlightTag.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
