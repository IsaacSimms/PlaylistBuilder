using FluentAssertions;
using Moq;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using PlaylistBuilder.Api.Services;
using PlaylistBuilder.Core.Models;
using PlaylistBuilder.Tests.Helpers;

namespace PlaylistBuilder.Tests.Unit.Services;

// == ClaudeService Tests == //
public class ClaudeServiceTests
{
    [Fact]
    public void BuildPrompt_IncludesPlaylistMetadata()
    {
        // Arrange
        var metadata = TestData.CreateMetadata();
        var userPrompt = "Make it more upbeat";
        var excludeList = new List<string> { "Song 1", "Song 2" };

        // Act
        var prompt = ClaudeService.BuildPrompt(metadata, userPrompt, excludeList, 20);

        // Assert
        prompt.Should().Contain("EDM Lo-Fi Mix");               // playlist name
        prompt.Should().Contain("Make it more upbeat");          // user prompt
        prompt.Should().Contain("Song 1");                       // exclusion list
        prompt.Should().Contain("Song 2");
        prompt.Should().Contain("0.7");                          // danceability
        prompt.Should().Contain("120");                          // tempo
        prompt.Should().Contain("20");                           // track count
    }

    [Fact]
    public void BuildPrompt_RequestsJsonFormat()
    {
        // Arrange
        var metadata = TestData.CreateMetadata();

        // Act
        var prompt = ClaudeService.BuildPrompt(metadata, "test", new List<string>(), 20);

        // Assert
        prompt.Should().Contain("JSON");
        prompt.Should().Contain("name");
        prompt.Should().Contain("artist");
        prompt.Should().Contain("reason");
    }

    [Fact]
    public void ParseRecommendations_ValidJson_ReturnsList()
    {
        // Arrange
        var json = """
            [
                {"name": "Midnight City", "artist": "M83", "reason": "Similar synth-driven energy"},
                {"name": "Intro", "artist": "The xx", "reason": "Matching lo-fi atmosphere"}
            ]
            """;

        // Act
        var result = ClaudeService.ParseRecommendations(json);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Midnight City");
        result[0].Artist.Should().Be("M83");
        result[0].Reason.Should().Be("Similar synth-driven energy");
        result[1].Name.Should().Be("Intro");
        result[1].Artist.Should().Be("The xx");
    }

    [Fact]
    public void ParseRecommendations_JsonWithSurroundingText_ExtractsArray()
    {
        // Claude sometimes wraps JSON in explanation text
        var response = """
            Here are my recommendations:

            [
                {"name": "Midnight City", "artist": "M83", "reason": "Great match"}
            ]

            These songs should work well!
            """;

        // Act
        var result = ClaudeService.ParseRecommendations(response);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Midnight City");
    }

    [Fact]
    public void ParseRecommendations_MalformedJson_ReturnsEmptyList()
    {
        // Arrange
        var badJson = "This is not JSON at all";

        // Act
        var result = ClaudeService.ParseRecommendations(badJson);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseRecommendations_EmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = ClaudeService.ParseRecommendations(json);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseRecommendations_MissingFields_HandlesGracefully()
    {
        // Arrange - missing "reason" field
        var json = """
            [
                {"name": "Midnight City", "artist": "M83"}
            ]
            """;

        // Act
        var result = ClaudeService.ParseRecommendations(json);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Midnight City");
        result[0].Reason.Should().BeEmpty();
    }

    [Fact]
    public void BuildPrompt_IncludesAllExcludedTracks()
    {
        // Arrange
        var metadata = TestData.CreateMetadata();
        var excludeList = new List<string> { "Track A", "Track B", "Track C", "Track D" };

        // Act
        var prompt = ClaudeService.BuildPrompt(metadata, "test", excludeList, 10);

        // Assert
        foreach (var track in excludeList)
        {
            prompt.Should().Contain(track);
        }
    }
}
