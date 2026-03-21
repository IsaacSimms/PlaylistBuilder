using Microsoft.AspNetCore.Mvc;

namespace PlaylistBuilder.Api.Controllers;

// == HealthController == //
[ApiController]
public class HealthController : ControllerBase
{
    // == Health Endpoint == //
    [HttpGet("/api/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
