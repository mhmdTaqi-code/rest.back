using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.Services.Interfaces;
using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Infrastructure.Services;

namespace SmartDiningSystem.Infrastructure.Data.Seed;

public class AdminSeedService
{
    private const string DevelopmentAdminSeedPassword = "DEV_ADMIN_COOKIE_AUTH_ONLY";
    private const string DemoOwnerSeedPassword = "Owner123!";
    private const string NormalUserSeedPassword = "User123!";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public AdminSeedService(AppDbContext dbContext, IPasswordHashService passwordHashService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync(cancellationToken);
        
        // Setup massive Iraqi demo data injection sequences
        await SeedApprovedRestaurantMenusAsync(cancellationToken);
        
        // Ensure regular user boundaries logically exist to attach histories towards
        await SeedNormalUsersAsync(cancellationToken);
        
        // Create full timeline events utilizing the users + menus
        await SeedDemoOrderHistoryAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var hashedAdminPassword = _passwordHashService.HashPassword(DevelopmentAdminSeedPassword);

        var existingAdmin = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(user => user.Id == AdminAuthenticationService.DevelopmentAdminId, cancellationToken);

        if (existingAdmin is null)
        {
            existingAdmin = await _dbContext.UserAccounts
                .FirstOrDefaultAsync(
                    user => user.PhoneNumber == AdminAuthenticationService.DevelopmentAdminPhone,
                    cancellationToken);
        }

        if (existingAdmin is null)
        {
            existingAdmin = new UserAccount
            {
                Id = AdminAuthenticationService.DevelopmentAdminId,
                FullName = AdminAuthenticationService.DevelopmentAdminFullName,
                PhoneNumber = AdminAuthenticationService.DevelopmentAdminPhone,
                Username = AdminAuthenticationService.DevelopmentAdminPhone,
                PasswordHash = hashedAdminPassword,
                Role = UserRole.Admin,
                IsActive = true,
                IsPhoneVerified = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.UserAccounts.Add(existingAdmin);
        }
        else
        {
            existingAdmin.FullName = AdminAuthenticationService.DevelopmentAdminFullName;
            existingAdmin.PhoneNumber = AdminAuthenticationService.DevelopmentAdminPhone;
            existingAdmin.Username = AdminAuthenticationService.DevelopmentAdminPhone;
            existingAdmin.PasswordHash = hashedAdminPassword;
            existingAdmin.Role = UserRole.Admin;
            existingAdmin.IsActive = true;
            existingAdmin.IsPhoneVerified = true;
            existingAdmin.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedApprovedRestaurantMenusAsync(CancellationToken cancellationToken)
    {
        var hashedDemoOwnerPassword = _passwordHashService.HashPassword(DemoOwnerSeedPassword);
        var menuOwners = IraqiDemoDataBuilder.GetMenuOwners();

        foreach (var ownerDefinition in menuOwners)
        {
            var nowUtc = DateTime.UtcNow;

            var owner = await _dbContext.UserAccounts
                .FirstOrDefaultAsync(user => user.Id == ownerDefinition.OwnerId, cancellationToken);

            if (owner is null)
            {
                owner = new UserAccount
                {
                    Id = ownerDefinition.OwnerId,
                    FullName = ownerDefinition.OwnerName,
                    Username = ownerDefinition.OwnerUsername,
                    PhoneNumber = ownerDefinition.OwnerPhoneNumber,
                    PasswordHash = hashedDemoOwnerPassword,
                    Role = UserRole.RestaurantOwner,
                    IsPhoneVerified = true,
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _dbContext.UserAccounts.Add(owner);
            }
            else
            {
                owner.FullName = ownerDefinition.OwnerName;
                owner.Username = ownerDefinition.OwnerUsername;
                owner.PhoneNumber = ownerDefinition.OwnerPhoneNumber;
                owner.PasswordHash = hashedDemoOwnerPassword;
                owner.Role = UserRole.RestaurantOwner;
                owner.IsPhoneVerified = true;
                owner.IsActive = true;
                owner.UpdatedAtUtc = nowUtc;
            }

            var restaurant = await _dbContext.Restaurants
                .FirstOrDefaultAsync(entity => entity.Id == ownerDefinition.RestaurantId, cancellationToken);

            if (restaurant is null)
            {
                restaurant = new Restaurant
                {
                    Id = ownerDefinition.RestaurantId,
                    OwnerId = ownerDefinition.OwnerId,
                    Name = ownerDefinition.RestaurantName,
                    Description = ownerDefinition.RestaurantDescription,
                    Address = ownerDefinition.RestaurantAddress,
                    ContactPhone = ownerDefinition.RestaurantPhoneNumber,
                    ApprovalStatus = RestaurantApprovalStatus.Approved,
                    ImageUrl = ownerDefinition.ImageUrl,
                    Latitude = ownerDefinition.Latitude,
                    Longitude = ownerDefinition.Longitude,
                    CreatedAtUtc = nowUtc,
                    ApprovedAtUtc = nowUtc,
                    RejectedAtUtc = null,
                    RejectionReason = null
                };

                _dbContext.Restaurants.Add(restaurant);
            }
            else
            {
                restaurant.OwnerId = ownerDefinition.OwnerId;
                restaurant.Name = ownerDefinition.RestaurantName;
                restaurant.Description = ownerDefinition.RestaurantDescription;
                restaurant.Address = ownerDefinition.RestaurantAddress;
                restaurant.ContactPhone = ownerDefinition.RestaurantPhoneNumber;
                restaurant.ApprovalStatus = RestaurantApprovalStatus.Approved;
                restaurant.ImageUrl = ownerDefinition.ImageUrl;
                restaurant.Latitude = ownerDefinition.Latitude;
                restaurant.Longitude = ownerDefinition.Longitude;
                restaurant.ApprovedAtUtc = restaurant.ApprovedAtUtc ?? nowUtc;
                restaurant.RejectedAtUtc = null;
                restaurant.RejectionReason = null;
            }

            foreach (var categoryDefinition in ownerDefinition.Categories)
            {
                var category = await _dbContext.MenuCategories
                    .FirstOrDefaultAsync(entity => entity.Id == categoryDefinition.CategoryId, cancellationToken)
                    ?? await _dbContext.MenuCategories
                        .FirstOrDefaultAsync(
                            entity => entity.RestaurantId == ownerDefinition.RestaurantId &&
                                      entity.Name == categoryDefinition.Name,
                            cancellationToken);

                if (category is null)
                {
                    category = new MenuCategory
                    {
                        Id = categoryDefinition.CategoryId,
                        RestaurantId = ownerDefinition.RestaurantId,
                        Name = categoryDefinition.Name,
                        Description = categoryDefinition.Description,
                        DisplayOrder = categoryDefinition.DisplayOrder,
                        IsActive = true,
                        CreatedAtUtc = nowUtc
                    };

                    _dbContext.MenuCategories.Add(category);
                }
                else
                {
                    category.RestaurantId = ownerDefinition.RestaurantId;
                    category.Name = categoryDefinition.Name;
                    category.Description = categoryDefinition.Description;
                    category.DisplayOrder = categoryDefinition.DisplayOrder;
                    category.IsActive = true;
                }

                foreach (var itemDefinition in categoryDefinition.Items)
                {
                    var menuItem = await _dbContext.MenuItems
                        .FirstOrDefaultAsync(entity => entity.Id == itemDefinition.MenuItemId, cancellationToken)
                        ?? await _dbContext.MenuItems
                            .FirstOrDefaultAsync(
                                entity => entity.RestaurantId == ownerDefinition.RestaurantId &&
                                          entity.MenuCategoryId == category.Id &&
                                          entity.Name == itemDefinition.Name,
                                cancellationToken);

                    if (menuItem is null)
                    {
                        menuItem = new MenuItem
                        {
                            Id = itemDefinition.MenuItemId,
                            RestaurantId = ownerDefinition.RestaurantId,
                            MenuCategoryId = categoryDefinition.CategoryId,
                            Name = itemDefinition.Name,
                            Description = itemDefinition.Description,
                            Price = itemDefinition.Price,
                            ImageUrl = itemDefinition.ImageUrl,
                            IsAvailable = true,
                            DisplayOrder = itemDefinition.DisplayOrder,
                            CreatedAtUtc = nowUtc
                        };

                        _dbContext.MenuItems.Add(menuItem);
                    }
                    else
                    {
                        menuItem.RestaurantId = ownerDefinition.RestaurantId;
                        menuItem.MenuCategoryId = categoryDefinition.CategoryId;
                        menuItem.Name = itemDefinition.Name;
                        menuItem.Description = itemDefinition.Description;
                        menuItem.Price = itemDefinition.Price;
                        menuItem.ImageUrl = itemDefinition.ImageUrl;
                        menuItem.IsAvailable = true;
                        menuItem.DisplayOrder = itemDefinition.DisplayOrder;
                    }
                }
            }

            await SeedRestaurantTablesAsync(ownerDefinition.RestaurantId, ownerDefinition.RestaurantName, nowUtc, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedNormalUsersAsync(CancellationToken cancellationToken)
    {
        var hashedUserPassword = _passwordHashService.HashPassword(NormalUserSeedPassword);
        var nowUtc = DateTime.UtcNow;

        // Generate 15 Active Normal Users
        for (int i = 1; i <= 15; i++)
        {
            var userGuid = Guid.Parse($"33333333-3333-3333-3333-{i:D12}");
            var phoneNumber = $"9647800000{i:D3}";

            var user = await _dbContext.UserAccounts
                .FirstOrDefaultAsync(u => u.Id == userGuid, cancellationToken);

            if (user == null)
            {
                user = new UserAccount
                {
                    Id = userGuid,
                    FullName = $"Customer {i}",
                    Username = $"customer{i}",
                    PhoneNumber = phoneNumber,
                    PasswordHash = hashedUserPassword,
                    Role = UserRole.User,
                    IsActive = true,
                    IsPhoneVerified = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };
                _dbContext.UserAccounts.Add(user);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoOrderHistoryAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Orders.AnyAsync(cancellationToken))
        {
            return; // Only seed history if no orders exist, preventing infinite spam loops
        }

        var rng = new Random(500);
        var restaurants = await _dbContext.Restaurants.Include(r => r.MenuItems).Include(r => r.Tables).ToListAsync(cancellationToken);
        var users = await _dbContext.UserAccounts.Where(u => u.Role == UserRole.User).ToListAsync(cancellationToken);

        var statuses = new[] { OrderStatus.OrderReceived, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Served };

        foreach (var user in users)
        {
            int orderCount = rng.Next(2, 6); // 2 to 5 orders per user

            for (int i = 0; i < orderCount; i++)
            {
                var restaurant = restaurants[rng.Next(restaurants.Count)];
                var table = restaurant.Tables.Count > 0 ? restaurant.Tables.ElementAt(rng.Next(restaurant.Tables.Count)) : null;

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RestaurantId = restaurant.Id,
                    RestaurantTableId = table?.Id ?? Guid.Empty,
                    Status = statuses[rng.Next(statuses.Length)],
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-rng.Next(1, 30)).AddHours(rng.Next(-5, 5)),
                    UpdatedAtUtc = DateTime.UtcNow
                };

                // Generate 2-4 items for each order organically finding menus from that specific location
                int itemCount = rng.Next(2, 5);
                for (int j = 0; j < itemCount && j < restaurant.MenuItems.Count; j++)
                {
                    var menuItem = restaurant.MenuItems.ElementAt(rng.Next(restaurant.MenuItems.Count));
                    int quantity = rng.Next(1, 4);

                    order.OrderItems.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        MenuItemId = menuItem.Id,
                        Quantity = quantity,
                        UnitPrice = menuItem.Price
                    });
                }

                if (order.OrderItems.Any())
                {
                    _dbContext.Orders.Add(order);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRestaurantTablesAsync(
        Guid restaurantId,
        string restaurantName,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var rng = new Random(restaurantId.GetHashCode());
        int tableCount = rng.Next(5, 16); // Randomly 5-15 tables safely distributed

        for (var tableNumber = 1; tableNumber <= tableCount; tableNumber++)
        {
            var existingTable = await _dbContext.RestaurantTables
                .FirstOrDefaultAsync(
                    table => table.RestaurantId == restaurantId && table.TableNumber == tableNumber,
                    cancellationToken);

            var generatedToken = BuildSeedTableToken(restaurantId, restaurantName, tableNumber);

            if (existingTable is null)
            {
                existingTable = new RestaurantTable
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    TableNumber = tableNumber,
                    TableToken = generatedToken,
                    IsActive = true,
                    ImageUrl = "https://images.unsplash.com/photo-1595180058204-6484eb3b55db", // Mock table seating URL
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _dbContext.RestaurantTables.Add(existingTable);
            }
            else
            {
                existingTable.TableToken = string.IsNullOrWhiteSpace(existingTable.TableToken)
                    ? generatedToken
                    : existingTable.TableToken;
                existingTable.IsActive = true;
                existingTable.UpdatedAtUtc = nowUtc;
            }
        }
    }

    private static string BuildSeedTableToken(Guid restaurantId, string restaurantName, int tableNumber)
    {
        var prefix = new string(
            restaurantName
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .Take(12)
                .ToArray());

        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = "restaurant";
        }

        var restaurantKey = restaurantId.ToString("N")[..8];
        return $"{prefix}-{restaurantKey}-table-{tableNumber}";
    }
}
