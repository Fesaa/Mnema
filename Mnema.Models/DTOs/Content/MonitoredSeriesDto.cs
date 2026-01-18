using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record MonitoredSeriesDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// This title has no effect on actual downloads
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Titles that are considered valid for this release. I.e. Translated, original, romanized, etc
    /// </summary>
    /// <remarks>You can use the auto complete in the UI to load from metadata providers</remarks>
    public List<string> ValidTitles { get; set; }

    public List<Provider> Providers { get; set; }

    public ContentFormat ContentFormat { get; set; }
    public Format Format { get; set; }


    /// <summary>
    /// Contains ids of <see cref="MetadataProvider"/>
    /// </summary>
    public MetadataBag Metadata { get; set; }


    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
