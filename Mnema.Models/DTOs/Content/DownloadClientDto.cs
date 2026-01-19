using System;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public class DownloadClientDto
{
    public Guid Id { get; set; }
    public DownloadClientType Type { get; set; }
    public string Name { get; set; }
    public bool IsFailed { get; set; }
    public DateTime? FailedAt { get; set; }
    public MetadataBag Metadata { get; set; }
}
