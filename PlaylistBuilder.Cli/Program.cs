using PlaylistBuilder.Cli;
using PlaylistBuilder.Core.DTOs.Requests;

// == PlaylistBuilder CLI Entry Point == //

if (args.Length == 0)
{
    ConsoleHelper.WriteHeader("PlaylistBuilder CLI");
    ConsoleHelper.WriteInfo("Usage: playlistbuilder <prompt>");
    ConsoleHelper.WriteInfo("Example: playlistbuilder \"Make a playlist like EDM Lo-Fi Mix but with different songs\"");
    return 1;
}

var userPrompt = string.Join(" ", args);
var apiClient = new ApiClient();

// Step 1: Check API health
ConsoleHelper.WriteInfo("Connecting to PlaylistBuilder API...");
if (!await apiClient.IsHealthyAsync())
{
    ConsoleHelper.WriteError("Cannot connect to the API. Make sure it's running on https://localhost:7055");
    ConsoleHelper.WriteInfo("Start it with: dotnet run --project PlaylistBuilder.Api");
    return 1;
}

// Step 2: Check Spotify authentication
ConsoleHelper.WriteInfo("Checking Spotify authentication...");
if (!await apiClient.IsAuthenticatedAsync())
{
    ConsoleHelper.WriteWarning("Not authenticated with Spotify. Starting OAuth flow...");
    var authUrl = await apiClient.GetAuthUrlAsync();

    if (authUrl == null)
    {
        ConsoleHelper.WriteError("Could not get Spotify authorization URL. Check API configuration.");
        return 1;
    }

    ConsoleHelper.WriteInfo($"Open this URL in your browser to authorize:\n");
    ConsoleHelper.WriteSuccess(authUrl);
    ConsoleHelper.WriteInfo("\nWaiting for authentication...");

    // Poll for auth status
    while (!await apiClient.IsAuthenticatedAsync())
    {
        await Task.Delay(2000);
    }

    ConsoleHelper.WriteSuccess("Authenticated with Spotify!");
}

// Step 3: Parse playlist identifier from the prompt
// The API/Claude will handle extracting the playlist name from the natural language prompt
var request = new AnalyzePlaylistRequest
{
    PlaylistIdentifier = ExtractPlaylistIdentifier(userPrompt),
    UserPrompt = userPrompt,
    TrackCount = 20
};

// Step 4: Analyze the playlist
ConsoleHelper.WriteInfo($"\nAnalyzing playlist: \"{request.PlaylistIdentifier}\"...");
var analyzeResult = await apiClient.AnalyzeAsync(request);

if (analyzeResult == null || !analyzeResult.Success)
{
    ConsoleHelper.WriteError(analyzeResult?.ErrorMessage ?? "Failed to analyze playlist.");
    return 1;
}

ConsoleHelper.DisplayRecommendations(analyzeResult);

// Step 5: Ask to create playlist
if (!ConsoleHelper.AskConfirmation("Create this playlist on Spotify?"))
{
    ConsoleHelper.WriteInfo("Cancelled. No playlist was created.");
    return 0;
}

// Step 6: Build the playlist
ConsoleHelper.WriteInfo("\nCreating playlist on Spotify...");
var buildResult = await apiClient.BuildAsync(request);

if (buildResult == null || !buildResult.Success)
{
    ConsoleHelper.WriteError(buildResult?.ErrorMessage ?? "Failed to create playlist.");
    return 1;
}

ConsoleHelper.DisplayResult(buildResult);
return 0;

// == Helper: Extract Playlist Identifier == //
static string ExtractPlaylistIdentifier(string prompt)
{
    // Check for Spotify URL in the prompt
    var urlMatch = System.Text.RegularExpressions.Regex.Match(
        prompt, @"https?://open\.spotify\.com/playlist/[a-zA-Z0-9]+(\?[^\s]*)?");
    if (urlMatch.Success)
        return urlMatch.Value;

    // Check for common patterns like "like X" or "similar to X"
    var likeMatch = System.Text.RegularExpressions.Regex.Match(
        prompt, @"(?:like|similar to|based on|inspired by)\s+[""']?(.+?)[""']?(?:\s+but|\s+with|\s+however|$)",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (likeMatch.Success)
        return likeMatch.Groups[1].Value.Trim().Trim('"', '\'');

    // Fallback: use the entire prompt as the identifier
    return prompt;
}
