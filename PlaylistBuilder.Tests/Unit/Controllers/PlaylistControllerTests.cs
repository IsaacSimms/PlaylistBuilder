using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PlaylistBuilder.Api.Controllers;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;
using PlaylistBuilder.Tests.Helpers;

namespace PlaylistBuilder.Tests.Unit.Controllers;

// == PlaylistController Tests == //
public class PlaylistControllerTests
{
    private readonly Mock<IPlaylistOrchestrator> _orchestratorMock;
    private readonly PlaylistController _controller;

    public PlaylistControllerTests()
    {
        _orchestratorMock = new Mock<IPlaylistOrchestrator>();
        _controller = new PlaylistController(_orchestratorMock.Object);
    }

    [Fact]
    public async Task Build_WithValidRequest_Returns200WithResponse()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest();
        var response = new AnalyzePlaylistResponse
        {
            Success = true,
            Metadata = TestData.CreateMetadata(),
            Recommendations = TestData.CreateRecommendations(3),
            NewPlaylistId = "newpl123",
            NewPlaylistUrl = "https://open.spotify.com/playlist/newpl123"
        };

        _orchestratorMock.Setup(o => o.BuildPlaylistAsync(It.IsAny<AnalyzePlaylistRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Build(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = okResult.Value.Should().BeOfType<AnalyzePlaylistResponse>().Subject;
        body.Success.Should().BeTrue();
        body.NewPlaylistUrl.Should().Contain("newpl123");
    }

    [Fact]
    public async Task Build_WhenOrchestratorFails_Returns400()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest();
        var response = new AnalyzePlaylistResponse
        {
            Success = false,
            ErrorMessage = "Playlist not found"
        };

        _orchestratorMock.Setup(o => o.BuildPlaylistAsync(It.IsAny<AnalyzePlaylistRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Build(request);

        // Assert
        var badResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var body = badResult.Value.Should().BeOfType<AnalyzePlaylistResponse>().Subject;
        body.ErrorMessage.Should().Contain("not found");
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
        var result = await _controller.Build(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Build_WithEmptyPrompt_Returns400()
    {
        // Arrange
        var request = new AnalyzePlaylistRequest
        {
            PlaylistIdentifier = "EDM Lo-Fi Mix",
            UserPrompt = ""
        };

        // Act
        var result = await _controller.Build(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyze_WithValidRequest_Returns200()
    {
        // Arrange
        var request = TestData.CreateAnalyzeRequest();
        var response = new AnalyzePlaylistResponse
        {
            Success = true,
            Metadata = TestData.CreateMetadata(),
            Recommendations = TestData.CreateRecommendations(5)
        };

        _orchestratorMock.Setup(o => o.AnalyzePlaylistAsync(It.IsAny<AnalyzePlaylistRequest>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Analyze(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = okResult.Value.Should().BeOfType<AnalyzePlaylistResponse>().Subject;
        body.Success.Should().BeTrue();
        body.NewPlaylistUrl.Should().BeNull(); // Analyze only
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
