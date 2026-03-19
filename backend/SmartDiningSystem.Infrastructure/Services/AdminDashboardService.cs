using Microsoft.EntityFrameworkCore;
using SmartDiningSystem.Application.Areas.Admin.Models;
using SmartDiningSystem.Infrastructure.Data;
using SmartDiningSystem.Domain.Enums;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _dbContext;

    public AdminDashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var visibleAccounts = _dbContext.UserAccounts
            .AsNoTracking()
            .VisibleToAdminUi();

        var activeUsers = await visibleAccounts
            .CountAsync(
                user => user.Role == UserRole.User && user.IsActive,
                cancellationToken);

        var activeRestaurantOwners = await visibleAccounts
            .CountAsync(
                user => user.Role == UserRole.RestaurantOwner && user.IsActive,
                cancellationToken);

        var restaurantRecords = await _dbContext.Restaurants
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingRestaurantRecords = await _dbContext.Restaurants
            .AsNoTracking()
            .CountAsync(
                restaurant => restaurant.ApprovalStatus == RestaurantApprovalStatus.Pending,
                cancellationToken);

        return new AdminDashboardViewModel
        {
            ActiveUsers = activeUsers,
            ActiveRestaurantOwners = activeRestaurantOwners,
            RestaurantRecords = restaurantRecords,
            PendingRestaurantRecords = pendingRestaurantRecords
        };
    }
}
