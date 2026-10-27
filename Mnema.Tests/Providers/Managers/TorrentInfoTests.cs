using System;
using System.Collections.Generic;
using System.Text.Json;
using Mnema.Providers.Managers.QBit;
using Xunit;

namespace Mnema.Tests.Providers.Managers;

public class TorrentInfoTests
{
    [Theory]
    [InlineData("uploading", "Uploading")]
    [InlineData("stoppedUP", "PausedUpload")]
    [InlineData("pausedUP", "PausedUpload")]
    [InlineData("stoppedDL", "PausedDownload")]
    [InlineData("madeUpState", "Unknown")]
    public void Deserialize_MapsState(string state, string expected)
    {
        var json = $$"""{"hash":"abc","state":"{{state}}"}""";

        var torrent = JsonSerializer.Deserialize<TorrentInfo>(json);

        Assert.NotNull(torrent);
        Assert.Equal(Enum.Parse<TorrentState>(expected), torrent.State);
    }

    [Fact]
    public void Deserialize_MapsEtaToEstimatedTime()
    {
        var json = """{"hash":"abc","state":"downloading","eta":90}""";

        var torrent = JsonSerializer.Deserialize<TorrentInfo>(json);

        Assert.NotNull(torrent);
        Assert.Equal(90, torrent.EstimatedTime?.TotalSeconds);
    }

    [Theory]
    [InlineData(0, "Skip")]
    [InlineData(1, "Minimal")]
    [InlineData(6, "VeryHigh")]
    public void Deserialize_MapsFilePriority(int priority, string expected)
    {
        var json = $$"""[{"index":0,"name":"vol1.cbz","priority":{{priority}}}]""";

        var contents = JsonSerializer.Deserialize<List<TorrentContent>>(json);

        Assert.NotNull(contents);
        Assert.Equal(Enum.Parse<TorrentContentPriority>(expected), contents[0].Priority);
    }
}
