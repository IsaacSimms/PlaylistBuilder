namespace PlaylistBuilder.Api.Services;

// == SpotifyTokenStore == //
/// <summary>
/// In-memory store for the Spotify OAuth token. Singleton-scoped.
/// Single-user, localhost-only design -- no persistent storage needed.
/// </summary>
public class SpotifyTokenStore
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && DateTime.UtcNow < ExpiresAt;

    public void SetToken(string accessToken, string refreshToken, int expiresInSeconds)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        ExpiresAt = DateTime.MinValue;
    }
}
