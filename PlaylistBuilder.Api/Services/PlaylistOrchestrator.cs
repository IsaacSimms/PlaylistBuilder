using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;
using System.Text.RegularExpressions;

namespace PlaylistBuilder.Api.Services;

// == PlaylistOrchestrator == //
public class PlaylistOrchestrator : IPlaylistOrchestrator
{
    private readonly ISpotifyService _spotifyService;
    private readonly IClaudeService _claudeService;
    private readonly ILogger<PlaylistOrchestrator> _logger;

    // Matches Spotify playlist URLs like https://open.spotify.com/playlist/abc123?si=xyz
    private static readonly Regex SpotifyUrlRegex = new(
        @"https?://open\.spotify\.com/playlist/([a-zA-Z0-9]+)",
        RegexOptions.Compiled);

    public PlaylistOrchestrator(ISpotifyService spotifyService, IClaudeService claudeService, ILogger<PlaylistOrchestrator> logger)
    {
        _spotifyService = spotifyService;
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<AnalyzePlaylistResponse> AnalyzePlaylistAsync(AnalyzePlaylistRequest request)
    {
        List<SpotifyTrack> tracks;
        string sourceName;

        // Step 1: Try to resolve a playlist, fall back to listening history
        try
        {
            var playlist = await ResolvePlaylistAsync(request.PlaylistIdentifier);
            if (playlist is not null && playlist.Tracks.Count > 0)
            {
                tracks = playlist.Tracks;
                sourceName = playlist.Name;
            }
            else
            {
                // No playlist found — use the user's listening history instead
                tracks = await GetListeningHistoryAsync();
                sourceName = "Your Listening History";

                if (tracks.Count == 0)
                {
                    return new AnalyzePlaylistResponse
                    {
                        Success = false,
                        ErrorMessage = "Could not find a matching playlist or any listening history on your Spotify account."
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve playlist or fetch listening history from Spotify");
            return new AnalyzePlaylistResponse
            {
                Success = false,
                ErrorMessage = $"Spotify API error while fetching playlist: {ex.Message}"
            };
        }

        // Step 2: Get audio features (optional — Spotify deprecated this for newer apps)
        var audioFeatures = new List<AudioFeatures>();
        try
        {
            var trackIds = tracks.Select(t => t.Id).ToList();
            audioFeatures = await _spotifyService.GetAudioFeaturesAsync(trackIds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch audio features — continuing without them");
        }

        // Step 3: Build metadata from tracks + audio features
        var metadata = BuildMetadataFromTracks(sourceName, tracks, audioFeatures);

        // Step 4: Get recommendations from Claude
        var excludeList = tracks.Select(t => t.Name).ToList();
        try
        {
            var recommendations = await _claudeService.GetRecommendationsAsync(
                metadata, request.UserPrompt, excludeList, request.TrackCount, request.ModelId);

            if (recommendations.Count == 0)
            {
                return new AnalyzePlaylistResponse
                {
                    Success = false,
                    Metadata = metadata,
                    ErrorMessage = "Claude returned a response but no song recommendations could be parsed from it."
                };
            }

            return new AnalyzePlaylistResponse
            {
                Success = true,
                Metadata = metadata,
                Recommendations = recommendations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed");
            return new AnalyzePlaylistResponse
            {
                Success = false,
                Metadata = metadata,
                ErrorMessage = $"Claude API error: {ex.Message}"
            };
        }
    }

    public async Task<AnalyzePlaylistResponse> BuildPlaylistAsync(AnalyzePlaylistRequest request)
    {
        // Analyze first
        var analyzeResult = await AnalyzePlaylistAsync(request);
        if (!analyzeResult.Success)
            return analyzeResult;

        if (analyzeResult.Recommendations.Count == 0)
        {
            return new AnalyzePlaylistResponse
            {
                Success = false,
                Metadata = analyzeResult.Metadata,
                ErrorMessage = "Claude returned no recommendations for this playlist."
            };
        }

        // Search Spotify for each recommended track
        var trackUris = new List<string>();
        foreach (var rec in analyzeResult.Recommendations)
        {
            try
            {
                var track = await _spotifyService.SearchTrackAsync(rec.Name, rec.Artist);
                if (track is not null)
                    trackUris.Add(track.Uri);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search for track: {Name} by {Artist}", rec.Name, rec.Artist);
            }
        }

        if (trackUris.Count == 0)
        {
            return new AnalyzePlaylistResponse
            {
                Success = false,
                Metadata = analyzeResult.Metadata,
                Recommendations = analyzeResult.Recommendations,
                ErrorMessage = "Could not find any of the recommended tracks on Spotify (no tracks matched)."
            };
        }

        // Create the new playlist
        try
        {
            var playlistName = $"{analyzeResult.Metadata!.PlaylistName} - AI Remix";
            var description = $"Generated by PlaylistBuilder based on '{analyzeResult.Metadata.PlaylistName}'";
            var newPlaylistId = await _spotifyService.CreatePlaylistAsync(playlistName, description, trackUris);

            return new AnalyzePlaylistResponse
            {
                Success = true,
                Metadata = analyzeResult.Metadata,
                Recommendations = analyzeResult.Recommendations,
                NewPlaylistId = newPlaylistId,
                NewPlaylistUrl = $"https://open.spotify.com/playlist/{newPlaylistId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create playlist on Spotify");
            return new AnalyzePlaylistResponse
            {
                Success = false,
                Metadata = analyzeResult.Metadata,
                Recommendations = analyzeResult.Recommendations,
                ErrorMessage = $"Failed to create playlist on Spotify: {ex.Message}"
            };
        }
    }

    // == ResolvePlaylist == //
    private async Task<SpotifyPlaylist?> ResolvePlaylistAsync(string identifier)
    {
        var match = SpotifyUrlRegex.Match(identifier);
        if (match.Success)
        {
            var playlistId = match.Groups[1].Value;
            return await _spotifyService.GetPlaylistAsync(playlistId);
        }

        return await _spotifyService.SearchPlaylistByNameAsync(identifier);
    }

    // == GetListeningHistory == //
    private async Task<List<SpotifyTrack>> GetListeningHistoryAsync()
    {
        // Combine recently played and top tracks for richer context
        var recentlyPlayed = await _spotifyService.GetRecentlyPlayedAsync(50);
        var topTracks = await _spotifyService.GetTopTracksAsync(50);

        // Merge and deduplicate, preferring top tracks first
        var seen = new HashSet<string>();
        var combined = new List<SpotifyTrack>();

        foreach (var track in topTracks.Concat(recentlyPlayed))
        {
            if (seen.Add(track.Id))
                combined.Add(track);
        }

        return combined;
    }

    // == BuildMetadata (from playlist) == //
    private static PlaylistMetadata BuildMetadata(SpotifyPlaylist playlist, List<AudioFeatures> audioFeatures)
    {
        return BuildMetadataFromTracks(playlist.Name, playlist.Tracks, audioFeatures);
    }

    // == BuildMetadata (from track list) == //
    private static PlaylistMetadata BuildMetadataFromTracks(string name, List<SpotifyTrack> tracks, List<AudioFeatures> audioFeatures)
    {
        var metadata = new PlaylistMetadata
        {
            PlaylistName = name,
            TrackCount = tracks.Count,
            TrackNames = tracks.Select(t => $"{t.Name} by {string.Join(", ", t.Artists)}").ToList()
        };

        if (audioFeatures.Count > 0)
        {
            metadata.AvgDanceability = audioFeatures.Average(f => f.Danceability);
            metadata.AvgEnergy = audioFeatures.Average(f => f.Energy);
            metadata.AvgTempo = audioFeatures.Average(f => f.Tempo);
            metadata.AvgValence = audioFeatures.Average(f => f.Valence);
            metadata.AvgAcousticness = audioFeatures.Average(f => f.Acousticness);
            metadata.AvgInstrumentalness = audioFeatures.Average(f => f.Instrumentalness);
        }

        return metadata;
    }
}
