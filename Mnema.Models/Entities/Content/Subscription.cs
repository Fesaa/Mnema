using System;
using Microsoft.EntityFrameworkCore;
using Mnema.Common;
using Mnema.Models.Entities.User;

namespace Mnema.Models.Entities.Content;

[Index(nameof(UserId))]
[Obsolete("Use MonitoredSeries")]
public class Subscription: IEntityDate
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public MnemaUser User { get; set; }

    /// <summary>
    ///     The external content id
    /// </summary>
    public required string ContentId { get; set; }

    /// <summary>
    ///     Title given by the user, defaults to the series name
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     The directory to download the content in
    /// </summary>
    public required string BaseDir { get; set; }

    public required Provider Provider { get; set; }
    public required MetadataBag Metadata { get; set; }

    public required SubscriptionStatus Status { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

public enum SubscriptionStatus
{
    Enabled = 0,
    Disabled = 1,
}
