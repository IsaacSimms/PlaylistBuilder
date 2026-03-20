namespace PlaylistBuilder.Api.Configuration;

// == SpotifySettings == //
public class SpotifySettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5263/api/spotify/auth/callback";
}
