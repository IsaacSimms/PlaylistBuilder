using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Core.Interfaces;

// == ISpotifyService == //
public interface ISpotifyService
{
    /// <summary>
    /// Fetches a playlist by its Spotify ID.
    /// </summary>
    Task<SpotifyPlaylist> GetPlaylistAsync(string playlistId);

    /// <summary>
    /// Searches for a playlist by name and returns the best match.
    /// </summary>
    Task<SpotifyPlaylist?> SearchPlaylistByNameAsync(string name);

    /// <summary>
    /// Fetches audio features for a batch of track IDs.
    /// </summary>
    Task<List<AudioFeatures>> GetAudioFeaturesAsync(List<string> trackIds);

    /// <summary>
    /// Searches for a track by name and artist, returns the best match.
    /// </summary>
    Task<SpotifyTrack?> SearchTrackAsync(string trackName, string artist);

    /// <summary>
    /// Creates a new playlist for the current user.
    /// </summary>
    Task<string> CreatePlaylistAsync(string name, string description, List<string> trackUris);

    /// <summary>
    /// Gets the current authenticated user's Spotify ID.
    /// </summary>
    Task<string> GetCurrentUserIdAsync();

    /// <summary>
    /// Fetches the user's recently played tracks.
    /// </summary>
    Task<List<SpotifyTrack>> GetRecentlyPlayedAsync(int limit = 50);

    /// <summary>
    /// Fetches the user's top tracks based on listening history.
    /// </summary>
    Task<List<SpotifyTrack>> GetTopTracksAsync(int limit = 50);

    /// <summary>
    /// Checks if the user is currently authenticated with Spotify.
    /// </summary>
    bool IsAuthenticated { get; }
}
