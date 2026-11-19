using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface IContentFileManager
{
    Task<DownloadFilesDto> GetFiles(string contentId, CancellationToken ct = default);
    Task UpdateFileMarkers(string contentId, IReadOnlyList<UpdateDownloadFileMarkersDto> files, CancellationToken ct = default);
}
