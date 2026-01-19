using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mnema.Models.Publication;

public sealed record Chapter
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string Summary { get; set; } = string.Empty;

    public required string VolumeMarker { get; set; }
    public required string ChapterMarker { get; set; }

    public string? CoverUrl { get; set; }
    public string? RefUrl { get; set; }

    public DateTime? ReleaseDate { get; set; }
    public required IList<Tag> Tags { get; set; }
    public required IList<Person> People { get; set; }

    public required IList<string> TranslationGroups { get; set; }

    public bool IsOneShot => string.IsNullOrEmpty(ChapterMarker) && string.IsNullOrEmpty(VolumeMarker);

    public string Label()
    {
        if (!string.IsNullOrEmpty(ChapterMarker) && !string.IsNullOrEmpty(VolumeMarker))
            return $"Volume {VolumeMarker} Chapter {ChapterMarker}: {Title}";

        if (!string.IsNullOrEmpty(ChapterMarker)) return $"Chapter {ChapterMarker}: {Title}";

        return $"OneShot: {Title}";
    }

    private const NumberStyles NumberStyle = NumberStyles.AllowDecimalPoint
                                             | NumberStyles.AllowLeadingSign
                                             | NumberStyles.Float;

    public float? VolumeNumber()
    {
        if (string.IsNullOrEmpty(VolumeMarker)) return null;

        if (float.TryParse(VolumeMarker, NumberStyle, CultureInfo.InvariantCulture, out var volume)) return volume;

        return null;
    }

    public float? ChapterNumber()
    {
        if (string.IsNullOrEmpty(ChapterMarker)) return null;

        if (float.TryParse(ChapterMarker, NumberStyle, CultureInfo.InvariantCulture, out var chapter)) return chapter;

        return null;
    }
}
