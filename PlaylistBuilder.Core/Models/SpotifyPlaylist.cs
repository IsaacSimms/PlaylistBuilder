namespace PlaylistBuilder.Core.Models;

// == SpotifyPlaylist == //
public class SpotifyPlaylist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public List<SpotifyTrack> Tracks { get; set; } = new();
}
