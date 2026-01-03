using Mnema.Common;

namespace Mnema.Models.Entities.External;

public class ExternalConnection
{
    public Guid Id { get; set; }
    
    public ExternalConnectionType Type { get; set; }
    public string Name { get; set; }
    public List<ExternalConnectionEvent> FollowedEvents { get; set; }
    public MetadataBag Metadata { get; set; }
    
}

public enum ExternalConnectionType
{
    Discord = 0,
    Kavita = 1,
}

public enum ExternalConnectionEvent
{
    /// <summary>
    /// Fired when Content starts to download, after metadata has loaded and something new will start
    /// </summary>
    ContentDownloadStarted = 0,
    /// <summary>
    /// Fired when Content has finished cleaning itself up
    /// </summary>
    DownloadFinished = 1,
    /// <summary>
    /// Fired when a Download unexpectedly fails
    /// </summary>
    DownloadFailure = 2,
}