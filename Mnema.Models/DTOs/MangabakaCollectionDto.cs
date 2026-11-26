namespace Mnema.Models.DTOs;

/// <summary>
/// A single Mangabaka collection (a physical/digital edition grouping of works) of a series
/// </summary>
public sealed record MangabakaCollectionDto
{
    public required string Id { get; init; }
    public int SeriesId { get; init; }
    public string? Title { get; init; }
    public string? Type { get; init; }
    public string? Medium { get; init; }
    public int CountMain { get; init; }
    public int CountExtra { get; init; }
    public int CountOther { get; init; }
    public string? LanguageIso { get; init; }
    public string? PublisherName { get; init; }
}
