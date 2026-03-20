using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Tests.Helpers;

namespace PlaylistBuilder.Tests.Integration;

// == Playlist Endpoint Integration Tests == //
public class PlaylistEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PlaylistEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with fakes
                services.AddScoped<ISpotifyService, FakeSpotifyService>();
                services.AddScoped<IClaudeService, FakeClaudeService>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Analyze_WithValidRequest_Returns200WithRecommendations()
    {
        // Arrange
        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = "Make it more upbeat",
            TrackCount = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/playlist/analyze", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AnalyzePlaylistResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Recommendations.Should().HaveCount(5);
        body.NewPlaylistUrl.Should().BeNull(); // Analyze only
    }

    [Fact]
    public async Task Build_WithValidRequest_Returns200WithPlaylistUrl()
    {
        // Arrange
        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = "Similar vibe but different songs",
            TrackCount = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/playlist/build", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AnalyzePlaylistResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.NewPlaylistId.Should().NotBeNullOrEmpty();
        body.NewPlaylistUrl.Should().Contain("spotify.com/playlist");
    }

    [Fact]
    public async Task Build_WithEmptyIdentifier_Returns400()
    {
        // Arrange
        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "",
            UserPrompt = "test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/playlist/build", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Build_WithSpotifyUrl_Returns200()
    {
        // Arrange
        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "https://open.spotify.com/playlist/abc123",
            UserPrompt = "More upbeat version",
            TrackCount = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/playlist/build", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
