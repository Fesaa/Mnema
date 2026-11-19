using System.Collections.Generic;

namespace Mnema.Models.DTOs.Content;

public sealed class DownloadFilesDto
{
    public required List<ExternalDownloadFileDto> Files { get; init; }
    public List<MonitoredChapterOptionDto>? MonitoredChapters { get; init; }
}
