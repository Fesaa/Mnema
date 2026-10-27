using System.Text.Json.Serialization;

namespace Mnema.Providers.Managers.QBit;

internal sealed class TorrentContent
{
    [JsonPropertyName("index")]
    public int? Index { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("progress")]
    public double Progress { get; init; }

    [JsonPropertyName("priority")]
    public TorrentContentPriority Priority { get; init; }
}
