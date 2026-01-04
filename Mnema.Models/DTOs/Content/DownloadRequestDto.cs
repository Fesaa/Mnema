using System;
using System.Text.Json.Serialization;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadRequestDto
{
    [JsonIgnore] public Guid UserId;

    public required Provider Provider { get; set; }
    public required string Id { get; set; }

    [JsonPropertyName("dir")] public required string BaseDir { get; set; }

    [JsonPropertyName("title")] public required string TempTitle { get; set; }

    public required DownloadMetadataDto DownloadMetadata { get; set; }

    [JsonIgnore] public Guid? SubscriptionId { get; set; }

    public bool IsSubscription => SubscriptionId != null;

    public string? GetString(string key)
    {
        return DownloadMetadata.Extra.GetString(key);
    }

    public string GetStringOrDefault(string key, string defaultValue)
    {
        return DownloadMetadata.Extra.GetStringOrDefault(key, defaultValue);
    }

    public bool GetBool(string key, bool fallback = false)
    {
        return DownloadMetadata.Extra.GetBool(key, fallback);
    }
}

public sealed record DownloadMetadataDto
{
    public bool StartImmediately { get; set; } = false;
    public MetadataBag Extra { get; set; } = [];
}