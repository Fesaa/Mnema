using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadRequestDto: IValidatableObject
{
    public Guid UserId { get; set; }

    public required Provider Provider { get; set; }
    public required string Id { get; set; }
    /// <summary>
    /// I.e. Torrent magnet url
    /// </summary>
    public string? DownloadUrl { get; set; }

    public required string BaseDir { get; set; }

    [JsonPropertyName("title")]
    public required string TempTitle { get; set; }

    [Required]
    public bool StartImmediately { get; set; }

    public required MetadataBag Metadata { get; set; }

    public T GetKey<T>(IMetadataKey<T> key)
    {
        return Metadata.GetKey(key);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GetKey(RequestConstants.IsGroupedDownload))
        {
            var hasMangabaka = Metadata.HasKey(RequestConstants.MangaBakaKey);
            var hasHardcover = Metadata.HasKey(RequestConstants.HardcoverSeriesIdKey);

            if (!hasMangabaka && !hasHardcover)
            {
                yield return new ValidationResult(
                    $"Grouped downloads must be linked to external metadata",
                    [RequestConstants.MangaBakaKey.Key, RequestConstants.HardcoverSeriesIdKey.Key]
                );
            }
        }
    }
}
