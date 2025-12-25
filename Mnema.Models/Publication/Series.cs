namespace Mnema.Models.Publication;

public sealed record Series
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? LocalizedSeries { get; set; }
    public required string Summary { get; set; }
    
    public string? CoverUrl { get; set; }
    public string? RefUrl { get; set; }
    
    public required PublicationStatus Status { get; set; }
    public PublicationStatus? TranslationStatus { get; set; }
    
    public int? Year { get; set; }
    
    public float? HighestVolumeNumber { get; set; }
    public float? HighestChapterNumber { get; set; }
    
    public AgeRating? AgeRating { get; set; }
    public required IList<Tag> Tags { get; set; }
    public required IList<Person> People { get; set; }
    public required IList<string> Links { get; set; }
    
    public required IList<Chapter> Chapters { get; set; }
    
}