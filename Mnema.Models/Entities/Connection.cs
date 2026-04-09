using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities;

public class Connection: IDatabaseEntity
{
    public Guid Id { get; set; }

    public ConnectionType Type { get; set; }
    public string Name { get; set; }
    public List<ConnectionEvent> FollowedEvents { get; set; }
    public MetadataBag Metadata { get; set; }
}

public enum ConnectionType
{
    Discord = 0,
    Kavita = 1,
    Native = 2
}

public enum ConnectionEvent
{
    /// <summary>
    ///     Fired when Content starts to download, after metadata has loaded and something new will start
    /// </summary>
    DownloadStarted = 0,

    /// <summary>
    ///     Fired when Content has finished cleaning itself up
    /// </summary>
    DownloadFinished = 1,

    /// <summary>
    ///     Fired when a Download unexpectedly fails
    /// </summary>
    DownloadFailure = 2,

    /// <summary>
    ///     Fired when a subscription has downloaded everything it can find
    /// </summary>
    SubscriptionExhausted = 3,
    /// <summary>
    ///     Fired when a series is added to the monitored list
    /// </summary>
    SeriesMonitored = 4,
    /// <summary>
    ///     Fired when a series is removed from the monitored list
    /// </summary>
    SeriesUnmonitored = 5,
    /// <summary>
    ///     Fired when an automated download from a monitored series wants to download > 10 chapters. Requiring manual approval
    /// </summary>
    TooManyForAutomatedDownload = 6,
    /// <summary>
    ///     Fired when a download client is locked or unlocked
    /// </summary>
    DownloadClientEvents = 7,

    /// <summary>
    ///     Fired when an exception occurs through the application. This does not catch all of them
    /// </summary>
    Exception = 8,
}
