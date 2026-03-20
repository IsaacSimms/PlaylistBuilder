# PlaylistBuilder

A CLI-driven Spotify playlist builder powered by Claude AI. Describe a playlist in plain English and PlaylistBuilder will analyze an existing Spotify playlist, use Claude to recommend similar songs, and create a new playlist on your Spotify account.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A [Spotify Developer](https://developer.spotify.com/dashboard) application (free)
- An [Anthropic API key](https://console.anthropic.com/) for Claude

## Setup

### 1. Clone and build

```bash
git clone https://github.com/your-username/PlaylistBuilder.git
cd PlaylistBuilder
dotnet build
```

### 2. Create a Spotify Developer application

1. Go to the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) and create a new app.
2. Set the **Redirect URI** to `https://localhost:7055/api/spotify/auth/callback`.
3. Note your **Client ID** and **Client Secret**.

### 3. Configure API credentials

**Option A: .NET User Secrets (recommended for local development)**

```bash
dotnet user-secrets set "Spotify:ClientId" "your-client-id" --project PlaylistBuilder.Api
dotnet user-secrets set "Spotify:ClientSecret" "your-client-secret" --project PlaylistBuilder.Api
dotnet user-secrets set "Anthropic:ApiKey" "your-anthropic-api-key" --project PlaylistBuilder.Api
```

**Option B: Environment variables**

```bash
export Spotify__ClientId="your-client-id"
export Spotify__ClientSecret="your-client-secret"
export Anthropic__ApiKey="your-anthropic-api-key"
```

On Windows (PowerShell):
```powershell
$env:Spotify__ClientId = "your-client-id"
$env:Spotify__ClientSecret = "your-client-secret"
$env:Anthropic__ApiKey = "your-anthropic-api-key"
```

## Usage

### 1. Start the API server

```bash
dotnet run --project PlaylistBuilder.Api --launch-profile https
```

The API runs on `https://localhost:7055`. Swagger UI is available at `https://localhost:7055/swagger` during development.

### 2. Run the CLI

Open a second terminal and run:

```bash
dotnet run --project PlaylistBuilder.Cli -- "Make a playlist like EDM Lo-Fi Mix but with different songs"
```

You can reference playlists by **name** or by **Spotify URL**:

```bash
# By name
dotnet run --project PlaylistBuilder.Cli -- "Something similar to Chill Vibes but more upbeat"

# By URL
dotnet run --project PlaylistBuilder.Cli -- "Make a playlist like https://open.spotify.com/playlist/37i9dQZF1DX4WYpdgoIcn6 but jazzier"
```

### 3. Authenticate with Spotify

On your first run, the CLI will display a Spotify authorization URL. Open it in your browser, log in, and grant permissions. The CLI will detect the authentication automatically and continue.

### 4. Review and confirm

PlaylistBuilder will:

1. Fetch the source playlist and its audio features from Spotify.
2. Send the playlist metadata to Claude for song recommendations.
3. Display the recommended tracks for your review.
4. Ask for confirmation before creating the playlist on your Spotify account.

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/playlist/build` | Analyze, recommend, and create a new playlist |
| `POST` | `/api/playlist/analyze` | Preview recommendations without creating |
| `GET` | `/api/spotify/auth/url` | Get Spotify OAuth authorization URL |
| `GET` | `/api/spotify/auth/callback` | OAuth redirect handler |
| `GET` | `/api/spotify/auth/status` | Check Spotify authentication state |
| `GET` | `/api/health` | Health check |

### Example request body

```json
{
  "playlistIdentifier": "EDM Lo-Fi Mix",
  "userPrompt": "Make a playlist like EDM Lo-Fi Mix but with different songs",
  "trackCount": 20
}
```

## Running Tests

```bash
dotnet test
```

The test suite includes unit tests for all services, controllers, and CLI components, plus integration tests that exercise the full API pipeline with fake services.

## Project Structure

```
PlaylistBuilder/
  PlaylistBuilder.Api/       ASP.NET Core Web API (controllers, services, DI)
  PlaylistBuilder.Cli/       Console application (thin HTTP client)
  PlaylistBuilder.Core/      Shared models, DTOs, and interfaces
  PlaylistBuilder.Tests/     xUnit tests (unit + integration)
```

## License

See [LICENSE.txt](LICENSE.txt) for details.
