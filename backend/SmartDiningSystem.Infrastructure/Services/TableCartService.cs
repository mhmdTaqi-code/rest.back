using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.DTOs.TableOrdering;
using SmartDiningSystem.Application.Services.Exceptions;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Infrastructure.Data;

namespace SmartDiningSystem.Infrastructure.Services;

public class TableCartService : ITableCartService
{
    private readonly AppDbContext _dbContext;
    private readonly IRestaurantTableAccessService _restaurantTableAccessService;

    public TableCartService(
        AppDbContext dbContext,
        IRestaurantTableAccessService restaurantTableAccessService)
    {
        _dbContext = dbContext;
        _restaurantTableAccessService = restaurantTableAccessService;
    }

    public async Task<TableCartResponseDto> GetCurrentCartAsync(Guid userId, string tableToken, CancellationToken cancellationToken)
    {
        await EnsureActiveUserAsync(userId, cancellationToken);

        var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);
        var cart = await LoadCartAsync(userId, table.RestaurantTableId, cancellationToken);

        return cart is null
            ? BuildEmptyCart(table)
            : MapCart(cart, table);
    }

    public async Task<TableCartResponseDto> AddItemAsync(
        Guid userId,
        string tableToken,
        AddCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureActiveUserAsync(userId, cancellationToken);

        var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);
        await LoadValidMenuItemAsync(table.RestaurantId, request.MenuItemId, cancellationToken);

        var cart = await LoadCartAsync(userId, table.RestaurantTableId, cancellationToken);
        if (cart is null)
        {
            cart = new TableCart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RestaurantId = table.RestaurantId,
                RestaurantTableId = table.RestaurantTableId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.TableCarts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(item => item.MenuItemId == request.MenuItemId);
        if (existingItem is null)
        {
            cart.Items.Add(new TableCartItem
            {
                Id = Guid.NewGuid(),
                MenuItemId = request.MenuItemId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity += request.Quantity;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(cart).Collection(entity => entity.Items).LoadAsync(cancellationToken);

        return await GetCurrentCartAsync(userId, table.TableToken, cancellationToken);
    }

    public async Task<TableCartResponseDto> UpdateItemAsync(
        Guid userId,
        string tableToken,
        Guid cartItemId,
        UpdateCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        await EnsureActiveUserAsync(userId, cancellationToken);

        var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);
        var cart = await LoadCartAsync(userId, table.RestaurantTableId, cancellationToken);
        if (cart is null)
        {
            throw BuildCartNotFoundException();
        }

        var cartItem = cart.Items.FirstOrDefault(item => item.Id == cartItemId);
        if (cartItem is null)
        {
            throw BuildCartItemNotFoundException();
        }

        await LoadValidMenuItemAsync(table.RestaurantId, cartItem.MenuItemId, cancellationToken);

        cartItem.Quantity = request.Quantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCurrentCartAsync(userId, table.TableToken, cancellationToken);
    }

    public async Task<TableCartResponseDto> RemoveItemAsync(
        Guid userId,
        string tableToken,
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        await EnsureActiveUserAsync(userId, cancellationToken);

        var table = await _restaurantTableAccessService.ResolveTableAsync(tableToken, cancellationToken);
        var cart = await LoadCartAsync(userId, table.RestaurantTableId, cancellationToken);
        if (cart is null)
        {
            throw BuildCartNotFoundException();
        }

        var cartItem = cart.Items.FirstOrDefault(item => item.Id == cartItemId);
        if (cartItem is null)
        {
            throw BuildCartItemNotFoundException();
        }

        _dbContext.TableCartItems.Remove(cartItem);
        cart.UpdatedAtUtc = DateTime.UtcNow;

        if (cart.Items.Count == 1)
        {
            _dbContext.TableCarts.Remove(cart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCurrentCartAsync(userId, table.TableToken, cancellationToken);
    }

    private async Task EnsureActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            throw new TableOrderingServiceException(
                "Authenticated user account was not found.",
                StatusCodes.Status401Unauthorized);
        }
    }

    private async Task<TableCart?> LoadCartAsync(Guid userId, Guid restaurantTableId, CancellationToken cancellationToken)
    {
        return await _dbContext.TableCarts
            .Include(cart => cart.Items)
            // EF uses this include path to load optional navigations; mapping/validation below still guards nulls.
            .ThenInclude(item => item.MenuItem!)
            .ThenInclude(menuItem => menuItem.MenuCategory)
            .Include(cart => cart.Restaurant)
            .Include(cart => cart.RestaurantTable)
            .FirstOrDefaultAsync(
                cart => cart.UserId == userId && cart.RestaurantTableId == restaurantTableId,
                cancellationToken);
    }

    private async Task<MenuItem> LoadValidMenuItemAsync(Guid restaurantId, Guid menuItemId, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems
            .Include(item => item.MenuCategory)
            .FirstOrDefaultAsync(item => item.Id == menuItemId, cancellationToken);

        if (menuItem is null)
        {
            throw new TableOrderingServiceException(
                "Menu item was not found.",
                StatusCodes.Status404NotFound,
                new Dictionary<string, string[]>
                {
                    ["menuItemId"] = ["The selected menu item does not exist."]
                });
        }

        if (menuItem.RestaurantId != restaurantId)
        {
            throw new TableOrderingServiceException(
                "Menu item does not belong to this restaurant table.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["menuItemId"] = ["The selected menu item belongs to a different restaurant."]
                });
        }

        if (menuItem.MenuCategory is null || menuItem.MenuCategory.RestaurantId != restaurantId)
        {
            throw new TableOrderingServiceException(
                "Menu category relationship is invalid for this item.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["menuItemId"] = ["The selected menu item has an invalid category relationship."]
                });
        }

        if (!menuItem.MenuCategory.IsActive || !menuItem.IsAvailable)
        {
            throw new TableOrderingServiceException(
                "The selected menu item is currently unavailable.",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, string[]>
                {
                    ["menuItemId"] = ["The selected menu item is unavailable."]
                });
        }

        return menuItem;
    }

    private static TableCartResponseDto BuildEmptyCart(ResolvedRestaurantTableDto table)
    {
        return new TableCartResponseDto
        {
            CartId = Guid.Empty,
            RestaurantId = table.RestaurantId,
            RestaurantName = table.RestaurantName,
            RestaurantTableId = table.RestaurantTableId,
            TableNumber = table.TableNumber,
            TableDisplayName = table.TableDisplayName,
            TableToken = table.TableToken,
            Items = Array.Empty<TableCartItemDto>(),
            TotalAmount = 0,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static TableCartResponseDto MapCart(TableCart cart, ResolvedRestaurantTableDto table)
    {
        var validItems = new List<(int CategoryDisplayOrder, string MenuItemName, TableCartItemDto ItemDto)>();

        foreach (var cartItem in cart.Items)
        {
            var menuItem = cartItem.MenuItem;
            var category = menuItem?.MenuCategory;
            if (menuItem is null || category is null || !menuItem.MenuCategoryId.HasValue)
            {
                continue;
            }

            validItems.Add((category.DisplayOrder, menuItem.Name, new TableCartItemDto
            {
                CartItemId = cartItem.Id,
                MenuItemId = cartItem.MenuItemId,
                MenuCategoryId = menuItem.MenuCategoryId.Value,
                MenuCategoryName = category.Name,
                MenuItemName = menuItem.Name,
                Description = menuItem.Description,
                ImageUrl = menuItem.ImageUrl,
                UnitPrice = menuItem.Price,
                Quantity = cartItem.Quantity,
                LineTotal = menuItem.Price * cartItem.Quantity,
                IsAvailable = menuItem.IsAvailable && category.IsActive
            }));
        }

        var items = validItems
            .OrderBy(item => item.CategoryDisplayOrder)
            .ThenBy(item => item.MenuItemName)
            .Select(item => item.ItemDto)
            .ToList();

        return new TableCartResponseDto
        {
            CartId = cart.Id,
            RestaurantId = table.RestaurantId,
            RestaurantName = table.RestaurantName,
            RestaurantTableId = table.RestaurantTableId,
            TableNumber = table.TableNumber,
            TableDisplayName = table.TableDisplayName,
            TableToken = table.TableToken,
            Items = items,
            TotalAmount = items.Sum(item => item.LineTotal),
            UpdatedAtUtc = cart.UpdatedAtUtc
        };
    }

    private static TableOrderingServiceException BuildCartNotFoundException()
    {
        return new TableOrderingServiceException(
            "No active cart was found for this table.",
            StatusCodes.Status404NotFound);
    }

    private static TableOrderingServiceException BuildCartItemNotFoundException()
    {
        return new TableOrderingServiceException(
            "Cart item was not found.",
            StatusCodes.Status404NotFound);
    }
}
