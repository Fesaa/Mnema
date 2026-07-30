using System;
using System.Collections.Generic;
using System.IO;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Models.Entities.Content;

/// <summary>
/// Represents a download being down by an external client (I.e. Qbit)
/// </summary>
public class ExternalDownload: IEntityDate, IDatabaseEntity
{
    public Guid Id { get; set; }
    /// <summary>
    /// Id in the external client (I.e. torrent hash)
    /// </summary>
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public required string BaseDir  { get; set; }
    public required Provider Provider { get; set; }
    public required Guid UserId { get; set; }
    public required MetadataBag Metadata { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public required List<ExternalDownloadFile> Files { get; set; }

    public T GetKey<T>(IMetadataKey<T> key) => Metadata.GetKey(key);
}

public class ExternalDownloadFile
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string FullPath { get; set; }
    public required string? VolumeMarker { get; set; }
    public required string? ChapterMarker { get; set; }
    public required bool Selected { get; set; } = true;
}
