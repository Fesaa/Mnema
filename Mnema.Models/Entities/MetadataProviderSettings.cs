using System;
using Microsoft.EntityFrameworkCore;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;
using Mnema.Models.Enums;

namespace Mnema.Models.Entities;

[Index(nameof(MetadataProvider), IsUnique = true)]
public class MetadataProviderSettings: IDatabaseEntity
{
    public Guid Id { get; set; }
    public MetadataProvider MetadataProvider { get; set; }

    public int Priority { get; set; }

    public bool Enabled { get; set; }

    public bool SeriesTitle { get; set; }

    public bool SeriesSummary { get; set; }

    public bool SeriesLocalizedName { get; set; }

    public bool SeriesCoverUrl { get; set; }

    public bool SeriesPublicationStatus { get; set; }

    public bool SeriesAgeRating { get; set; }

    public bool SeriesYear { get; set; }

    public bool SeriesTags { get; set; }

    public bool SeriesPeople { get; set; }

    public bool SeriesLinks { get; set; }

    public bool Chapters { get; set; }

    public bool ChapterTitle { get; set; }

    public bool ChapterSummary { get; set; }

    public bool ChapterReleaseDate { get; set; }

    public bool ChapterPeople { get; set; }

    public bool ChapterTags { get; set; }

    public bool ChapterCoverUrl { get; set; }

    public MetadataBag MetadataProviderSpecific { get; set; }

    public T GetKey<T>(IMetadataKey<T> key)
    {
        return MetadataProviderSpecific.GetKey(key);
    }
}
