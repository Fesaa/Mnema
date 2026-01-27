namespace Mnema.Models.DTOs;

public sealed record MetadataProviderSettingsDto(
    int Priority,
    bool Enabled,
    SeriesMetadataSettingsDto SeriesSettings
);

public sealed record SeriesMetadataSettingsDto(
    bool Title,
    bool Summary,
    bool LocalizedSeries,
    bool CoverUrl,
    bool PublicationStatus,
    bool Year,
    bool AgeRating,
    bool Tags,
    bool People,
    bool Links,
    bool Chapters,
    ChapterMetadataSettingsDto ChapterSettings
);

public sealed record ChapterMetadataSettingsDto(
    bool Title,
    bool Summary,
    bool Cover,
    bool ReleaseDate,
    bool People,
    bool Tags
);
