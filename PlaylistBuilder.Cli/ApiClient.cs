using System.Net.Http.Json;
using PlaylistBuilder.Core.DTOs.Requests;
using PlaylistBuilder.Core.DTOs.Responses;

namespace PlaylistBuilder.Cli;

// == ApiClient == //
/// <summary>
/// HTTP client wrapper for communicating with the PlaylistBuilder API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public ApiClient(string baseUrl = "https://localhost:7055")
    {
        // Accept the .NET dev HTTPS certificate for localhost
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // Only trust localhost connections
            if (message.RequestUri?.Host == "localhost") return true;
            return errors == System.Net.Security.SslPolicyErrors.None;
        };
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    // == Check Auth Status == //
    public async Task<bool> IsAuthenticatedAsync()
    {
        var response = await _httpClient.GetAsync("/api/spotify/auth/status");
        if (!response.IsSuccessStatusCode) return false;

        var body = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        return body?.IsAuthenticated ?? false;
    }

    // == Get Auth URL == //
    public async Task<string?> GetAuthUrlAsync()
    {
        var response = await _httpClient.GetAsync("/api/spotify/auth/url");
        if (!response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadFromJsonAsync<AuthUrlResponse>();
        return body?.Url;
    }

    // == Analyze Playlist == //
    public async Task<AnalyzePlaylistResponse?> AnalyzeAsync(AnalyzePlaylistRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/playlist/analyze", request);
        return await response.Content.ReadFromJsonAsync<AnalyzePlaylistResponse>();
    }

    // == Build Playlist == //
    public async Task<AnalyzePlaylistResponse?> BuildAsync(AnalyzePlaylistRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/playlist/build", request);
        return await response.Content.ReadFromJsonAsync<AnalyzePlaylistResponse>();
    }

    // == Health Check == //
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // == Response DTOs == //
    private record AuthStatusResponse(bool IsAuthenticated);
    private record AuthUrlResponse(string Url);
}
