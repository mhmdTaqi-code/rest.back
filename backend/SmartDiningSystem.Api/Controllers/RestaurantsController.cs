using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.DTOs.Restaurants;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantsController : ControllerBase
{
    private readonly IRestaurantQueryService _restaurantQueryService;

    public RestaurantsController(IRestaurantQueryService restaurantQueryService)
    {
        _restaurantQueryService = restaurantQueryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>>> GetPublicRestaurants(
        CancellationToken cancellationToken)
    {
        var restaurants = await _restaurantQueryService.GetPublicRestaurantsAsync(cancellationToken);
        return Ok(new ApiSuccessResponseDto<IReadOnlyList<PublicRestaurantSummaryDto>>
        {
            Message = "Approved restaurants loaded successfully.",
            Data = restaurants
        });
    }
}
