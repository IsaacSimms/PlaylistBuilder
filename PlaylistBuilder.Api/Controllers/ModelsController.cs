using Microsoft.AspNetCore.Mvc;
using PlaylistBuilder.Core;

namespace PlaylistBuilder.Api.Controllers;

// == ModelsController == //
[ApiController]
public class ModelsController : ControllerBase
{
    // == List Available Models == //
    [HttpGet("/api/models")]
    public IActionResult GetModels()
    {
        return Ok(SupportedModels.All);
    }
}
