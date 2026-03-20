namespace PlaylistBuilder.Core.DTOs.Requests;

// == CreatePlaylistRequest == //
public class CreatePlaylistRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> TrackUris { get; set; } = new();
}
