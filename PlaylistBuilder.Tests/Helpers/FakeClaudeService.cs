using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Helpers;

// == FakeClaudeService == //
/// <summary>
/// In-memory fake for integration tests. Returns deterministic recommendations without hitting Anthropic.
/// </summary>
public class FakeClaudeService : IClaudeService
{
    public Task<List<TrackRecommendation>> GetRecommendationsAsync(
        PlaylistMetadata metadata,
        string userPrompt,
        List<string> excludeTrackNames,
        int trackCount = 20,
        string? modelId = null)
    {
        var recommendations = Enumerable.Range(1, trackCount)
            .Select(i => new TrackRecommendation
            {
                Name = $"Recommended Track {i}",
                Artist = $"Recommended Artist {i}",
                Reason = $"Matches the {metadata.PlaylistName} vibe"
            })
            .ToList();

        return Task.FromResult(recommendations);
    }
}
