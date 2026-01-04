using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadRequestDto
{
    [JsonIgnore]
    public Guid UserId;

    public required Provider Provider { get; set; }
    public required string Id { get; set; }

    [JsonPropertyName("dir")]
    public required string BaseDir { get; set; }

    [JsonPropertyName("title")]
    public required string TempTitle { get; set; }

    [Required]
    public bool StartImmediately { get; set; }

    public required MetadataBag DownloadMetadata { get; set; }

    [JsonIgnore] public Guid? SubscriptionId { get; set; }

    public bool IsSubscription => SubscriptionId != null;

    public string? GetString(string key)
    {
        return DownloadMetadata.GetString(key);
    }

    public string GetStringOrDefault(string key, string defaultValue)
    {
        return DownloadMetadata.GetStringOrDefault(key, defaultValue);
    }

    public bool GetBool(string key, bool fallback = false)
    {
        return DownloadMetadata.GetBool(key, fallback);
    }
}
