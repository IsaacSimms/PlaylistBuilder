using FluentAssertions;
using Moq;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;
using PlaylistBuilder.Tests.Helpers;

namespace PlaylistBuilder.Tests.Unit.Services;

// == PlaylistOrchestrator Tests == //
public class PlaylistOrchestratorTests
{
    private readonly Mock<ISpotifyService> _spotifyServiceMock;
    private readonly Mock<IClaudeService> _claudeServiceMock;
    private readonly IPlaylistOrchestrator _orchestrator;

    public PlaylistOrchestratorTests()
    {
        _spotifyServiceMock = new Mock<ISpotifyService>();
        _claudeServiceMock = new Mock<IClaudeService>();

        // Will fail to compile until PlaylistOrchestrator is implemented
        _orchestrator = new Api.Services.PlaylistOrchestrator(
            _spotifyServiceMock.Object,
            _claudeServiceMock.Object);
    }

    // == Analyze Tests == //

    [Fact]
    public async Task AnalyzePlaylist_WithPlaylistName_SearchesAndFetchesPlaylist()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(recommendations);

        // Act
        var result = await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.PlaylistName.Should().Be("EDM Lo-Fi Mix");
        result.Recommendations.Should().HaveCount(5);
        result.NewPlaylistUrl.Should().BeNull(); // Analyze only, no creation
    }

    [Fact]
    public async Task AnalyzePlaylist_WithSpotifyUrl_ExtractsIdAndFetches()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("https://open.spotify.com/playlist/abc123?si=xyz");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.GetPlaylistAsync("abc123"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(recommendations);

        // Act
        var result = await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        _spotifyServiceMock.Verify(s => s.GetPlaylistAsync("abc123"), Times.Once);
        _spotifyServiceMock.Verify(s => s.SearchPlaylistByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzePlaylist_PassesExclusionList_ToClaudeService()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);

        List<string>? capturedExcludeList = null;
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback<PlaylistMetadata, string, List<string>, int, string?>((_, _, exclude, _, _) => capturedExcludeList = exclude)
            .ReturnsAsync(recommendations);

        // Act
        await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert - original track names should be in the exclusion list
        capturedExcludeList.Should().NotBeNull();
        capturedExcludeList.Should().Contain("Song 1");
        capturedExcludeList.Should().Contain("Song 2");
        capturedExcludeList.Should().Contain("Song 3");
    }

    [Fact]
    public async Task AnalyzePlaylist_WhenPlaylistNotFoundAndNoHistory_ReturnsError()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("Nonexistent Playlist");
        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("Nonexistent Playlist"))
            .ReturnsAsync((SpotifyPlaylist?)null);
        _spotifyServiceMock.Setup(s => s.GetRecentlyPlayedAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SpotifyTrack>());
        _spotifyServiceMock.Setup(s => s.GetTopTracksAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<SpotifyTrack>());

        // Act
        var result = await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("listening history");
    }

    [Fact]
    public async Task AnalyzePlaylist_CallsServicesInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync(It.IsAny<string>()))
            .Callback(() => callOrder.Add("SearchPlaylist"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .Callback(() => callOrder.Add("GetAudioFeatures"))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback(() => callOrder.Add("GetRecommendations"))
            .ReturnsAsync(recommendations);

        // Act
        await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert - services must be called in this specific order
        callOrder.Should().ContainInOrder("SearchPlaylist", "GetAudioFeatures", "GetRecommendations");
    }

    // == Build Tests == //

    [Fact]
    public async Task BuildPlaylist_CreatesNewPlaylistWithRecommendedTracks()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(3);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(recommendations);

        // Each recommendation resolves to a Spotify track
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 1", "Rec Artist 1"))
            .ReturnsAsync(TestData.CreateTrack("rec1", "Rec Song 1", "Rec Artist 1"));
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 2", "Rec Artist 2"))
            .ReturnsAsync(TestData.CreateTrack("rec2", "Rec Song 2", "Rec Artist 2"));
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 3", "Rec Artist 3"))
            .ReturnsAsync(TestData.CreateTrack("rec3", "Rec Song 3", "Rec Artist 3"));

        _spotifyServiceMock.Setup(s => s.CreatePlaylistAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync("newplaylist456");

        // Act
        var result = await _orchestrator.BuildPlaylistAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.NewPlaylistId.Should().Be("newplaylist456");
        result.NewPlaylistUrl.Should().Contain("newplaylist456");

        _spotifyServiceMock.Verify(s => s.CreatePlaylistAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.Is<List<string>>(uris => uris.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task BuildPlaylist_SkipsTracksNotFoundOnSpotify()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(3);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(recommendations);

        // First and third recommendations found
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 1", "Rec Artist 1"))
            .ReturnsAsync(TestData.CreateTrack("rec1", "Rec Song 1", "Rec Artist 1"));
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 2", "Rec Artist 2"))
            .ReturnsAsync((SpotifyTrack?)null);
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync("Rec Song 3", "Rec Artist 3"))
            .ReturnsAsync(TestData.CreateTrack("rec3", "Rec Song 3", "Rec Artist 3"));

        _spotifyServiceMock.Setup(s => s.CreatePlaylistAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync("newplaylist456");

        // Act
        var result = await _orchestrator.BuildPlaylistAsync(request);

        // Assert - only 2 tracks should be in the playlist (skipped the unfound one)
        result.Success.Should().BeTrue();
        _spotifyServiceMock.Verify(s => s.CreatePlaylistAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.Is<List<string>>(uris => uris.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task BuildPlaylist_WithNoRecommendations_ReturnsError()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(new List<TrackRecommendation>());

        // Act
        var result = await _orchestrator.BuildPlaylistAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no recommendations");
        _spotifyServiceMock.Verify(s => s.CreatePlaylistAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task BuildPlaylist_WhenAllSearchesFail_ReturnsError()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(3);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .ReturnsAsync(recommendations);

        // All searches return null
        _spotifyServiceMock.Setup(s => s.SearchTrackAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((SpotifyTrack?)null);

        // Act
        var result = await _orchestrator.BuildPlaylistAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("no tracks");
        _spotifyServiceMock.Verify(s => s.CreatePlaylistAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzePlaylist_BuildsCorrectMetadata_FromAudioFeatures()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        var playlist = TestData.CreatePlaylist(2);
        var audioFeatures = new List<AudioFeatures>
        {
            new() { TrackId = "track1", Danceability = 0.8f, Energy = 0.6f, Tempo = 120f, Valence = 0.5f, Acousticness = 0.2f, Instrumentalness = 0.3f },
            new() { TrackId = "track2", Danceability = 0.6f, Energy = 0.8f, Tempo = 140f, Valence = 0.7f, Acousticness = 0.4f, Instrumentalness = 0.5f }
        };
        var recommendations = TestData.CreateRecommendations(1);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);

        PlaylistMetadata? capturedMetadata = null;
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback<PlaylistMetadata, string, List<string>, int, string?>((meta, _, _, _, _) => capturedMetadata = meta)
            .ReturnsAsync(recommendations);

        // Act
        await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert - metadata should have averaged audio features
        capturedMetadata.Should().NotBeNull();
        capturedMetadata!.AvgDanceability.Should().BeApproximately(0.7f, 0.01f);
        capturedMetadata.AvgEnergy.Should().BeApproximately(0.7f, 0.01f);
        capturedMetadata.AvgTempo.Should().BeApproximately(130f, 0.01f);
        capturedMetadata.TrackCount.Should().Be(2);
    }

    // == Model Selection Tests == //

    [Fact]
    public async Task AnalyzePlaylist_WithModelId_PassesModelToClaudeService()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        request.ModelId = "claude-3-opus-20240229";

        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);

        string? capturedModelId = null;
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback<PlaylistMetadata, string, List<string>, int, string?>((_, _, _, _, model) => capturedModelId = model)
            .ReturnsAsync(recommendations);

        // Act
        await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert
        capturedModelId.Should().Be("claude-3-opus-20240229");
    }

    [Fact]
    public async Task AnalyzePlaylist_WithoutModelId_PassesNullToClaudeService()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");

        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(5);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);

        string? capturedModelId = "not-null-sentinel";
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback<PlaylistMetadata, string, List<string>, int, string?>((_, _, _, _, model) => capturedModelId = model)
            .ReturnsAsync(recommendations);

        // Act
        await _orchestrator.AnalyzePlaylistAsync(request);

        // Assert
        capturedModelId.Should().BeNull();
    }

    [Fact]
    public async Task BuildPlaylist_WithModelId_PassesModelThroughToClaudeService()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest("EDM Lo-Fi Mix");
        request.ModelId = "claude-3-5-haiku-20241022";

        var playlist = TestData.CreatePlaylist(3);
        var audioFeatures = TestData.CreateAudioFeaturesList(3);
        var recommendations = TestData.CreateRecommendations(3);

        _spotifyServiceMock.Setup(s => s.SearchPlaylistByNameAsync("EDM Lo-Fi Mix"))
            .ReturnsAsync(playlist);
        _spotifyServiceMock.Setup(s => s.GetAudioFeaturesAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(audioFeatures);

        string? capturedModelId = null;
        _claudeServiceMock.Setup(c => c.GetRecommendationsAsync(
                It.IsAny<PlaylistMetadata>(), It.IsAny<string>(), It.IsAny<List<string>>(), 20, It.IsAny<string?>()))
            .Callback<PlaylistMetadata, string, List<string>, int, string?>((_, _, _, _, model) => capturedModelId = model)
            .ReturnsAsync(recommendations);

        _spotifyServiceMock.Setup(s => s.SearchTrackAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(TestData.CreateTrack("rec1", "Rec Song 1", "Rec Artist 1"));
        _spotifyServiceMock.Setup(s => s.CreatePlaylistAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
            .ReturnsAsync("newplaylist789");

        // Act
        await _orchestrator.BuildPlaylistAsync(request);

        // Assert
        capturedModelId.Should().Be("claude-3-5-haiku-20241022");
    }
}
