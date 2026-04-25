using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.User;

namespace Mnema.Providers.Managers.Publication;

internal class ImageIoWorker(ILogger<ImageIoWorker> logger, IImageService imageService) : IIoHandler
{
    public async Task HandleIoWork(string title, string id, IoWork ioWork, CancellationTokenSource tokenSource)
    {
        await using (ioWork.Stream)
        {
            if (tokenSource.IsCancellationRequested || !Path.Exists(ioWork.FilePath)) return;

            var realFileType = ioWork.Url.GetFileType();
            var fileType = ioWork.Preferences.ImageFormat.GetFileExtension(ioWork.Url);

            if (string.IsNullOrEmpty(fileType))
                fileType = ioWork.Format;

            var fileCounter = $"{ioWork.Idx}".PadLeft(4, '0');
            var filePath = Path.Join(ioWork.FilePath, $"page {fileCounter}{fileType}");

            var format = ioWork.Preferences.ImageFormat;
            if (ioWork.Preferences.ImageFormat == ImageFormat.Webp && realFileType == ".webp")
            {
                format = ImageFormat.Upstream;
            }

            await imageService.ConvertAndSave(ioWork.Stream, format, filePath, tokenSource.Token);

            logger.LogTrace("[{Title}/{Id}] Wrote {FilePath} / {Idx} to disk", title, id, filePath, ioWork.Idx);
        }
    }
}

internal class FileIoWorker(ILogger<FileIoWorker> logger, IFileSystem fileSystem) : IIoHandler
{
    public async Task HandleIoWork(string title, string id, IoWork ioWork, CancellationTokenSource tokenSource)
    {
        await using (ioWork.Stream)
        {
            if (tokenSource.IsCancellationRequested || !fileSystem.Path.Exists(ioWork.FilePath)) return;

            var realFileType = ioWork.Url.GetFileType();
            var filePath = ioWork.FilePath + realFileType;

            fileSystem.Directory.Delete(ioWork.FilePath, true);

            await using var file = fileSystem.File.Open(filePath, FileMode.Create);
            await ioWork.Stream.CopyToAsync(file);
        }
    }
}
