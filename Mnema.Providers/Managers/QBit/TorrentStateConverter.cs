using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mnema.Providers.Managers.QBit;

internal sealed class TorrentStateConverter : JsonConverter<TorrentState>
{
    public override TorrentState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString() switch
        {
            "error" => TorrentState.Error,
            "missingFiles" => TorrentState.MissingFiles,
            "uploading" => TorrentState.Uploading,
            "pausedUP" or "stoppedUP" => TorrentState.PausedUpload,
            "queuedUP" => TorrentState.QueuedUpload,
            "stalledUP" => TorrentState.StalledUpload,
            "checkingUP" => TorrentState.CheckingUpload,
            "forcedUP" => TorrentState.ForcedUpload,
            "downloading" => TorrentState.Downloading,
            "metaDL" => TorrentState.FetchingMetadata,
            "forcedMetaDL" => TorrentState.ForcedFetchingMetadata,
            "pausedDL" or "stoppedDL" => TorrentState.PausedDownload,
            "queuedDL" => TorrentState.QueuedDownload,
            "stalledDL" => TorrentState.StalledDownload,
            "checkingDL" => TorrentState.CheckingDownload,
            "forcedDL" => TorrentState.ForcedDownload,
            "queuedForChecking" => TorrentState.QueuedForChecking,
            "checkingResumeData" => TorrentState.CheckingResumeData,
            "allocating" => TorrentState.Allocating,
            "moving" => TorrentState.Moving,
            _ => TorrentState.Unknown,
        };
    }

    public override void Write(Utf8JsonWriter writer, TorrentState value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            TorrentState.Error => "error",
            TorrentState.MissingFiles => "missingFiles",
            TorrentState.Uploading => "uploading",
            TorrentState.PausedUpload => "stoppedUP",
            TorrentState.QueuedUpload => "queuedUP",
            TorrentState.StalledUpload => "stalledUP",
            TorrentState.CheckingUpload => "checkingUP",
            TorrentState.ForcedUpload => "forcedUP",
            TorrentState.Downloading => "downloading",
            TorrentState.FetchingMetadata => "metaDL",
            TorrentState.ForcedFetchingMetadata => "forcedMetaDL",
            TorrentState.PausedDownload => "stoppedDL",
            TorrentState.QueuedDownload => "queuedDL",
            TorrentState.StalledDownload => "stalledDL",
            TorrentState.CheckingDownload => "checkingDL",
            TorrentState.ForcedDownload => "forcedDL",
            TorrentState.QueuedForChecking => "queuedForChecking",
            TorrentState.CheckingResumeData => "checkingResumeData",
            TorrentState.Allocating => "allocating",
            TorrentState.Moving => "moving",
            _ => "unknown",
        });
    }
}
