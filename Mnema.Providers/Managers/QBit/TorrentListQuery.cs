using System.Collections.Generic;

namespace Mnema.Providers.Managers.QBit;

internal sealed class TorrentListQuery
{
    public TorrentListFilter Filter { get; init; } = TorrentListFilter.All;
    public string? Category { get; init; }
    public string? SortBy { get; init; }
    public bool ReverseSort { get; init; }
    public int? Limit { get; init; }
    public int? Offset { get; init; }
    public IEnumerable<string>? Hashes { get; init; }
    public string? Tag { get; init; }
}
