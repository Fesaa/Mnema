using System;
using System.Collections.Generic;
using Mnema.Common;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.DTOs.Content;

public class ExternalDownloadDto: IDatabaseEntity
{
    public Guid Id { get; set; }
    /// <summary>
    /// Id in the external client (I.e. torrent hash)
    /// </summary>
    public string ExternalId { get; set; }

    public Provider Provider { get; set; }
    public Guid UserId { get; set; }
    public MetadataBag Metadata { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public List<ExternalDownloadFileDto> Files { get; set; }
}

public class ExternalDownloadFileDto
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; }
    public string FileName { get; set; }
    public string? VolumeMarker { get; set; }
    public string? ChapterMarker { get; set; }
    public bool Selected { get; set; } = true;
}
