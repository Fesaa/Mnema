namespace Mnema.Models.Publication;

public sealed record Tag
{
    public string Id { get; set; }
    public string Value { get; set; }
    /// <summary>
    /// True if the source has explicitly marked the tag as genre
    /// </summary>
    public bool IsMarkedAsGenre { get; set; } = false;
    
    public Tag() {}

    public Tag(string value)
    {
        Id = value;
        Value = value;
    }
}