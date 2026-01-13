using System;
using Microsoft.EntityFrameworkCore;
using Mnema.Common;

namespace Mnema.Models.Entities.Content;

[Index(nameof(Type), IsUnique = true)]
public class DownloadClient
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DownloadClientType Type { get; set; }
    public MetadataBag Metadata { get; set; }
}

public enum DownloadClientType
{
    QBittorrent,
}
