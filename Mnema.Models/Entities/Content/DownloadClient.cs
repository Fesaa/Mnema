using System;
using Microsoft.EntityFrameworkCore;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.Content;

[Index(nameof(Type), IsUnique = true)]
public class DownloadClient: IDatabaseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DownloadClientType Type { get; set; }
    public bool IsFailed { get; set; }
    public DateTime? FailedAt { get; set; }
    public MetadataBag Metadata { get; set; }
}

public enum DownloadClientType
{
    QBittorrent,
}
