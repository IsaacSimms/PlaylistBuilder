# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PlaylistBuilder is a CLI-driven Spotify playlist builder powered by Claude AI. Users provide a natural-language prompt, and the system analyzes a Spotify playlist (or the user's listening history), sends metadata to Claude for song recommendations, then optionally creates a new Spotify playlist with those recommendations.

## Solution Structure

- **PlaylistBuilder.slnx** — Solution file
- **PlaylistBuilder.Core/** — Shared models, DTOs, and interfaces (no external dependencies)
  - `Models/` — Domain objects (`SpotifyTrack`, `SpotifyPlaylist`, `AudioFeatures`, `PlaylistMetadata`, `TrackRecommendation`, `SupportedModel`)
  - `DTOs/Requests/` — `AnalyzePlaylistRequest`
  - `DTOs/Responses/` — `AnalyzePlaylistResponse`, `AuthStatusResponse`, `AuthUrlResponse`
  - `Interfaces/` — `ISpotifyService`, `IClaudeService`, `IPlaylistOrchestrator`
  - `SupportedModels.cs` — Static catalog of valid Anthropic model IDs with default selection and validation
- **PlaylistBuilder.Api/** — ASP.NET Core 8.0 Web API
  - `Controllers/` — `PlaylistController`, `SpotifyAuthController`, `ModelsController`, `HealthController`
  - `Services/` — `SpotifyService`, `ClaudeService`, `PlaylistOrchestrator`, `SpotifyTokenStore`
  - `Configuration/` — `SpotifySettings`, `AnthropicSettings`
- **PlaylistBuilder.Cli/** — Console app (thin HTTP client calling the API)
  - `ApiClient.cs` — HTTP wrapper for all API endpoints
  - `ConsoleHelper.cs` — Formatted colored console output and interactive prompts
  - `Program.cs` — Entry point with arg parsing, auth flow, model selection, and playlist workflow
- **PlaylistBuilder.Tests/** — xUnit tests (unit + integration)
  - `Unit/Controllers/` — `PlaylistControllerTests`, `ModelsControllerTests`
  - `Unit/Services/` — `PlaylistOrchestratorTests`
  - `Unit/Cli/` — `ApiClientTests` (uses `MockHttpHandler` for deterministic HTTP)
  - `Integration/` — `PlaylistEndpointTests` (uses `WebApplicationFactory<Program>` with fake services)
  - `Helpers/` — `TestData` (factory methods), `FakeSpotifyService`, `FakeClaudeService`

## Build & Run Commands

Environment variables: `Spotify__ClientId`, `Spotify__ClientSecret`, `Anthropic__ApiKey`, `Anthropic__ModelId`

## Key Configuration

- **Target framework:** .NET 8.0 / C# 12.0
- **Nullable reference types:** Enabled
- **Implicit usings:** Enabled
- **Launch profiles:** `http` (port 5263), `https` (port 7055), `IIS Express`
- **Key packages:** SpotifyAPI.Web 7.4.2, Anthropic.SDK 5.10.0, Swashbuckle.AspNetCore 6.6.2
- **Test packages:** xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing

## Architecture Notes

### Dependency Injection
- Services registered in `Program.cs` with `AddScoped` (per-request lifetime)
- `SpotifyTokenStore` is a `AddSingleton` — it holds in-memory OAuth tokens across requests
- `ISpotifyClient` is scoped and created from `SpotifyTokenStore.AccessToken`; throws `InvalidOperationException` if not authenticated
- All controllers and services depend on interfaces from `PlaylistBuilder.Core`, never on concrete implementations

### Orchestrator Workflow (`PlaylistOrchestrator`)
1. **Resolve playlist** — extract Spotify ID from URL via regex, or search by name
2. **Fallback** — if no playlist found, combine `GetRecentlyPlayedAsync` + `GetTopTracksAsync` as source tracks
3. **Audio features** — fetch via `GetAudioFeaturesAsync` (wrapped in try/catch — see Known Issues)
4. **Build metadata** — aggregate track names, audio feature averages into `PlaylistMetadata`
5. **Claude recommendations** — send metadata + user prompt + exclusion list to Anthropic
6. **Build mode only:** search Spotify for each recommendation, create new playlist with matched tracks

### Model Selection
- `SupportedModels.cs` in Core defines the valid model catalog (static `IReadOnlyList<SupportedModel>`)
- Resolution order: per-request `ModelId` → `AnthropicSettings.ModelId` → `SupportedModels.DefaultModelId`
- Invalid or null model IDs silently fall back to the default via `SupportedModels.Resolve()`
- The default model is `claude-sonnet-4-6`; update `SupportedModels.All` when adding new models

### CLI Design
- CLI is a thin HTTP client — all logic lives in the API
- `ApiClient` handles error deserialization: tries JSON `AnalyzePlaylistResponse`, falls back to raw text
- `Program.cs` extracts playlist identifiers from natural language using regex (looks for URLs, "like X", "similar to X" patterns)
- Interactive model selection when `--model` flag is not provided
- Polls `/api/spotify/auth/status` every 2 seconds during OAuth flow

### SpotifyService Batching
- Audio features and playlist track additions are batched in chunks of 100 (Spotify API limit)
- `CreatePlaylistAsync` creates the playlist first, then adds tracks in batches

## Known Issues & Gotchas

### Spotify Audio Features Deprecation
- Spotify deprecated the `/audio-features` endpoint (November 2024)
- `PlaylistOrchestrator.AnalyzePlaylistAsync` wraps the call in try/catch and continues with empty audio features on failure
- The Claude prompt still references audio feature averages — they will be `0.00` when unavailable
- If extending the prompt, handle the case where all audio feature values are zero

### SpotifyAPI.Web Exceptions
- `SpotifyAPI.Web.APIException` is thrown by the library when Spotify returns non-success HTTP status
- Always wrap `ISpotifyClient` calls in try/catch when adding new Spotify interactions
- The orchestrator already handles this for playlist resolution and audio features; `BuildPlaylistAsync` handles it for track search and playlist creation

### Single-User Design
- `SpotifyTokenStore` is an in-memory singleton — only one Spotify user at a time
- No token refresh logic — tokens expire and require re-authentication
- Not suitable for multi-user deployment without redesign

### CLI-API Connection
- CLI defaults to `http://127.0.0.1:5263` — API must be running locally
- No authentication between CLI and API — localhost-only design

## Code Style & Commenting Conventions

Follow these conventions for all C# files:
1. Begin functions, classes, or important code blocks with a title comment: `// == Title Here == //`
2. Multi-line or longer comments go above the code: `// explanation here`
3. Single-line comments on the same line as code, spaced out and aligned with nearby comments (soft rule)
4. File summaries at top of every file using: `// <summary> // ... // </summary>` — outline what the file does, important code blocks, sensitive info
5. Keep comments short and concise. Avoid long sentences unless high-impact.
6. **Do NOT change actual code when applying comment conventions.**

## Testing Conventions

### Unit Tests
- Use Moq for interface mocking, FluentAssertions for assertions
- Test class name matches the class under test: `PlaylistControllerTests`, `PlaylistOrchestratorTests`
- Constructor sets up mocks and creates the system under test
- Follow Arrange/Act/Assert pattern with explicit comments
- Use `TestData` factory methods for creating test objects — do not inline test data

### Integration Tests
- Use `WebApplicationFactory<Program>` with `IClassFixture`
- Replace real services with `FakeSpotifyService` and `FakeClaudeService` via `ConfigureServices`
- `Program.cs` exposes `public partial class Program { }` specifically for `WebApplicationFactory` access
- Fake services return deterministic data and never hit external APIs

### Adding New Tests
- Place unit tests in `PlaylistBuilder.Tests/Unit/{Layer}/` matching the source project structure
- Place integration tests in `PlaylistBuilder.Tests/Integration/`
- Add new factory methods to `TestData.cs` for any new model types
- Add new fake service implementations in `Helpers/` for any new interfaces

