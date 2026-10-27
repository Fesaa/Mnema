using System;
using System.Collections.Generic;
using System.Linq;

namespace Mnema.Providers.Managers.QBit;

internal sealed class AddTorrentUrlsRequest
{
    public AddTorrentUrlsRequest(Uri url)
    {
        TorrentUrls = [url];
    }

    public AddTorrentUrlsRequest(IEnumerable<Uri> urls)
    {
        TorrentUrls = urls.ToList();
    }

    public IReadOnlyList<Uri> TorrentUrls { get; }
    public string? DownloadFolder { get; init; }
    public string? Category { get; init; }
    public IEnumerable<string>? Tags { get; init; }
    public bool Paused { get; init; }
}
