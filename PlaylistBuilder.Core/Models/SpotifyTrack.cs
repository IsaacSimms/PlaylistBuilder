namespace PlaylistBuilder.Core.Models;

// == SpotifyTrack == //
public class SpotifyTrack
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Artists { get; set; } = new();
    public string Album { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public string Uri { get; set; } = string.Empty;
}
