using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Models.Entities.User;
using NetVips;

namespace Mnema.Services;

public class ImageService : IImageService
{
    public async Task ConvertAndSave(Stream stream, ImageFormat format, string filePath, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        switch (format)
        {
            case ImageFormat.Upstream:
            {
                await using var file = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1024 * 64,
                    useAsync: true
                );

                await stream.CopyToAsync(file, cancellationToken);
                break;
            }

            case ImageFormat.Webp:
            {
                using var image = Image.NewFromStream(stream, access: Enums.Access.Sequential);
                if (cancellationToken.IsCancellationRequested) return;

                image.Webpsave(filePath, lossless: true, q: 80);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    public async Task Convert(Stream stream, ImageFormat format, Stream outputStream)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        switch (format)
        {
            case ImageFormat.Upstream:
                await stream.CopyToAsync(outputStream);
                break;
            case ImageFormat.Webp:
            {
                using var image = Image.NewFromStream(stream);

                image.WebpsaveStream(outputStream, lossless: true, q: 80);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }
}

