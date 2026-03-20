using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;

namespace PlaylistBuilder.Core.Interfaces;

// == IPlaylistOrchestrator == //
public interface IPlaylistOrchestrator
{
    /// <summary>
    /// Analyzes a playlist and returns recommendations without creating a new playlist.
    /// </summary>
    Task<AnalyzePlaylistResponse> AnalyzePlaylistAsync(AnalyzePlaylistRequest request);

    /// <summary>
    /// Full workflow: analyze, recommend, search, and create a new Spotify playlist.
    /// </summary>
    Task<AnalyzePlaylistResponse> BuildPlaylistAsync(AnalyzePlaylistRequest request);
}
