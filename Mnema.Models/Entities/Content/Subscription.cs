using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Models.Entities.Content;

public class Subscription
{
    
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    public MnemaUser User { get; set; }
    
    /// <summary>
    /// The external content id
    /// </summary>
    public required string ContentId { get; set; }
    /// <summary>
    /// The directory to download the content in
    /// </summary>
    public required string BaseDir { get; set; }
    /// <summary>
    /// The last full directory (I.e. with name) the content was downloaded in
    /// </summary>
    public string LastDownloadDir { get; set; } = string.Empty;
    
    public required DownloadMetadataDto Metadata { get; set; }
 
    /// <summary>
    /// When the last run took place
    /// </summary>
    public DateTime LastRun { get; set; }
    /// <summary>
    /// If the last run was a success
    /// </summary>
    public bool LastRunSuccess { get; set; }
    
    /// <summary>
    /// When the next run is expected to take place
    /// </summary>
    public DateTime NextRun { get; set; }
    
    /// <summary>
    /// Represents the amount of sequential runs without any chapters being downloaded
    /// </summary>
    public int NoDownloadsRuns { get; set; }
    
}