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

    private static readonly SeedOwnerDefinition[] MenuOwners =
    [
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Baghdad Grill Owner",
            "baghdadgrillowner",
            "9647700000101",
            Guid.Parse("21111111-1111-1111-1111-111111111111"),
            "Baghdad Grill House",
            "Classic Iraqi grill specialties and starters.",
            "Baghdad, Karrada",
            "9647700000201",
            new[]
            {
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111111"),
                    "Grills",
                    "Signature grilled dishes.",
                    1,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111111"),
                            "Iraqi Kebab",
                            "Charcoal grilled minced lamb kebab.",
                            12000m,
                            "https://images.unsplash.com/photo-1555939594-58d7cb561ad1",
                            1),
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111112"),
                            "Chicken Tikka",
                            "Marinated chicken tikka with spices.",
                            11000m,
                            "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398",
                            2)
                    }),
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111112"),
                    "Starters",
                    "Fresh starters and dips.",
                    2,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111113"),
                            "Hummus",
                            "Creamy hummus served with bread.",
                            5000m,
                            "https://images.unsplash.com/photo-1571197119282-7c4b5bdb8e45",
                            1)
                    })
            }),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111112"),
            "Tigris Pizza Owner",
            "tigrispizzaowner",
            "9647700000102",
            Guid.Parse("21111111-1111-1111-1111-111111111112"),
            "Tigris Pizza",
            "Pizza, sides, and casual comfort food.",
            "Baghdad, Mansour",
            "9647700000202",
            new[]
            {
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111121"),
                    "Pizzas",
                    "Stone-baked pizzas.",
                    1,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111121"),
                            "Margherita",
                            "Classic tomato, mozzarella, and basil.",
                            9000m,
                            "https://images.unsplash.com/photo-1513104890138-7c749659a591",
                            1),
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111122"),
                            "Pepperoni",
                            "Pepperoni pizza with mozzarella.",
                            10500m,
                            "https://images.unsplash.com/photo-1628840042765-356cda07504e",
                            2)
                    }),
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111122"),
                    "Sides",
                    "Light bites and sides.",
                    2,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111123"),
                            "Fries",
                            "Crispy seasoned french fries.",
                            3000m,
                            "https://images.unsplash.com/photo-1573080496219-bb080dd4f877",
                            1)
                    })
            }),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111113"),
            "Sweet Bite Owner",
            "sweetbiteowner",
            "9647700000103",
            Guid.Parse("21111111-1111-1111-1111-111111111113"),
            "Sweet Bite",
            "Desserts, coffee, and sweets.",
            "Baghdad, Jadriya",
            "9647700000203",
            new[]
            {
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111131"),
                    "Desserts",
                    "House desserts and bakery items.",
                    1,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111131"),
                            "Cheesecake",
                            "Creamy cheesecake slice.",
                            5500m,
                            "https://images.unsplash.com/photo-1533134242443-d4fd215305ad",
                            1),
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111132"),
                            "Brownie",
                            "Rich chocolate brownie.",
                            6500m,
                            "https://images.unsplash.com/photo-1606312619070-d48b4c652a52",
                            2)
                    }),
                new SeedCategoryDefinition(
                    Guid.Parse("31111111-1111-1111-1111-111111111132"),
                    "Drinks",
                    "Coffee and warm drinks.",
                    2,
                    new[]
                    {
                        new SeedMenuItemDefinition(
                            Guid.Parse("41111111-1111-1111-1111-111111111133"),
                            "Espresso",
                            "Single-shot espresso.",
                            2500m,
                            "https://images.unsplash.com/photo-1511920170033-f8396924c348",
                            1)
                    })
            })
    ];

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
        await SeedApprovedRestaurantMenusAsync(cancellationToken);
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

        foreach (var ownerDefinition in MenuOwners)
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

    private async Task SeedRestaurantTablesAsync(
        Guid restaurantId,
        string restaurantName,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        for (var tableNumber = 1; tableNumber <= 3; tableNumber++)
        {
            var existingTable = await _dbContext.RestaurantTables
                .FirstOrDefaultAsync(
                    table => table.RestaurantId == restaurantId && table.TableNumber == tableNumber,
                    cancellationToken);

            if (existingTable is null)
            {
                existingTable = new RestaurantTable
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    TableNumber = tableNumber,
                    TableToken = BuildSeedTableToken(restaurantId, restaurantName, tableNumber),
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                };

                _dbContext.RestaurantTables.Add(existingTable);
            }
            else
            {
                existingTable.TableToken = string.IsNullOrWhiteSpace(existingTable.TableToken)
                    ? BuildSeedTableToken(restaurantId, restaurantName, tableNumber)
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

    private sealed record SeedOwnerDefinition(
        Guid OwnerId,
        string OwnerName,
        string OwnerUsername,
        string OwnerPhoneNumber,
        Guid RestaurantId,
        string RestaurantName,
        string RestaurantDescription,
        string RestaurantAddress,
        string RestaurantPhoneNumber,
        IReadOnlyList<SeedCategoryDefinition> Categories);

    private sealed record SeedCategoryDefinition(
        Guid CategoryId,
        string Name,
        string? Description,
        int DisplayOrder,
        IReadOnlyList<SeedMenuItemDefinition> Items);

    private sealed record SeedMenuItemDefinition(
        Guid MenuItemId,
        string Name,
        string? Description,
        decimal Price,
        string ImageUrl,
        int DisplayOrder);
}
