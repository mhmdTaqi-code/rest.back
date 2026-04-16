using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Orders;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/team10/orders")]
public class Team10OrdersController : ControllerBase
{
    private readonly ITeam10OrderTrackingService _team10OrderTrackingService;

    public Team10OrdersController(ITeam10OrderTrackingService team10OrderTrackingService)
    {
        _team10OrderTrackingService = team10OrderTrackingService;
    }

    [HttpGet("tracking")]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<Team10OrderTrackingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<Team10OrderTrackingListItemDto>>>> GetTrackingOrders(
        CancellationToken cancellationToken)
    {
        var orders = await _team10OrderTrackingService.GetActiveOrdersForTrackingAsync(cancellationToken);

        return Ok(new ApiSuccessResponseDto<IReadOnlyList<Team10OrderTrackingListItemDto>>
        {
            Message = "Active Team 10 tracking orders loaded successfully.",
            Data = orders
        });
    }
}
