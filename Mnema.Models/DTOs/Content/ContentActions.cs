using System.Collections.Generic;

namespace Mnema.Models.DTOs.Content;

public static class ContentActions
{
    private static readonly IReadOnlyList<ContentAction> None = [];

    private static readonly IReadOnlyList<ContentAction> StopOnly =
        [ContentAction.Stop];

    private static readonly IReadOnlyList<ContentAction> BrowseAndStop =
        [ContentAction.Browse, ContentAction.Stop];

    private static readonly IReadOnlyList<ContentAction> WaitingTorrent =
        [ContentAction.EditMarkers, ContentAction.SetContent, ContentAction.MarkReady, ContentAction.Stop];

    private static readonly IReadOnlyList<ContentAction> WaitingTorrentWithoutFiles =
        [ContentAction.SetContent, ContentAction.MarkReady, ContentAction.Stop];

    public static IReadOnlyList<ContentAction> ForTorrent(ContentState state, bool hasFiles) => state switch
    {
        ContentState.Waiting => hasFiles ? WaitingTorrent : WaitingTorrentWithoutFiles,
        ContentState.Downloading => BrowseAndStop,
        ContentState.Cleanup => None,
        _ => StopOnly,
    };

    public static IReadOnlyList<ContentAction> ForPublication(ContentState state) => state switch
    {
        ContentState.Downloading => BrowseAndStop,
        ContentState.Cleanup => None,
        _ => StopOnly,
    };
}
