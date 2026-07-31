using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface IPublicationManager : IContentManager
{
    Task<IPublication?> GetPublicationById(string id);
    Task MoveToDownloadQueue(string id);
}

public interface IPublication : IContent
{
    Task Cancel();
    Task Cleanup();
    Task<MessageDto> ProcessMessage(MessageDto message);
    Task LoadMetadataAsync(CancellationTokenSource source);
    Task DownloadContentAsync(CancellationTokenSource source);
}

public class OnDiskContent: IHasPositionMarkers
{
    public string SeriesName { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public string? Chapter { get; set; }
    public string? Volume { get; set; }

    public string VolumeMarker => Volume ?? string.Empty;
    public string ChapterMarker => Chapter ?? string.Empty;
}
