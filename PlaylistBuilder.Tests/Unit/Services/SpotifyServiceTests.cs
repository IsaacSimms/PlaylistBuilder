using FluentAssertions;
using Moq;
using SpotifyAPI.Web;
using PlaylistBuilder.Api.Services;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Unit.Services;

// == SpotifyService Tests == //
public class SpotifyServiceTests
{
    private readonly Mock<ISpotifyClient> _spotifyClientMock;
    private readonly SpotifyService _service;

    public SpotifyServiceTests()
    {
        _spotifyClientMock = new Mock<ISpotifyClient>();
        _service = new SpotifyService(_spotifyClientMock.Object);
    }

    // == GetPlaylist Tests == //

    [Fact]
    public async Task GetPlaylist_MapsSpotifyResponseToModel()
    {
        // Arrange
        var spotifyPlaylist = new FullPlaylist
        {
            Id = "playlist123",
            Name = "Test Playlist",
            Description = "A test playlist",
            Owner = new PublicUser { DisplayName = "testuser" },
            Items = new Paging<PlaylistTrack<IPlayableItem>>
            {
                Items = new List<PlaylistTrack<IPlayableItem>>
                {
                    new()
                    {
                        Track = new FullTrack
                        {
                            Id = "track1",
                            Name = "Song 1",
                            Artists = new List<SimpleArtist> { new() { Name = "Artist 1" } },
                            Album = new SimpleAlbum { Name = "Album 1" },
                            DurationMs = 200000,
                            Uri = "spotify:track:track1"
                        }
                    }
                }
            }
        };

        _spotifyClientMock.Setup(c => c.Playlists.Get("playlist123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(spotifyPlaylist);

        // Act
        var result = await _service.GetPlaylistAsync("playlist123");

        // Assert
        result.Id.Should().Be("playlist123");
        result.Name.Should().Be("Test Playlist");
        result.Owner.Should().Be("testuser");
        result.Tracks.Should().HaveCount(1);
        result.Tracks[0].Id.Should().Be("track1");
        result.Tracks[0].Name.Should().Be("Song 1");
        result.Tracks[0].Artists.Should().Contain("Artist 1");
    }

    // == SearchTrack Tests == //

    [Fact]
    public async Task SearchTrack_ReturnsFirstMatch()
    {
        // Arrange
        var searchResponse = new SearchResponse
        {
            Tracks = new Paging<FullTrack, SearchResponse>
            {
                Items = new List<FullTrack>
                {
                    new()
                    {
                        Id = "found1",
                        Name = "Midnight City",
                        Artists = new List<SimpleArtist> { new() { Name = "M83" } },
                        Album = new SimpleAlbum { Name = "Hurry Up, We're Dreaming" },
                        DurationMs = 243000,
                        Uri = "spotify:track:found1"
                    }
                }
            }
        };

        _spotifyClientMock.Setup(c => c.Search.Item(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        // Act
        var result = await _service.SearchTrackAsync("Midnight City", "M83");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("found1");
        result.Name.Should().Be("Midnight City");
    }

    [Fact]
    public async Task SearchTrack_WhenNoResults_ReturnsNull()
    {
        // Arrange
        var searchResponse = new SearchResponse
        {
            Tracks = new Paging<FullTrack, SearchResponse>
            {
                Items = new List<FullTrack>()
            }
        };

        _spotifyClientMock.Setup(c => c.Search.Item(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        // Act
        var result = await _service.SearchTrackAsync("Nonexistent Song", "Nobody");

        // Assert
        result.Should().BeNull();
    }

    // == GetAudioFeatures Tests == //

    [Fact]
    public async Task GetAudioFeatures_MapsResponseCorrectly()
    {
        // Arrange
        var response = new TracksAudioFeaturesResponse
        {
            AudioFeatures = new List<TrackAudioFeatures>
            {
                new()
                {
                    Id = "track1",
                    Danceability = 0.8f,
                    Energy = 0.6f,
                    Tempo = 120f,
                    Valence = 0.5f,
                    Acousticness = 0.3f,
                    Instrumentalness = 0.4f,
                    Speechiness = 0.05f,
                    Liveness = 0.1f
                }
            }
        };

        _spotifyClientMock.Setup(c => c.Tracks.GetSeveralAudioFeatures(
                It.IsAny<TracksAudioFeaturesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.GetAudioFeaturesAsync(new List<string> { "track1" });

        // Assert
        result.Should().HaveCount(1);
        result[0].TrackId.Should().Be("track1");
        result[0].Danceability.Should().Be(0.8f);
        result[0].Energy.Should().Be(0.6f);
        result[0].Tempo.Should().Be(120f);
    }

    [Fact]
    public async Task GetAudioFeatures_BatchesLargeRequests()
    {
        // Arrange - Spotify limits to 100 tracks per request
        var trackIds = Enumerable.Range(1, 150).Select(i => $"track{i}").ToList();

        var firstBatchResponse = new TracksAudioFeaturesResponse
        {
            AudioFeatures = Enumerable.Range(1, 100)
                .Select(i => new TrackAudioFeatures { Id = $"track{i}", Danceability = 0.5f })
                .ToList()
        };

        var secondBatchResponse = new TracksAudioFeaturesResponse
        {
            AudioFeatures = Enumerable.Range(101, 50)
                .Select(i => new TrackAudioFeatures { Id = $"track{i}", Danceability = 0.5f })
                .ToList()
        };

        var callCount = 0;
        _spotifyClientMock.Setup(c => c.Tracks.GetSeveralAudioFeatures(
                It.IsAny<TracksAudioFeaturesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? firstBatchResponse : secondBatchResponse;
            });

        // Act
        var result = await _service.GetAudioFeaturesAsync(trackIds);

        // Assert
        result.Should().HaveCount(150);
        _spotifyClientMock.Verify(c => c.Tracks.GetSeveralAudioFeatures(
            It.IsAny<TracksAudioFeaturesRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // == SearchPlaylistByName Tests == //

    [Fact]
    public async Task SearchPlaylistByName_ReturnsFirstMatch()
    {
        // Arrange - search returns FullPlaylist in Paging
        var searchResponse = new SearchResponse
        {
            Playlists = new Paging<FullPlaylist, SearchResponse>
            {
                Items = new List<FullPlaylist>
                {
                    new()
                    {
                        Id = "pl123",
                        Name = "EDM Lo-Fi Mix",
                        Description = "A mix",
                        Owner = new PublicUser { DisplayName = "creator" }
                    }
                }
            }
        };

        _spotifyClientMock.Setup(c => c.Search.Item(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        // Mock the full playlist fetch (GetPlaylistAsync called internally)
        var fullPlaylist = new FullPlaylist
        {
            Id = "pl123",
            Name = "EDM Lo-Fi Mix",
            Description = "A mix",
            Owner = new PublicUser { DisplayName = "creator" },
            Items = new Paging<PlaylistTrack<IPlayableItem>>
            {
                Items = new List<PlaylistTrack<IPlayableItem>>()
            }
        };

        _spotifyClientMock.Setup(c => c.Playlists.Get("pl123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fullPlaylist);

        // Act
        var result = await _service.SearchPlaylistByNameAsync("EDM Lo-Fi Mix");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("pl123");
        result.Name.Should().Be("EDM Lo-Fi Mix");
    }

    [Fact]
    public async Task SearchPlaylistByName_WhenNoResults_ReturnsNull()
    {
        // Arrange
        var searchResponse = new SearchResponse
        {
            Playlists = new Paging<FullPlaylist, SearchResponse>
            {
                Items = new List<FullPlaylist>()
            }
        };

        _spotifyClientMock.Setup(c => c.Search.Item(
                It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        // Act
        var result = await _service.SearchPlaylistByNameAsync("Nonexistent");

        // Assert
        result.Should().BeNull();
    }
}
