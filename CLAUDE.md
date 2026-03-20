# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PlaylistBuilder is a CLI-driven Spotify playlist builder powered by Claude AI. Users provide a natural-language prompt, and the system analyzes a Spotify playlist, sends metadata to Claude for song recommendations, then creates a new Spotify playlist with those recommendations.

## Solution Structure

- **PlaylistBuilder.slnx** — Solution file
- **PlaylistBuilder.Core/** — Shared models, DTOs, and interfaces (no external dependencies)
  - `Models/` — Domain objects (SpotifyTrack, SpotifyPlaylist, AudioFeatures, etc.)
  - `DTOs/` — Request/response objects for API communication
  - `Interfaces/` — ISpotifyService, IClaudeService, IPlaylistOrchestrator
- **PlaylistBuilder.Api/** — ASP.NET Core 8.0 Web API
  - `Controllers/` — PlaylistController, SpotifyAuthController
  - `Services/` — SpotifyService, ClaudeService, PlaylistOrchestrator, SpotifyTokenStore
  - `Configuration/` — SpotifySettings, AnthropicSettings
- **PlaylistBuilder.Cli/** — Console app (thin client calling the API)
- **PlaylistBuilder.Tests/** — xUnit tests (unit + integration)

## Build & Run Commands

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run API (HTTP on localhost:5263)
dotnet run --project PlaylistBuilder.Api

# Run CLI
dotnet run --project PlaylistBuilder.Cli -- "Make a playlist like EDM Lo-Fi Mix but with different songs"
```

Swagger UI is available at http://localhost:5263/swagger in Development mode.

## API Endpoints

- `POST /api/playlist/build` — Full workflow: analyze + recommend + create
- `POST /api/playlist/analyze` — Preview recommendations only
- `GET /api/spotify/auth/url` — Get Spotify OAuth URL
- `GET /api/spotify/auth/callback` — OAuth redirect handler
- `GET /api/spotify/auth/status` — Check auth state
- `GET /api/health` — Health check

## Configuration & Secrets

Local development uses .NET User Secrets:
```bash
dotnet user-secrets set "Spotify:ClientId" "<value>" --project PlaylistBuilder.Api
dotnet user-secrets set "Spotify:ClientSecret" "<value>" --project PlaylistBuilder.Api
dotnet user-secrets set "Anthropic:ApiKey" "<value>" --project PlaylistBuilder.Api
```

Environment variables: `Spotify__ClientId`, `Spotify__ClientSecret`, `Anthropic__ApiKey`

## Key Configuration

- **Target framework:** .NET 8.0
- **Nullable reference types:** Enabled
- **Implicit usings:** Enabled
- **Launch profiles:** `http` (port 5263), `https` (port 7055), `IIS Express`
- **Key packages:** SpotifyAPI.Web 7.4.2, Anthropic.SDK 5.10.0, Swashbuckle.AspNetCore 6.6.2
- **Test packages:** xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing

## Architecture Notes

- Services are registered via DI in Program.cs with interface-based injection
- SpotifyTokenStore is a singleton holding in-memory OAuth tokens (single-user design)
- PlaylistOrchestrator coordinates the workflow: fetch playlist → get audio features → build metadata → Claude recommendations → search Spotify → create playlist
- CLI communicates with API over HTTP (localhost only, no auth)
