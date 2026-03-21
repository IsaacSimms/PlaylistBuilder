using PlaylistBuilder.Api.Configuration;
using PlaylistBuilder.Api.Services;
using PlaylistBuilder.Core.Interfaces;
using SpotifyAPI.Web;

var builder = WebApplication.CreateBuilder(args);

// == Configuration Binding == //
builder.Services.Configure<SpotifySettings>(builder.Configuration.GetSection("Spotify"));
builder.Services.Configure<AnthropicSettings>(builder.Configuration.GetSection("Anthropic"));

// == Service Registration == //
builder.Services.AddSingleton<SpotifyTokenStore>();

builder.Services.AddScoped<ISpotifyClient>(sp =>
{
    var tokenStore = sp.GetRequiredService<SpotifyTokenStore>();
    if (!tokenStore.IsAuthenticated)
        throw new InvalidOperationException("Spotify user is not authenticated. Complete OAuth flow first.");

    return new SpotifyClient(tokenStore.AccessToken!);
});

builder.Services.AddScoped<ISpotifyService, SpotifyService>();
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IPlaylistOrchestrator, PlaylistOrchestrator>();

// == Web API Setup == //
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// == Middleware Pipeline == //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// == Partial Class for Integration Tests == //
public partial class Program { }
