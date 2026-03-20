namespace PlaylistBuilder.Core.Models;

// == AudioFeatures == //
public class AudioFeatures
{
    public string TrackId { get; set; } = string.Empty;
    public float Danceability { get; set; }
    public float Energy { get; set; }
    public float Tempo { get; set; }
    public float Valence { get; set; }
    public float Acousticness { get; set; }
    public float Instrumentalness { get; set; }
    public float Speechiness { get; set; }
    public float Liveness { get; set; }
}
