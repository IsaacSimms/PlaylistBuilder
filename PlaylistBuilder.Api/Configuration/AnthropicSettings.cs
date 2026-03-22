namespace PlaylistBuilder.Api.Configuration;

// == AnthropicSettings == //
public class AnthropicSettings
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default Anthropic model ID used when none is specified per-request.
    /// </summary>
    public string? ModelId { get; set; }
}
