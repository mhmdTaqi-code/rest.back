using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDiningSystem.Application.DTOs.Common;
using SmartDiningSystem.Application.Services.Interfaces;

namespace SmartDiningSystem.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/testing")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,MainAdmin")]
public class AdminTestingController : ControllerBase
{
    private readonly IAdminRestaurantService _adminRestaurantService;

    public AdminTestingController(IAdminRestaurantService adminRestaurantService)
    {
        _adminRestaurantService = adminRestaurantService;
    }

    [HttpPost("reset-all-tables-globally")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAllTablesGlobally(CancellationToken cancellationToken)
    {
        await _adminRestaurantService.ResetAllTablesGloballyAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "All tables across all restaurants have been reset and are available for ordering.",
            data = (object?)null,
            errors = (object?)null
        });
    }
}
