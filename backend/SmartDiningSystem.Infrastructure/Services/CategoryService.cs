using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.MenuManagement;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _dbContext;

    public CategoryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetOwnerCategoriesAsync(Guid ownerId, Guid restaurantId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);

        return await _dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => category.RestaurantId == restaurant.Id)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(MapCategory())
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuCategoryDto> CreateCategoryAsync(
        Guid ownerId,
        Guid restaurantId,
        CreateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var normalizedName = request.Name.Trim();

        var nameExists = await _dbContext.MenuCategories
            .AnyAsync(
                category => category.RestaurantId == restaurant.Id && category.Name == normalizedName,
                cancellationToken);

        if (nameExists)
        {
            throw new MenuManagementServiceException(
                "A category with this name already exists for your restaurant.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, string[]>
                {
                    [nameof(request.Name)] = ["Category name must be unique within the restaurant."]
                });
        }

        var category = new MenuCategory
        {
            Id = Guid.NewGuid(),
            RestaurantId = restaurant.Id,
            Name = normalizedName,
            Description = NormalizeOptionalText(request.Description),
            DisplayOrder = request.DisplayOrder ?? 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MenuCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCategoryValue(category, restaurant);
    }

    public async Task<MenuCategoryDto> UpdateCategoryAsync(
        Guid ownerId,
        Guid restaurantId,
        Guid categoryId,
        UpdateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var category = await GetOwnedCategoryAsync(restaurant.Id, categoryId, cancellationToken);
        var normalizedName = request.Name.Trim();

        var nameExists = await _dbContext.MenuCategories
            .AnyAsync(
                existing => existing.RestaurantId == restaurant.Id &&
                            existing.Id != categoryId &&
                            existing.Name == normalizedName,
                cancellationToken);

        if (nameExists)
        {
            throw new MenuManagementServiceException(
                "A category with this name already exists for your restaurant.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, string[]>
                {
                    [nameof(request.Name)] = ["Category name must be unique within the restaurant."]
                });
        }

        category.Name = normalizedName;
        category.Description = NormalizeOptionalText(request.Description);
        category.DisplayOrder = request.DisplayOrder ?? 0;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCategoryValue(category, restaurant);
    }

    public async Task DeleteCategoryAsync(Guid ownerId, Guid restaurantId, Guid categoryId, CancellationToken cancellationToken)
    {
        var restaurant = await GetApprovedOwnerRestaurantAsync(ownerId, restaurantId, cancellationToken);
        var category = await GetOwnedCategoryAsync(restaurant.Id, categoryId, cancellationToken);

        var hasItems = await _dbContext.MenuItems
            .AnyAsync(item => item.MenuCategoryId == category.Id, cancellationToken);

        if (hasItems)
        {
            throw new MenuManagementServiceException(
                "This category cannot be deleted because it still has menu items.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, string[]>
                {
                    ["categoryId"] = ["Delete or move the menu items before deleting this category."]
                });
        }

        _dbContext.MenuCategories.Remove(category);
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
                StatusCodes.Status404NotFound);
        }

        return category;
    }

    private static System.Linq.Expressions.Expression<Func<MenuCategory, MenuCategoryDto>> MapCategory()
    {
        return category => new MenuCategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            AverageRating = category.Restaurant != null
                ? Math.Round(category.Restaurant.Ratings.Select(rating => (double?)rating.Stars).Average() ?? 0d, 2)
                : 0d,
            TotalRatingsCount = category.Restaurant != null
                ? category.Restaurant.Ratings.Count()
                : 0,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder
        };
    }

    private static MenuCategoryDto MapCategoryValue(MenuCategory category, Restaurant restaurant)
    {
        return new MenuCategoryDto
        {
            Id = category.Id,
            RestaurantId = category.RestaurantId,
            AverageRating = Math.Round(restaurant.Ratings.Select(rating => (double)rating.Stars).DefaultIfEmpty().Average(), 2),
            TotalRatingsCount = restaurant.Ratings.Count,
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
