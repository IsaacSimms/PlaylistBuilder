namespace PlaylistBuilder.Core.Models;

// == SupportedModel == //
/// <summary>
/// Represents an Anthropic model available for playlist recommendations.
/// </summary>
public record SupportedModel(string Id, string DisplayName, bool IsDefault);
