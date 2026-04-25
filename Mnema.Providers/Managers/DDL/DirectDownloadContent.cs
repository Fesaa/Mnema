using Mnema.API.Content;
using Mnema.Models.DTOs.Content;

namespace Mnema.Providers.Managers.DDL;

public class DirectDownloadContent: IContent
{
    public string Id { get; }
    public string Title { get; }
    public string DownloadDir { get; }
    public ContentState State { get; }
    public DownloadRequestDto Request { get; }
    public DownloadInfo DownloadInfo { get; }
}
