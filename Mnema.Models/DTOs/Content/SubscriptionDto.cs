using System;
using Mnema.Common;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs.Content;

public class SubscriptionDto: IDatabaseEntity
{
    /// <inheritdoc cref="Subscription.Id" />
    public Guid Id { get; set; } = Guid.Empty;

    /// <inheritdoc cref="Subscription.UserId" />
    public Guid UserId { get; set; }

    /// <inheritdoc cref="Subscription.ContentId" />
    public required string ContentId { get; set; }

    /// <inheritdoc cref="Subscription.Title" />
    public required string Title { get; set; }

    /// <inheritdoc cref="Subscription.BaseDir" />
    public required string BaseDir { get; set; }

    /// <inheritdoc cref="Subscription.Provider" />
    public Provider Provider { get; set; }

    /// <inheritdoc cref="Subscription.Metadata" />
    public required MetadataBag Metadata { get; set; }

    /// <inheritdoc cref="Subscription.Status" />
    public required SubscriptionStatus Status { get; set; }
}
