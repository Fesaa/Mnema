using Mnema.Models.Entities.Content;

namespace Mnema.Models.DTOs.Content;

public sealed record DownloadRequestDto
{
    
    public required Provider Provider { get; set; }
    public required string Id { get; set; }
    public required string BaseDir { get; set; }
    public required string TempTitle { get; set; }
    public required DownloadMetadataDto DownloadMetadata { get; set; }

    public string? GetString(string key)
    {
        if (DownloadMetadata.Extra.TryGetValue(key, out var list) && list.Count > 0)
        {
            return list[0];
        }

        return null;
    }
    
    public string GetStringOrDefault(string key, string defaultValue)
    {
        var value = GetString(key);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }
    
}

public sealed record DownloadMetadataDto
{
    public bool StartImmediately { get; set; } = false;
    public Dictionary<string, IList<string>> Extra { get; set; } = [];
}