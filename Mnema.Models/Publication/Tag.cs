namespace Mnema.Models.Publication;

public sealed record Tag
{
    public required string Id { get; set; }
    public required string Value { get; set; }
    /// <summary>
    /// True if the source has explicitly marked the tag as genre
    /// </summary>
    public bool IsMarkedAsGenre { get; set; } = false;
}