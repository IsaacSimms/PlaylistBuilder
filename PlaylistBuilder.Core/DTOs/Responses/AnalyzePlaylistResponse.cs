using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Core.DTOs.Responses;

// == AnalyzePlaylistResponse == //
public class AnalyzePlaylistResponse
{
    public PlaylistMetadata? Metadata { get; set; }
    public List<TrackRecommendation> Recommendations { get; set; } = new();
    public string? NewPlaylistUrl { get; set; }
    public string? NewPlaylistId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
