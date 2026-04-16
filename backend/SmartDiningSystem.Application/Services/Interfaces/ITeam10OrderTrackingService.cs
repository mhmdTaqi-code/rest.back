using SmartDiningSystem.Application.DTOs.Orders;

namespace SmartDiningSystem.Application.Services.Interfaces;

public interface ITeam10OrderTrackingService
{
    Task<IReadOnlyList<Team10OrderTrackingListItemDto>> GetActiveOrdersForTrackingAsync(CancellationToken cancellationToken);
}
