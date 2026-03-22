using SpotifyAPI.Web;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Api.Services;

// == SpotifyService == //
public class SpotifyService : ISpotifyService
{
    private readonly ISpotifyClient _client;
    private const int AudioFeaturesBatchSize = 100; // Spotify API limit

    /// <summary>
    /// Constructor for dependency injection with a pre-configured ISpotifyClient.
    /// </summary>
    public SpotifyService(ISpotifyClient client)
    {
        _client = client;
    }

    public bool IsAuthenticated => _client != null;

    // == GetPlaylist == //
    public async Task<SpotifyPlaylist> GetPlaylistAsync(string playlistId)
    {
        var spotifyPlaylist = await _client.Playlists.Get(playlistId);
        return MapPlaylist(spotifyPlaylist);
    }

    // == SearchPlaylistByName == //
    public async Task<SpotifyPlaylist?> SearchPlaylistByNameAsync(string name)
    {
        var searchRequest = new SearchRequest(SearchRequest.Types.Playlist, name);
        var searchResponse = await _client.Search.Item(searchRequest);

        var firstMatch = searchResponse.Playlists?.Items?.FirstOrDefault();
        if (firstMatch is null)
            return null;

        // Fetch the full playlist with track details
        return await GetPlaylistAsync(firstMatch.Id!);
    }

    // == GetAudioFeatures == //
    public async Task<List<AudioFeatures>> GetAudioFeaturesAsync(List<string> trackIds)
    {
        var allFeatures = new List<AudioFeatures>();

        // Batch requests in chunks of 100 (Spotify API limit)
        for (int i = 0; i < trackIds.Count; i += AudioFeaturesBatchSize)
        {
            var batch = trackIds.Skip(i).Take(AudioFeaturesBatchSize).ToList();
            var request = new TracksAudioFeaturesRequest(batch);
            var response = await _client.Tracks.GetSeveralAudioFeatures(request);

            if (response.AudioFeatures != null)
            {
                allFeatures.AddRange(response.AudioFeatures
                    .Where(f => f != null)
                    .Select(MapAudioFeatures));
            }
        }

        return allFeatures;
    }

    // == SearchTrack == //
    public async Task<SpotifyTrack?> SearchTrackAsync(string trackName, string artist)
    {
        var query = $"track:{trackName} artist:{artist}";
        var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
        var searchResponse = await _client.Search.Item(searchRequest);

        var firstMatch = searchResponse.Tracks?.Items?.FirstOrDefault();
        if (firstMatch is null)
            return null;

        return MapTrack(firstMatch);
    }

    // == CreatePlaylist == //
    public async Task<string> CreatePlaylistAsync(string name, string description, List<string> trackUris)
    {
        var createRequest = new PlaylistCreateRequest(name) { Description = description, Public = false };
        var newPlaylist = await _client.Playlists.Create(createRequest);

        // Add tracks in batches of 100
        for (int i = 0; i < trackUris.Count; i += 100)
        {
            var batch = trackUris.Skip(i).Take(100).ToList();
            await _client.Playlists.AddPlaylistItems(newPlaylist.Id!, new PlaylistAddItemsRequest(batch));
        }

        return newPlaylist.Id!;
    }

    // == GetRecentlyPlayed == //
    public async Task<List<SpotifyTrack>> GetRecentlyPlayedAsync(int limit = 50)
    {
        var request = new PlayerRecentlyPlayedRequest { Limit = limit };
        var response = await _client.Player.GetRecentlyPlayed(request);

        return response.Items?
            .Where(item => item.Track is FullTrack)
            .Select(item => MapTrack((FullTrack)item.Track))
            .GroupBy(t => t.Id)        // Deduplicate by track ID
            .Select(g => g.First())
            .ToList() ?? new List<SpotifyTrack>();
    }

    // == GetTopTracks == //
    public async Task<List<SpotifyTrack>> GetTopTracksAsync(int limit = 50)
    {
        var request = new PersonalizationTopRequest { Limit = limit };
        var response = await _client.Personalization.GetTopTracks(request);

        return response.Items?
            .Select(MapTrack)
            .ToList() ?? new List<SpotifyTrack>();
    }

    // == GetCurrentUserId == //
    public async Task<string> GetCurrentUserIdAsync()
    {
        var profile = await _client.UserProfile.Current();
        return profile.Id;
    }

    // == Mapping Helpers == //

    private static SpotifyPlaylist MapPlaylist(FullPlaylist sp)
    {
        var tracks = sp.Items?.Items?
            .Where(t => t.Track is FullTrack)
            .Select(t => MapTrack((FullTrack)t.Track))
            .ToList() ?? new List<SpotifyTrack>();

        return new SpotifyPlaylist
        {
            Id = sp.Id ?? string.Empty,
            Name = sp.Name ?? string.Empty,
            Description = sp.Description ?? string.Empty,
            Owner = sp.Owner?.DisplayName ?? string.Empty,
            Tracks = tracks
        };
    }

    private static SpotifyTrack MapTrack(FullTrack ft)
    {
        return new SpotifyTrack
        {
            Id = ft.Id,
            Name = ft.Name,
            Artists = ft.Artists?.Select(a => a.Name).ToList() ?? new List<string>(),
            Album = ft.Album?.Name ?? string.Empty,
            DurationMs = ft.DurationMs,
            Uri = ft.Uri
        };
    }

    private static AudioFeatures MapAudioFeatures(TrackAudioFeatures taf)
    {
        return new AudioFeatures
        {
            TrackId = taf.Id,
            Danceability = taf.Danceability,
            Energy = taf.Energy,
            Tempo = taf.Tempo,
            Valence = taf.Valence,
            Acousticness = taf.Acousticness,
            Instrumentalness = taf.Instrumentalness,
            Speechiness = taf.Speechiness,
            Liveness = taf.Liveness
        };
    }
}
