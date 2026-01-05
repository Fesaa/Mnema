using System;

namespace Mnema.Models.Entities.Content;

public class ContentRelease: IEntityDate
{
    /// <summary>
    /// The id in the database, not required when loading from a provider
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The id that uniquely defines this content release (I.e. chapter id). Used to match duplicates
    /// </summary>
    public required string ReleaseId { get; set; }

    /// <summary>
    /// The id that uniquely defines the content this release is part of (I.e. series id)
    /// </summary>
    /// <remarks>This is not required, releases from RSS feeds do not have this</remarks>
    public string? ContentId { get; set; }

    /// <summary>
    /// Name of the release (I.e. chapter name)
    /// </summary>
    public string ReleaseName { get; set; }

    /// <summary>
    /// Name of the content (I.e. series name)
    /// </summary>
    public string ContentName { get; set; }

    /// <summary>
    /// Time this release was published
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
