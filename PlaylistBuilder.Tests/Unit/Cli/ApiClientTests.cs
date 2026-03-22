using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlaylistBuilder.Cli;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Tests.Unit.Cli;

// == ApiClient Tests == //
public class ApiClientTests
{
    // == Mock HTTP Handler == //
    private class MockHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private static ApiClient CreateClientWithHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var httpClient = new HttpClient(new MockHttpHandler(handler))
        {
            BaseAddress = new Uri("http://localhost:5263")
        };
        return new ApiClient(httpClient);
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiResponds_ReturnsTrue()
    {
        // Arrange
        var client = CreateClientWithHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/health");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Act & Assert
        (await client.IsHealthyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WhenApiDown_ReturnsFalse()
    {
        // Arrange
        var client = CreateClientWithHandler(_ => throw new HttpRequestException("Connection refused"));

        // Act & Assert
        (await client.IsHealthyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var client = CreateClientWithHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/spotify/auth/status");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { isAuthenticated = true })
            };
        });

        // Act & Assert
        (await client.IsAuthenticatedAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task IsAuthenticatedAsync_WhenNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var client = CreateClientWithHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { isAuthenticated = false })
            });

        // Act & Assert
        (await client.IsAuthenticatedAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task BuildAsync_SendsCorrectRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClientWithHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { success = true, newPlaylistId = "test123" })
            };
        });

        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = "Make it more upbeat",
            TrackCount = 20
        };

        // Act
        await client.BuildAsync(request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.PathAndQuery.Should().Be("/api/playlist/build");

        var body = await capturedRequest.Content!.ReadFromJsonAsync<AnalyzePlaylistRequest>();
        body!.PlaylistIdentifier.Should().Be("EDM Lo-Fi Mix");
        body.UserPrompt.Should().Be("Make it more upbeat");
        body.TrackCount.Should().Be(20);
    }

    [Fact]
    public async Task AnalyzeAsync_SendsCorrectRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClientWithHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { success = true })
            };
        });

        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "https://open.spotify.com/playlist/abc123",
            UserPrompt = "test",
            TrackCount = 10
        };

        // Act
        await client.AnalyzeAsync(request);

        // Assert
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/api/playlist/analyze");
    }

    // == Model Selection Tests == //

    [Fact]
    public async Task AnalyzeAsync_WithModelId_SendsModelInRequestBody()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClientWithHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { success = true })
            };
        });

        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = "Make it more upbeat",
            TrackCount = 20,
            ModelId = "claude-3-opus-20240229"
        };

        // Act
        await client.AnalyzeAsync(request);

        // Assert
        var body = await capturedRequest!.Content!.ReadFromJsonAsync<AnalyzePlaylistRequest>();
        body!.ModelId.Should().Be("claude-3-opus-20240229");
    }

    [Fact]
    public async Task BuildAsync_WithModelId_SendsModelInRequestBody()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClientWithHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { success = true, newPlaylistId = "test123" })
            };
        });

        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = "Make it chill",
            TrackCount = 15,
            ModelId = "claude-3-5-haiku-20241022"
        };

        // Act
        await client.BuildAsync(request);

        // Assert
        var body = await capturedRequest!.Content!.ReadFromJsonAsync<AnalyzePlaylistRequest>();
        body!.ModelId.Should().Be("claude-3-5-haiku-20241022");
    }

    [Fact]
    public async Task GetModelsAsync_ReturnsModelList()
    {
        // Arrange
        var models = new[]
        {
            new { id = "claude-sonnet-4-6", displayName = "Claude Sonnet 4 (Latest)", isDefault = true },
            new { id = "claude-3-5-haiku-20241022", displayName = "Claude 3.5 Haiku (Fast)", isDefault = false }
        };

        var client = CreateClientWithHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/models");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(models)
            };
        });

        // Act
        var result = await client.GetModelsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![0].Id.Should().Be("claude-sonnet-4-6");
        result[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetModelsAsync_WhenApiFails_ReturnsNull()
    {
        // Arrange
        var client = CreateClientWithHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await client.GetModelsAsync();

        // Assert
        result.Should().BeNull();
    }
}
