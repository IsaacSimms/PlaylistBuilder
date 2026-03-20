# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PlaylistBuilder is an ASP.NET Core 8.0 Web API. The solution uses a single project (`PlaylistBuilder.Api`) with controller-based routing, built-in DI, and Swagger/OpenAPI via Swashbuckle.

## Build & Run Commands

```bash
# Build
dotnet build

# Run (HTTP on localhost:5263)
dotnet run --project PlaylistBuilder.Api

# Run with HTTPS (localhost:7055)
dotnet run --project PlaylistBuilder.Api --launch-profile https
```

Swagger UI is available at the root URL in Development mode.

## Solution Structure

- **PlaylistBuilder.slnx** — Solution file
- **PlaylistBuilder.Api/** — Web API project
  - `Program.cs` — Entry point and middleware pipeline configuration
  - `Controllers/` — API controllers

## Key Configuration

- **Target framework:** .NET 8.0
- **Nullable reference types:** Enabled
- **Implicit usings:** Enabled
- **Launch profiles:** `http` (port 5263), `https` (port 7055), `IIS Express`
- **Swagger package:** Swashbuckle.AspNetCore 6.6.2
