using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Core;

// <summary>
// Static catalog of Anthropic models available for use.
// Defines valid model IDs, display names, and the default selection.
// </summary>

// == SupportedModels == //
public static class SupportedModels
{
    public static readonly IReadOnlyList<SupportedModel> All = new List<SupportedModel>
    {
        new("claude-sonnet-4-6",            "Claude Sonnet 4.6 (Latest)", true),
        new("claude-haiku-4-5-20251001",    "Claude Haiku 4.5 (Fast)",    false),
    };

    // == Default Model == //
    public static string DefaultModelId => All.First(m => m.IsDefault).Id;

    // == Validation == //
    public static bool IsValid(string? modelId) =>
        !string.IsNullOrWhiteSpace(modelId) && All.Any(m => m.Id == modelId);

    // == Resolve == //
    /// <summary>
    /// Returns the given modelId if valid, otherwise the default.
    /// </summary>
    public static string Resolve(string? modelId) =>
        IsValid(modelId) ? modelId! : DefaultModelId;
}
