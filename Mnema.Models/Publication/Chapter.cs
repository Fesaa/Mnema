namespace Mnema.Models.Publication;

public sealed record Chapter
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string Summary { get; set; } = string.Empty;
    
    public required string VolumeCount { get; set; }
    public required string ChapterCount { get; set; }
    
    public string? CoverUrl { get; set; }
    public string? RefUrl { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
    public required IList<Tag> Tags { get; set; }
    public required IList<Person> People { get; set; }
    
    public required IList<string> TranslationGroups { get; set; }

    public string Label()
    {
        if (!string.IsNullOrEmpty(ChapterCount) && !string.IsNullOrEmpty(VolumeCount))
        {
            return $"Volume {VolumeCount} Chapter {ChapterCount}: {Title}";
        }

        if (!string.IsNullOrEmpty(ChapterCount))
        {
            return $"Chapter {ChapterCount}: {Title}";
        }

        return $"OneShot: {Title}";
    }

    public float? VolumeNumber()
    {
        if (string.IsNullOrEmpty(VolumeCount))
        {
            return null;
        }

        if (float.TryParse(VolumeCount, out var volume))
        {
            return volume;
        }

        return null;
    }

    public float? ChapterNumber()
    {
        if (string.IsNullOrEmpty(ChapterCount))
        {
            return null;
        }

        if (float.TryParse(ChapterCount, out var chapter))
        {
            return chapter;
        }

        return null;
    }
}