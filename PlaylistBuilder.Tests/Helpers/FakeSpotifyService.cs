using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Helpers;

// == FakeSpotifyService == //
/// <summary>
/// In-memory fake for integration tests. Returns deterministic data without hitting Spotify.
/// </summary>
public class FakeSpotifyService : ISpotifyService
{
    public bool IsAuthenticated => true;

    public Task<SpotifyPlaylist> GetPlaylistAsync(string playlistId)
    {
        return Task.FromResult(TestData.CreatePlaylist(5));
    }

    public Task<SpotifyPlaylist?> SearchPlaylistByNameAsync(string name)
    {
        var playlist = TestData.CreatePlaylist(5);
        playlist.Name = name;
        return Task.FromResult<SpotifyPlaylist?>(playlist);
    }

    public Task<List<AudioFeatures>> GetAudioFeaturesAsync(List<string> trackIds)
    {
        return Task.FromResult(TestData.CreateAudioFeaturesList(trackIds.Count));
    }

    public Task<SpotifyTrack?> SearchTrackAsync(string trackName, string artist)
    {
        var track = TestData.CreateTrack($"found_{trackName}", trackName, artist);
        return Task.FromResult<SpotifyTrack?>(track);
    }

    public Task<string> CreatePlaylistAsync(string name, string description, List<string> trackUris)
    {
        return Task.FromResult("fake_playlist_id");
    }

    public Task<string> GetCurrentUserIdAsync()
    {
        return Task.FromResult("fakeuser");
    }
}
