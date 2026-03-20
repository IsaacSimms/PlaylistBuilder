using PlaylistBuilder.Core.Models;
using PlaylistBuilder.Core.DTOs.Requests;

namespace PlaylistBuilder.Tests.Helpers;

// == TestData Factory == //
public static class TestData
{
    public static SpotifyTrack CreateTrack(string id = "track1", string name = "Test Song", string artist = "Test Artist")
    {
        return new SpotifyTrack
        {
            Id = id,
            Name = name,
            Artists = new List<string> { artist },
            Album = "Test Album",
            DurationMs = 200000,
            Uri = $"spotify:track:{id}"
        };
    }

    public static SpotifyPlaylist CreatePlaylist(int trackCount = 3)
    {
        var tracks = Enumerable.Range(1, trackCount)
            .Select(i => CreateTrack($"track{i}", $"Song {i}", $"Artist {i}"))
            .ToList();

        return new SpotifyPlaylist
        {
            Id = "playlist123",
            Name = "EDM Lo-Fi Mix",
            Description = "A chill mix of EDM and Lo-Fi beats",
            Owner = "testuser",
            Tracks = tracks
        };
    }

    public static AudioFeatures CreateAudioFeatures(string trackId = "track1")
    {
        return new AudioFeatures
        {
            TrackId = trackId,
            Danceability = 0.7f,
            Energy = 0.6f,
            Tempo = 120f,
            Valence = 0.5f,
            Acousticness = 0.3f,
            Instrumentalness = 0.4f,
            Speechiness = 0.05f,
            Liveness = 0.1f
        };
    }

    public static List<AudioFeatures> CreateAudioFeaturesList(int count = 3)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateAudioFeatures($"track{i}"))
            .ToList();
    }

    public static TrackRecommendation CreateRecommendation(string name = "Recommended Song", string artist = "Rec Artist")
    {
        return new TrackRecommendation
        {
            Name = name,
            Artist = artist,
            Reason = "Similar vibe and energy"
        };
    }

    public static List<TrackRecommendation> CreateRecommendations(int count = 5)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateRecommendation($"Rec Song {i}", $"Rec Artist {i}"))
            .ToList();
    }

    public static PlaylistMetadata CreateMetadata()
    {
        return new PlaylistMetadata
        {
            PlaylistName = "EDM Lo-Fi Mix",
            TrackCount = 3,
            GenreDistribution = new Dictionary<string, int> { { "edm", 2 }, { "lo-fi", 1 } },
            TrackNames = new List<string> { "Song 1", "Song 2", "Song 3" },
            AvgDanceability = 0.7f,
            AvgEnergy = 0.6f,
            AvgTempo = 120f,
            AvgValence = 0.5f,
            AvgAcousticness = 0.3f,
            AvgInstrumentalness = 0.4f
        };
    }

    public static AnalyzePlaylistRequest CreateAnalyzeRequest(string identifier = "EDM Lo-Fi Mix")
    {
        return new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = identifier,
            UserPrompt = "Make a playlist like EDM Lo-Fi Mix but with different songs",
            TrackCount = 20
        };
    }
}
