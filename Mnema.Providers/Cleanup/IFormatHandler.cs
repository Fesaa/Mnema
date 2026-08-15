using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.External;

namespace Mnema.Providers.Cleanup;

internal record FormatHandlerContext(
    string SourceFile,
    string DestinationPath,
    string? CoverUrl,
    ComicInfo? ComicInfo,
    Preferences Preferences,
    DownloadRequestDto Request
);

internal interface IFormatHandler
{
    Format SupportedFormat { get; }
    Task HandleAsync(FormatHandlerContext context);
}
