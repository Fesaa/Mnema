namespace Mnema.Models.Publication;

public sealed record Tag
{
    public Tag()
    {
    }

    public Tag(string value)
    {
        Id = value;
        Value = value;
    }

    public Tag(string value, bool isMarkedAsGenre)
    {
        Id = value;
        Value = value;
        IsMarkedAsGenre = isMarkedAsGenre;
    }

    public string Id { get; set; }
    public string Value { get; set; }

    /// <summary>
    ///     True if the source has explicitly marked the tag as genre
    /// </summary>
    public bool IsMarkedAsGenre { get; set; }
}