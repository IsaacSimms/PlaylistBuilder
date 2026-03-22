using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Core.Interfaces;

// == IClaudeService == //
public interface IClaudeService
{
    /// <summary>
    /// Sends playlist metadata to Claude and returns song recommendations.
    /// </summary>
    /// <param name="metadata">Aggregated playlist metadata including audio features.</param>
    /// <param name="userPrompt">The user's natural-language request.</param>
    /// <param name="excludeTrackNames">Track names from the original playlist to exclude.</param>
    /// <param name="trackCount">Number of recommendations to request.</param>
    /// <param name="modelId">Anthropic model ID to use. Null falls back to server default.</param>
    Task<List<TrackRecommendation>> GetRecommendationsAsync(
        PlaylistMetadata metadata,
        string userPrompt,
        List<string> excludeTrackNames,
        int trackCount = 20,
        string? modelId = null);
}
