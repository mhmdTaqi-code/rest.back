using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.MenuManagement;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class MenuRecommendationService : IMenuRecommendationService
{
    private const int RecommendationLimit = 10;

    private readonly AppDbContext _dbContext;

    public MenuRecommendationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RecommendedMenuItemDto>> GetRecommendationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userEligible = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId
                    && user.IsActive
                    && user.Role == UserRole.User,
                cancellationToken);

        if (!userEligible)
        {
            throw new MenuRecommendationServiceException(
                "Authenticated user account was not found or is not allowed to receive menu recommendations.",
                StatusCodes.Status401Unauthorized);
        }

        var candidates = await LoadEligibleMenuItemCandidatesAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return Array.Empty<RecommendedMenuItemDto>();
        }

        var hasHistory = await _dbContext.Orders
            .AsNoTracking()
            .AnyAsync(order => order.UserId == userId, cancellationToken);

        if (!hasHistory)
        {
            return BuildRandomFallback(candidates);
        }

        var userMenuItemWeights = await (
                from orderItem in _dbContext.OrderItems.AsNoTracking()
                join order in _dbContext.Orders.AsNoTracking() on orderItem.OrderId equals order.Id
                where order.UserId == userId
                group orderItem by orderItem.MenuItemId
                into grouped
                select new { MenuItemId = grouped.Key, Weight = grouped.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.MenuItemId, item => item.Weight, cancellationToken);

        var userCategoryWeights = await (
                from orderItem in _dbContext.OrderItems.AsNoTracking()
                join order in _dbContext.Orders.AsNoTracking() on orderItem.OrderId equals order.Id
                join menuItem in _dbContext.MenuItems.AsNoTracking() on orderItem.MenuItemId equals menuItem.Id
                where order.UserId == userId && menuItem.MenuCategoryId != null
                group orderItem by menuItem.MenuCategoryId!.Value
                into grouped
                select new { MenuCategoryId = grouped.Key, Weight = grouped.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.MenuCategoryId, item => item.Weight, cancellationToken);

        var userRestaurantWeights = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .GroupBy(order => order.RestaurantId)
            .Select(group => new { RestaurantId = group.Key, Weight = group.Count() })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.Weight, cancellationToken);

        var ranked = candidates
            .Select(candidate =>
            {
                var itemScore = userMenuItemWeights.TryGetValue(candidate.MenuItemId, out var itemWeight)
                    ? itemWeight
                    : 0;

                var categoryScore = userCategoryWeights.TryGetValue(candidate.MenuCategoryId, out var categoryWeight)
                    ? categoryWeight
                    : 0;

                var restaurantScore = userRestaurantWeights.TryGetValue(candidate.RestaurantId, out var restaurantWeight)
                    ? restaurantWeight
                    : 0;

                var score = itemScore * 1000
                    + categoryScore * 100
                    + restaurantScore * 10
                    + (int)Math.Round(candidate.AverageRating * 10, MidpointRounding.AwayFromZero);

                return new RankedMenuRecommendation(
                    candidate,
                    score,
                    itemScore,
                    categoryScore,
                    restaurantScore,
                    BuildReason(itemScore, categoryScore, restaurantScore));
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Candidate.AverageRating)
            .ThenBy(item => item.Candidate.RestaurantName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(RecommendationLimit)
            .ToList();

        if (ranked.Count == 0)
        {
            return BuildRandomFallback(candidates);
        }

        if (ranked.Count >= RecommendationLimit)
        {
            return ranked.Select(MapRecommendation).ToList();
        }

        var rankedIds = ranked.Select(item => item.Candidate.MenuItemId).ToHashSet();
        var fallback = BuildRandomFallback(candidates, rankedIds)
            .Take(RecommendationLimit - ranked.Count);

        return ranked
            .Select(MapRecommendation)
            .Concat(fallback)
            .Take(RecommendationLimit)
            .ToList();
    }

    private async Task<List<MenuItemRecommendationCandidate>> LoadEligibleMenuItemCandidatesAsync(
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem =>
                menuItem.IsAvailable &&
                menuItem.MenuCategoryId != null &&
                menuItem.MenuCategory != null &&
                menuItem.MenuCategory.IsActive &&
                menuItem.Restaurant != null &&
                menuItem.Restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved)
            .OrderBy(menuItem => menuItem.Name)
            .Select(menuItem => new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Description,
                menuItem.Price,
                menuItem.ImageUrl,
                menuItem.RestaurantId,
                RestaurantName = menuItem.Restaurant!.Name,
                MenuCategoryId = menuItem.MenuCategoryId!.Value,
                MenuCategoryName = menuItem.MenuCategory!.Name
            })
            .ToListAsync(cancellationToken);

        var restaurantIds = items
            .Select(item => item.RestaurantId)
            .Distinct()
            .ToList();

        var ratingAverages = await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Where(rating => restaurantIds.Contains(rating.RestaurantId))
            .GroupBy(rating => rating.RestaurantId)
            .Select(group => new
            {
                RestaurantId = group.Key,
                AverageRating = decimal.Round(group.Sum(rating => rating.Stars) / (decimal)group.Count(), 2)
            })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.AverageRating, cancellationToken);

        return items
            .Select(item => new MenuItemRecommendationCandidate(
                item.RestaurantId,
                item.RestaurantName,
                item.MenuCategoryId,
                item.MenuCategoryName,
                item.Id,
                item.Name,
                item.Description,
                item.Price,
                item.ImageUrl,
                ratingAverages.TryGetValue(item.RestaurantId, out var averageRating) ? averageRating : 0m))
            .ToList();
    }

    private static IReadOnlyList<RecommendedMenuItemDto> BuildRandomFallback(
        IReadOnlyList<MenuItemRecommendationCandidate> candidates,
        HashSet<Guid>? excludedMenuItemIds = null)
    {
        excludedMenuItemIds ??= new HashSet<Guid>();

        return candidates
            .Where(candidate => !excludedMenuItemIds.Contains(candidate.MenuItemId))
            .OrderBy(_ => Guid.NewGuid())
            .Take(RecommendationLimit)
            .Select(candidate => new RecommendedMenuItemDto
            {
                RestaurantId = candidate.RestaurantId,
                RestaurantName = candidate.RestaurantName,
                MenuCategoryId = candidate.MenuCategoryId,
                MenuCategoryName = candidate.MenuCategoryName,
                MenuItemId = candidate.MenuItemId,
                Name = candidate.Name,
                Description = candidate.Description,
                Price = candidate.Price,
                ImageUrl = candidate.ImageUrl,
                AverageRating = candidate.AverageRating,
                RecommendationReason = "Recommended for you"
            })
            .ToList();
    }

    private static string BuildReason(int itemScore, int categoryScore, int restaurantScore)
    {
        if (itemScore > 0)
        {
            return "Based on dishes you ordered before";
        }

        if (categoryScore > 0)
        {
            return "Because you often order from this category";
        }

        if (restaurantScore > 0)
        {
            return "From a restaurant you order from often";
        }

        return "Recommended for you";
    }

    private static RecommendedMenuItemDto MapRecommendation(RankedMenuRecommendation recommendation)
    {
        return new RecommendedMenuItemDto
        {
            RestaurantId = recommendation.Candidate.RestaurantId,
            RestaurantName = recommendation.Candidate.RestaurantName,
            MenuCategoryId = recommendation.Candidate.MenuCategoryId,
            MenuCategoryName = recommendation.Candidate.MenuCategoryName,
            MenuItemId = recommendation.Candidate.MenuItemId,
            Name = recommendation.Candidate.Name,
            Description = recommendation.Candidate.Description,
            Price = recommendation.Candidate.Price,
            ImageUrl = recommendation.Candidate.ImageUrl,
            AverageRating = recommendation.Candidate.AverageRating,
            RecommendationReason = recommendation.Reason
        };
    }

    private sealed record MenuItemRecommendationCandidate(
        Guid RestaurantId,
        string RestaurantName,
        Guid MenuCategoryId,
        string MenuCategoryName,
        Guid MenuItemId,
        string Name,
        string? Description,
        decimal Price,
        string ImageUrl,
        decimal AverageRating);

    private sealed record RankedMenuRecommendation(
        MenuItemRecommendationCandidate Candidate,
        int Score,
        int ItemScore,
        int CategoryScore,
        int RestaurantScore,
        string Reason);
}
