using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Options;
using PlaylistBuilder.Api.Configuration;
using PlaylistBuilder.Core;
using PlaylistBuilder.Core.Interfaces;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Api.Services;

// == ClaudeService == //
public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly AnthropicSettings _settings;
    private readonly ILogger<ClaudeService> _logger;

    // Matches the outermost JSON array in Claude's response text
    private static readonly Regex JsonArrayRegex = new(
        @"\[[\s\S]*\]",
        RegexOptions.Compiled);

    public ClaudeService(IOptions<AnthropicSettings> settings, ILogger<ClaudeService> logger)
    {
        _settings = settings.Value;
        _client = new AnthropicClient(_settings.ApiKey);
        _logger = logger;
    }

    public async Task<List<TrackRecommendation>> GetRecommendationsAsync(
        PlaylistMetadata metadata,
        string userPrompt,
        List<string> excludeTrackNames,
        int trackCount = 20,
        string? modelId = null)
    {
        // Resolve model: request → settings → hardcoded default
        var resolvedModel = SupportedModels.Resolve(modelId ?? _settings.ModelId);

        var prompt = BuildPrompt(metadata, userPrompt, excludeTrackNames, trackCount);

        var parameters = new MessageParameters
        {
            Messages = new List<Message>
            {
                new(RoleType.User, prompt)
            },
            MaxTokens = 4096,
            Model = resolvedModel,
            Stream = false,
            Temperature = 0.7m,
            System = new List<SystemMessage>
            {
                new("You are a music recommendation expert. You have deep knowledge of all music genres, artists, and songs. Always respond with valid JSON.")
            }
        };

        try
        {
            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            var responseText = response.Message.ToString();
            _logger.LogInformation("Claude response (first 500 chars): {Response}", responseText[..Math.Min(responseText.Length, 500)]);

            var recommendations = ParseRecommendations(responseText);
            _logger.LogInformation("Parsed {Count} recommendations from Claude response", recommendations.Count);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations from Claude. Model: {Model}", resolvedModel);
            throw; // Let the orchestrator handle the error with a meaningful message
        }
    }

    // == BuildPrompt == //
    public static string BuildPrompt(
        PlaylistMetadata metadata,
        string userPrompt,
        List<string> excludeTrackNames,
        int trackCount)
    {
        var excludeSection = excludeTrackNames.Count > 0
            ? string.Join("\n", excludeTrackNames.Select(t => $"- {t}"))
            : "(none)";

        return $$"""
            Analyze the following playlist and recommend {{trackCount}} songs that match its style.

            User's request: "{{userPrompt}}"

            Playlist: "{{metadata.PlaylistName}}"
            Track count: {{metadata.TrackCount}}

            Average audio features:
            - Danceability: {{metadata.AvgDanceability:F2}}
            - Energy: {{metadata.AvgEnergy:F2}}
            - Tempo: {{metadata.AvgTempo:F0}} BPM
            - Valence: {{metadata.AvgValence:F2}}
            - Acousticness: {{metadata.AvgAcousticness:F2}}
            - Instrumentalness: {{metadata.AvgInstrumentalness:F2}}

            Original tracks (DO NOT recommend any of these):
            {{excludeSection}}

            Respond ONLY with a JSON array of objects. Each object must have "name", "artist", and "reason" fields.
            Example: [{"name": "Song Title", "artist": "Artist Name", "reason": "Brief reason"}]
            """;
    }

    // == ParseRecommendations == //
    public static List<TrackRecommendation> ParseRecommendations(string responseText)
    {
        try
        {
            // Try to extract a JSON array from the response
            var match = JsonArrayRegex.Match(responseText);
            if (!match.Success)
                return new List<TrackRecommendation>();

            var jsonArray = match.Value;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var recommendations = JsonSerializer.Deserialize<List<TrackRecommendation>>(jsonArray, options);
            return recommendations ?? new List<TrackRecommendation>();
        }
        catch (JsonException)
        {
            return new List<TrackRecommendation>();
        }
    }
}
