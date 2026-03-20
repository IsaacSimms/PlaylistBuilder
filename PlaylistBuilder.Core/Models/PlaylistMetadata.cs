namespace PlaylistBuilder.Core.Models;

// == PlaylistMetadata == //
public class PlaylistMetadata
{
    public string PlaylistName { get; set; } = string.Empty;
    public int TrackCount { get; set; }
    public Dictionary<string, int> GenreDistribution { get; set; } = new();
    public List<string> TrackNames { get; set; } = new();

    // Average audio features across all tracks
    public float AvgDanceability { get; set; }
    public float AvgEnergy { get; set; }
    public float AvgTempo { get; set; }
    public float AvgValence { get; set; }
    public float AvgAcousticness { get; set; }
    public float AvgInstrumentalness { get; set; }
}
