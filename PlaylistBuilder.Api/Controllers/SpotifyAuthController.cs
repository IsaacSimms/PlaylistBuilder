using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PlaylistBuilder.Api.Configuration;
using PlaylistBuilder.Api.Services;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace PlaylistBuilder.Api.Controllers;

// == SpotifyAuthController == //
[ApiController]
[Route("api/spotify/auth")]
public class SpotifyAuthController : ControllerBase
{
    private readonly SpotifySettings _settings;
    private readonly SpotifyTokenStore _tokenStore;

    // Scopes needed: read playlists + modify playlists + listening history
    private static readonly string[] Scopes = new[]
    {
        "playlist-read-private",
        "playlist-read-collaborative",
        "playlist-modify-public",
        "playlist-modify-private",
        "user-read-private",
        "user-read-recently-played",
        "user-top-read"
    };

    public SpotifyAuthController(IOptions<SpotifySettings> settings, SpotifyTokenStore tokenStore)
    {
        _settings = settings.Value;
        _tokenStore = tokenStore;
    }

    // == Get Authorization URL == //
    [HttpGet("url")]
    public IActionResult GetAuthUrl()
    {
        var loginRequest = new LoginRequest(
            new Uri(_settings.RedirectUri),
            _settings.ClientId,
            LoginRequest.ResponseType.Code)
        {
            Scope = Scopes
        };

        return Ok(new { url = loginRequest.ToUri().ToString() });
    }

    // == OAuth Callback == //
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest(new { error = $"Spotify authorization failed: {error}" });

        if (string.IsNullOrEmpty(code))
            return BadRequest(new { error = "Authorization code is missing." });

        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                _settings.ClientId,
                _settings.ClientSecret,
                code,
                new Uri(_settings.RedirectUri)));

        _tokenStore.SetToken(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn);

        return Ok(new { message = "Authentication successful! You can close this window and return to the CLI." });
    }

    // == Auth Status == //
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { isAuthenticated = _tokenStore.IsAuthenticated });
    }
}
