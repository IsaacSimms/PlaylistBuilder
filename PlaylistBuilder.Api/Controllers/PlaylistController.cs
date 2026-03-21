using Microsoft.AspNetCore.Mvc;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;
using PlaylistBuilder.Core.Interfaces;

namespace PlaylistBuilder.Api.Controllers;

// == PlaylistController == //
[ApiController]
[Route("api/[controller]")]
public class PlaylistController : ControllerBase
{
    private readonly IPlaylistOrchestrator _orchestrator;

    public PlaylistController(IPlaylistOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    // == Build Endpoint == //
    [HttpPost("build")]
    public async Task<ActionResult<AnalyzePlaylistResponse>> Build([FromBody] AnalyzePlaylistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlaylistIdentifier))
            return BadRequest(new AnalyzePlaylistResponse { Success = false, ErrorMessage = "PlaylistIdentifier is required." });

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            return BadRequest(new AnalyzePlaylistResponse { Success = false, ErrorMessage = "UserPrompt is required." });

        var result = await _orchestrator.BuildPlaylistAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // == Analyze Endpoint == //
    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzePlaylistResponse>> Analyze([FromBody] AnalyzePlaylistRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlaylistIdentifier))
            return BadRequest(new AnalyzePlaylistResponse { Success = false, ErrorMessage = "PlaylistIdentifier is required." });

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
            return BadRequest(new AnalyzePlaylistResponse { Success = false, ErrorMessage = "UserPrompt is required." });

        var result = await _orchestrator.AnalyzePlaylistAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
