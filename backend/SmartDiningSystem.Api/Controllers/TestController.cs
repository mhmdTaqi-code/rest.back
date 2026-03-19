using Microsoft.AspNetCore.Mvc;

namespace SmartDiningSystem.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "SmartDiningSystem API is running."
        });
    }
}
