using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.Enums;

namespace Mnema.Providers.Cleanup;

internal class EpubFormatHandler(ILogger<EpubFormatHandler> logger, IFileSystem fileSystem, IEpubMetadataService epubMetadataService): IFormatHandler
{

    public Format SupportedFormat => Format.Epub;
    public async Task HandleAsync(FormatHandlerContext context)
    {
        if (fileSystem.File.Exists(context.DestinationPath))
            fileSystem.File.Delete(context.DestinationPath);

        fileSystem.File.Copy(context.SourceFile, context.DestinationPath);

        if (context.ComicInfo == null) return;

        await epubMetadataService.WriteComicInfo(context.ComicInfo, context.DestinationPath, CancellationToken.None);
    }
}
