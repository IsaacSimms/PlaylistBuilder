namespace PlaylistBuilder.Core.DTOs.Requests;

// == AnalyzePlaylistRequest == //
public class AnalyzePlaylistRequest
{
    /// <summary>
    /// Spotify playlist URL or name to search for.
    /// </summary>
    public string PlaylistIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Natural-language prompt describing the desired playlist.
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Number of tracks to recommend. Defaults to 20.
    /// </summary>
    public int TrackCount { get; set; } = 20;

    /// <summary>
    /// Anthropic model ID to use for recommendations. Null uses the server default.
    /// </summary>
    public string? ModelId { get; set; }
}
