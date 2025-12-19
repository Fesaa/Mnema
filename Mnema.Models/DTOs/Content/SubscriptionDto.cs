using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class SubscriptionDto
{
    /// <inheritdoc cref="Subscription.Id"/>
    public Guid Id { get; set; }
    /// <inheritdoc cref="Subscription.UserId"/>
    public Guid UserId { get; set; }
    
    /// <inheritdoc cref="Subscription.ContentId"/>
    public required string ContentId { get; set; }
    /// <inheritdoc cref="Subscription.Title"/>
    public required string Title { get; set; }
    /// <inheritdoc cref="Subscription.BaseDir"/>
    public required string BaseDir { get; set; }
    /// <inheritdoc cref="Subscription.Provider"/>
    public Provider Provider { get; set; }
    /// <inheritdoc cref="Subscription.Metadata"/>
    public required DownloadMetadataDto Metadata { get; set; }
 
    /// <inheritdoc cref="Subscription.LastRun"/>
    public DateTime LastRun { get; set; }
    /// <inheritdoc cref="Subscription.LastRunSuccess"/>
    public bool LastRunSuccess { get; set; }
    
    /// <inheritdoc cref="Subscription.NextRun"/>
    public DateTime NextRun { get; set; }
    
    /// <inheritdoc cref="Subscription.NoDownloadsRuns"/>
    public int NoDownloadsRuns { get; set; }
}