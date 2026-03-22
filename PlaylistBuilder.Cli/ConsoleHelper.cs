using PlaylistBuilder.Core.DTOs.Responses;
using PlaylistBuilder.Core.Models;
using PlaylistBuilder.Core.Models;

namespace PlaylistBuilder.Cli;

// == ConsoleHelper == //
/// <summary>
/// Formatted console output for the CLI.
/// </summary>
public static class ConsoleHelper
{
    public static void WriteHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n=== {text} ===\n");
        Console.ResetColor();
    }

    public static void WriteSuccess(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void WriteError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {text}");
        Console.ResetColor();
    }

    public static void WriteWarning(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void WriteInfo(string text)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    // == Display Recommendations == //
    public static void DisplayRecommendations(AnalyzePlaylistResponse response)
    {
        if (response.Metadata != null)
        {
            WriteHeader($"Analysis of \"{response.Metadata.PlaylistName}\"");
            WriteInfo($"  Tracks: {response.Metadata.TrackCount}");
            WriteInfo($"  Avg Danceability: {response.Metadata.AvgDanceability:F2}");
            WriteInfo($"  Avg Energy: {response.Metadata.AvgEnergy:F2}");
            WriteInfo($"  Avg Tempo: {response.Metadata.AvgTempo:F0} BPM");
        }

        WriteHeader("Recommended Tracks");
        for (int i = 0; i < response.Recommendations.Count; i++)
        {
            var rec = response.Recommendations[i];
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {i + 1,2}. ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(rec.Name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" by ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(rec.Artist);

            if (!string.IsNullOrEmpty(rec.Reason))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      {rec.Reason}");
            }
        }
        Console.ResetColor();
    }

    // == Display Result == //
    public static void DisplayResult(AnalyzePlaylistResponse response)
    {
        if (!string.IsNullOrEmpty(response.NewPlaylistUrl))
        {
            WriteHeader("Playlist Created!");
            WriteSuccess($"  Open in Spotify: {response.NewPlaylistUrl}");
        }
    }

    // == Ask Confirmation == //
    public static bool AskConfirmation(string question)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"\n{question} (y/n): ");
        Console.ResetColor();

        var key = Console.ReadLine()?.Trim().ToLower();
        return key == "y" || key == "yes";
    }

    // == Model Selection Menu == //
    public static string? SelectModel(List<SupportedModel> models)
    {
        WriteHeader("Select AI Model");

        for (int i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var defaultTag = model.IsDefault ? " (default)" : "";

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {i + 1}. ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(model.DisplayName);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(defaultTag);
        }

        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"\nChoose model (1-{models.Count}, or Enter for default): ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim();

        // Enter = use default
        if (string.IsNullOrEmpty(input))
            return null;

        if (int.TryParse(input, out var choice) && choice >= 1 && choice <= models.Count)
            return models[choice - 1].Id;

        WriteWarning("Invalid selection — using default model.");
        return null;
    }
}
