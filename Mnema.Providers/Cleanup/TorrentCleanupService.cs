using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Providers.QBit;

namespace Mnema.Providers.Cleanup;

internal class TorrentCleanupService(
    ILogger<TorrentCleanupService> logger,
    INamingService namingService,
    IScannerService scannerService,
    IParserService parserService,
    IFileSystem fileSystem,
    ApplicationConfiguration configuration
    ): ICleanupService
{
    public Task Cleanup(IContent content)
    {
        if (content is not QBitTorrent torrent)
            throw new MnemaException($"{nameof(PublicationCleanupService)} cannot cleanup {content.GetType()}");

        logger.LogDebug("[{Title}/{Id}] Cleaning up torrent", content.Title, content.Id);

        var request = content.Request;

        var downloadDir = content.DownloadDir;

        var title = request.Metadata.GetString(RequestConstants.TitleOverride)
            .OrNonEmpty(parserService.ParseSeries(torrent.Title, ContentFormat.Manga), request.TempTitle);
        var destDir = fileSystem.Path.Join(configuration.BaseDir, request.BaseDir, title);

        if (!fileSystem.Directory.Exists(destDir))
            fileSystem.Directory.CreateDirectory(destDir);

        var files = fileSystem.Directory.GetFiles(downloadDir, "*", SearchOption.AllDirectories);
        foreach (var fullFile in files)
        {
            var file = fileSystem.Path.GetFileName(fullFile);
            var ext = fileSystem.Path.GetExtension(file);

            if (!parserService.FileExtensionsForFormat(Format.Archive).IsMatch(ext))
                continue;

            var volume = parserService.ParseVolume(file, ContentFormat.Manga);
            var chapter = parserService.ParseChapter(file, ContentFormat.Manga);

            volume = parserService.IsLooseLeafVolume(volume) ? null : volume;
            chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter;

            var chapterFileName = namingService.GetChapterFileName(
                title,
                volume,
                chapter,
                chapter.AsFloat(),
                string.IsNullOrEmpty(chapter) && string.IsNullOrEmpty(volume),
                null,
                []
            );


            var destPath = fileSystem.Path.Join(configuration.BaseDir, request.BaseDir, title, chapterFileName + ext);

            fileSystem.File.Copy(fullFile, destPath, true);
        }

        return Task.CompletedTask;
    }
}
