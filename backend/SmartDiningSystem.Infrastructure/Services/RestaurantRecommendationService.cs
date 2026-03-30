using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class RestaurantRecommendationService : IRestaurantRecommendationService
{
    private const int RecommendationLimit = 10;

    private readonly AppDbContext _dbContext;

    public RestaurantRecommendationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponseDto<RecommendedRestaurantDto>> GetRecommendationsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        var userEligible = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId
                    && user.IsActive
                    && user.Role == UserRole.User,
                cancellationToken);

        if (!userEligible)
        {
            throw new RestaurantRecommendationServiceException(
                "Authenticated user account was not found or is not allowed to receive recommendations.",
                StatusCodes.Status401Unauthorized);
        }

        var restaurants = await LoadApprovedRestaurantsWithSignalsAsync(cancellationToken);
        if (restaurants.Count == 0)
        {
            return BuildPaginationResponse([], pageNumber, pageSize);
        }

        var hasHistory = await _dbContext.Orders
            .AsNoTracking()
            .AnyAsync(order => order.UserId == userId, cancellationToken);

        if (!hasHistory)
        {
            return BuildPaginationResponse(BuildFallbackRecommendations(restaurants), pageNumber, pageSize);
        }

        var userRestaurantCounts = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId)
            .GroupBy(order => order.RestaurantId)
            .Select(group => new { RestaurantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.Count, cancellationToken);

        var userCategoryWeights = await (
                from orderItem in _dbContext.OrderItems.AsNoTracking()
                join order in _dbContext.Orders.AsNoTracking() on orderItem.OrderId equals order.Id
                join menuItem in _dbContext.MenuItems.AsNoTracking() on orderItem.MenuItemId equals menuItem.Id
                where order.UserId == userId && menuItem.MenuCategoryId != null
                group orderItem by menuItem.MenuCategoryId!.Value
                into grouped
                select new { CategoryId = grouped.Key, Weight = grouped.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.CategoryId, item => item.Weight, cancellationToken);

        var userMenuItemWeights = await (
                from orderItem in _dbContext.OrderItems.AsNoTracking()
                join order in _dbContext.Orders.AsNoTracking() on orderItem.OrderId equals order.Id
                where order.UserId == userId
                group orderItem by orderItem.MenuItemId
                into grouped
                select new { MenuItemId = grouped.Key, Weight = grouped.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.MenuItemId, item => item.Weight, cancellationToken);

        var ranked = restaurants
            .Select(restaurant =>
            {
                var previousRestaurantOrders = userRestaurantCounts.TryGetValue(restaurant.RestaurantId, out var restaurantCount)
                    ? restaurantCount
                    : 0;

                var categoryScore = restaurant.CategoryIds
                    .Distinct()
                    .Sum(categoryId => userCategoryWeights.TryGetValue(categoryId, out var weight) ? weight : 0);

                var itemScore = restaurant.MenuItemIds
                    .Distinct()
                    .Sum(menuItemId => userMenuItemWeights.TryGetValue(menuItemId, out var weight) ? weight : 0);

                var score = previousRestaurantOrders * 1000
                    + categoryScore * 100
                    + itemScore * 10
                    + restaurant.TotalRatingsCount
                    + restaurant.TotalOrderCount;

                return new RankedRecommendation(
                    restaurant,
                    score,
                    previousRestaurantOrders,
                    categoryScore,
                    itemScore,
                    GetPersonalizedReason(previousRestaurantOrders, categoryScore, itemScore));
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Restaurant.AverageRating)
            .ThenByDescending(item => item.Restaurant.TotalRatingsCount)
            .ThenByDescending(item => item.Restaurant.TotalOrderCount)
            .ThenBy(item => item.Restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .Take(RecommendationLimit)
            .ToList();

        if (ranked.Count >= RecommendationLimit)
        {
            return BuildPaginationResponse(ranked.Select(MapRecommendation).ToList(), pageNumber, pageSize);
        }

        var fallbackFill = BuildFallbackRecommendations(
                restaurants,
                ranked.Select(item => item.Restaurant.RestaurantId).ToHashSet())
            .Take(RecommendationLimit - ranked.Count)
            .Select(dto => new RankedRecommendation(
                new RestaurantRecommendationCandidate(
                    dto.RestaurantId,
                    dto.Name,
                    dto.Description,
                    dto.ImageUrl,
                    dto.Latitude,
                    dto.Longitude,
                    dto.Address,
                    dto.ContactPhone,
                    dto.AverageRating,
                    dto.TotalRatingsCount,
                    0,
                    new List<Guid>(),
                    new List<Guid>()),
                0,
                0,
                0,
                0,
                dto.RecommendationReason));

        var recommendations = ranked
            .Concat(fallbackFill)
            .Take(RecommendationLimit)
            .Select(MapRecommendation)
            .ToList();

        return BuildPaginationResponse(recommendations, pageNumber, pageSize);
    }

    private static PaginationResponseDto<RecommendedRestaurantDto> BuildPaginationResponse(
        IReadOnlyList<RecommendedRestaurantDto> recommendations,
        int pageNumber,
        int pageSize)
    {
        var totalCount = recommendations.Count;
        var pagedItems = recommendations
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationResponseDto<RecommendedRestaurantDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            HasPreviousPage = pageNumber > 1 && totalCount > 0,
            HasNextPage = totalCount > 0 && pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        Dictionary<string, string[]>? errors = null;

        if (pageNumber < 1)
        {
            errors = new Dictionary<string, string[]>
            {
                ["pageNumber"] = ["Page number must be greater than or equal to 1."]
            };
        }

        if (pageSize is < 1 or > 50)
        {
            errors ??= new Dictionary<string, string[]>();
            errors["pageSize"] = ["Page size must be between 1 and 50."];
        }

        if (errors is not null)
        {
            throw new RestaurantRecommendationServiceException(
                "Invalid pagination parameters.",
                StatusCodes.Status400BadRequest,
                errors);
        }
    }

    private async Task<List<RestaurantRecommendationCandidate>> LoadApprovedRestaurantsWithSignalsAsync(
        CancellationToken cancellationToken)
    {
        var restaurants = await _dbContext.Restaurants
            .AsNoTracking()
            .Where(restaurant => restaurant.ApprovalStatus == RestaurantApprovalStatus.Approved)
            .OrderBy(restaurant => restaurant.Name)
            .Select(restaurant => new
            {
                restaurant.Id,
                restaurant.Name,
                restaurant.Description,
                restaurant.ImageUrl,
                restaurant.Latitude,
                restaurant.Longitude,
                restaurant.Address,
                restaurant.ContactPhone
            })
            .ToListAsync(cancellationToken);

        var restaurantIds = restaurants.Select(restaurant => restaurant.Id).ToList();

        var orderCounts = await _dbContext.Orders
            .AsNoTracking()
            .Where(order => restaurantIds.Contains(order.RestaurantId))
            .GroupBy(order => order.RestaurantId)
            .Select(group => new { RestaurantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.Count, cancellationToken);

        var ratingAggregates = await _dbContext.RestaurantRatings
            .AsNoTracking()
            .Where(rating => restaurantIds.Contains(rating.RestaurantId))
            .GroupBy(rating => rating.RestaurantId)
            .Select(group => new
            {
                RestaurantId = group.Key,
                AverageRating = Math.Round(group.Average(rating => (double)rating.Stars), 2),
                TotalRatingsCount = group.Count()
            })
            .ToDictionaryAsync(
                item => item.RestaurantId,
                item => new { item.AverageRating, item.TotalRatingsCount },
                cancellationToken);

        var categoryIdsByRestaurant = await _dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => restaurantIds.Contains(category.RestaurantId))
            .GroupBy(category => category.RestaurantId)
            .Select(group => new
            {
                RestaurantId = group.Key,
                CategoryIds = group.Select(category => category.Id).ToList()
            })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.CategoryIds, cancellationToken);

        var menuItemIdsByRestaurant = await _dbContext.MenuItems
            .AsNoTracking()
            .Where(menuItem => restaurantIds.Contains(menuItem.RestaurantId))
            .GroupBy(menuItem => menuItem.RestaurantId)
            .Select(group => new
            {
                RestaurantId = group.Key,
                MenuItemIds = group.Select(menuItem => menuItem.Id).ToList()
            })
            .ToDictionaryAsync(item => item.RestaurantId, item => item.MenuItemIds, cancellationToken);

        return restaurants
            .Select(restaurant =>
            {
                var averageRating = 0d;
                var totalRatingsCount = 0;
                if (ratingAggregates.TryGetValue(restaurant.Id, out var ratingAggregate))
                {
                    averageRating = ratingAggregate.AverageRating;
                    totalRatingsCount = ratingAggregate.TotalRatingsCount;
                }

                return new RestaurantRecommendationCandidate(
                    restaurant.Id,
                    restaurant.Name,
                    restaurant.Description,
                    restaurant.ImageUrl,
                    restaurant.Latitude,
                    restaurant.Longitude,
                    restaurant.Address,
                    restaurant.ContactPhone,
                    averageRating,
                    totalRatingsCount,
                    orderCounts.TryGetValue(restaurant.Id, out var orderCount) ? orderCount : 0,
                    categoryIdsByRestaurant.TryGetValue(restaurant.Id, out var categoryIds) ? categoryIds : new List<Guid>(),
                    menuItemIdsByRestaurant.TryGetValue(restaurant.Id, out var menuItemIds) ? menuItemIds : new List<Guid>());
            })
            .ToList();
    }

    private static IReadOnlyList<RecommendedRestaurantDto> BuildFallbackRecommendations(
        IReadOnlyList<RestaurantRecommendationCandidate> restaurants,
        HashSet<Guid>? excludedRestaurantIds = null)
    {
        excludedRestaurantIds ??= [];

        var rated = restaurants
            .Where(restaurant => !excludedRestaurantIds.Contains(restaurant.RestaurantId))
            .OrderByDescending(restaurant => restaurant.AverageRating)
            .ThenByDescending(restaurant => restaurant.TotalRatingsCount)
            .ThenByDescending(restaurant => restaurant.TotalOrderCount)
            .ThenBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .Take(RecommendationLimit)
            .Select(restaurant => MapFallbackRecommendation(restaurant, "Highly rated by diners"))
            .ToList();

        var selectedIds = rated.Select(restaurant => restaurant.RestaurantId).ToHashSet();

        var popular = restaurants
            .Where(restaurant =>
                !excludedRestaurantIds.Contains(restaurant.RestaurantId) &&
                !selectedIds.Contains(restaurant.RestaurantId))
            .OrderByDescending(restaurant => restaurant.TotalOrderCount)
            .ThenByDescending(restaurant => restaurant.AverageRating)
            .ThenByDescending(restaurant => restaurant.TotalRatingsCount)
            .ThenBy(restaurant => restaurant.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, RecommendationLimit - rated.Count))
            .Select(restaurant => MapFallbackRecommendation(restaurant, "Popular with customers"))
            .ToList();

        foreach (var item in popular)
        {
            selectedIds.Add(item.RestaurantId);
        }

        var randomFill = restaurants
            .Where(restaurant =>
                !excludedRestaurantIds.Contains(restaurant.RestaurantId) &&
                !selectedIds.Contains(restaurant.RestaurantId))
            .OrderBy(restaurant => Guid.NewGuid())
            .Take(Math.Max(0, RecommendationLimit - rated.Count - popular.Count))
            .Select(restaurant => MapFallbackRecommendation(restaurant, "Recommended for you"))
            .ToList();

        return rated
            .Concat(popular)
            .Concat(randomFill)
            .Take(RecommendationLimit)
            .ToList();
    }

    private static string GetPersonalizedReason(int previousRestaurantOrders, int categoryScore, int itemScore)
    {
        if (previousRestaurantOrders > 0)
        {
            return "Based on your previous orders from this restaurant";
        }

        if (categoryScore > 0)
        {
            return "Because you often order from similar menu categories";
        }

        if (itemScore > 0)
        {
            return "Because it matches menu items you order often";
        }

        return "Recommended for you";
    }

    private static RecommendedRestaurantDto MapRecommendation(RankedRecommendation recommendation)
    {
        return new RecommendedRestaurantDto
        {
            RestaurantId = recommendation.Restaurant.RestaurantId,
            Name = recommendation.Restaurant.Name,
            Description = recommendation.Restaurant.Description,
            ImageUrl = recommendation.Restaurant.ImageUrl,
            Latitude = recommendation.Restaurant.Latitude,
            Longitude = recommendation.Restaurant.Longitude,
            Address = recommendation.Restaurant.Address,
            ContactPhone = recommendation.Restaurant.ContactPhone,
            AverageRating = recommendation.Restaurant.AverageRating,
            TotalRatingsCount = recommendation.Restaurant.TotalRatingsCount,
            RecommendationReason = recommendation.Reason
        };
    }

    private static RecommendedRestaurantDto MapFallbackRecommendation(
        RestaurantRecommendationCandidate restaurant,
        string reason)
    {
        return new RecommendedRestaurantDto
        {
            RestaurantId = restaurant.RestaurantId,
            Name = restaurant.Name,
            Description = restaurant.Description,
            ImageUrl = restaurant.ImageUrl,
            Latitude = restaurant.Latitude,
            Longitude = restaurant.Longitude,
            Address = restaurant.Address,
            ContactPhone = restaurant.ContactPhone,
            AverageRating = restaurant.AverageRating,
            TotalRatingsCount = restaurant.TotalRatingsCount,
            RecommendationReason = reason
        };
    }

    private sealed class RestaurantRecommendationCandidate
    {
        public RestaurantRecommendationCandidate(
            Guid restaurantId,
            string name,
            string? description,
            string? imageUrl,
            double? latitude,
            double? longitude,
            string address,
            string contactPhone,
            double averageRating,
            int totalRatingsCount,
            int totalOrderCount,
            List<Guid> categoryIds,
            List<Guid> menuItemIds)
        {
            RestaurantId = restaurantId;
            Name = name;
            Description = description;
            ImageUrl = imageUrl;
            Latitude = latitude;
            Longitude = longitude;
            Address = address;
            ContactPhone = contactPhone;
            AverageRating = averageRating;
            TotalRatingsCount = totalRatingsCount;
            TotalOrderCount = totalOrderCount;
            CategoryIds = categoryIds;
            MenuItemIds = menuItemIds;
        }

        public Guid RestaurantId { get; }
        public string Name { get; }
        public string? Description { get; }
        public string? ImageUrl { get; }
        public double? Latitude { get; }
        public double? Longitude { get; }
        public string Address { get; }
        public string ContactPhone { get; }
        public double AverageRating { get; }
        public int TotalRatingsCount { get; }
        public int TotalOrderCount { get; }
        public List<Guid> CategoryIds { get; }
        public List<Guid> MenuItemIds { get; }
    }

    private sealed record RankedRecommendation(
        RestaurantRecommendationCandidate Restaurant,
        int Score,
        int PreviousRestaurantOrders,
        int CategoryScore,
        int ItemScore,
        string Reason);
}
