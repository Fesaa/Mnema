using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadRequestDto
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

    [JsonIgnore] public Guid? SubscriptionId { get; set; }

    public bool IsSubscription => SubscriptionId != null;

    public string? GetString(string key)
    {
        return Metadata.GetString(key);
    }

    public string GetStringOrDefault(string key, string defaultValue)
    {
        return Metadata.GetStringOrDefault(key, defaultValue);
    }

    public bool GetBool(string key, bool fallback = false)
    {
        return Metadata.GetBool(key, fallback);
    }
}
