using System;
using System.Text.Json.Serialization;

namespace Mnema.Providers.Managers.QBit;

internal sealed class TorrentInfo
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = string.Empty;

    [JsonPropertyName("save_path")]
    public string SavePath { get; init; } = string.Empty;

    [JsonPropertyName("progress")]
    public double Progress { get; init; }

    [JsonPropertyName("dlspeed")]
    public long DownloadSpeed { get; init; }

    [JsonPropertyName("eta")]
    public long Eta { get; init; }

    [JsonPropertyName("state")]
    [JsonConverter(typeof(TorrentStateConverter))]
    public TorrentState State { get; init; }

    [JsonIgnore]
    public TimeSpan? EstimatedTime => Eta >= 0 ? TimeSpan.FromSeconds(Eta) : null;
}
