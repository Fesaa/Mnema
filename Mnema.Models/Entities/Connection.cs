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
    SubscriptionExhausted = 3
}
