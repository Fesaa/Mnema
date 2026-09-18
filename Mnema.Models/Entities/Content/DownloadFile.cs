namespace Mnema.Models.Entities.Content;

public class DownloadFile
{
    public required string FileName { get; set; }
    public required string FullPath { get; set; }
    public required long FileSize { get; set; }
    public required string? VolumeMarker { get; set; }
    public required string? ChapterMarker { get; set; }
    public required bool Selected { get; set; } = true;
    /// <summary>
    /// True if this file has been processed in cleanup
    /// </summary>
    public bool Processed { get; set; } = false;
}
