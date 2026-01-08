using System;
using System.IO;
using Mnema.API;
using Mnema.Models.Entities.User;
using NetVips;

namespace Mnema.Services;

public class ImageService: IImageService
{
    public Stream ConvertFromStream(Stream stream, ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Upstream => stream,
            ImageFormat.Webp => ConvertToWebp(stream),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

    }

    private static MemoryStream ConvertToWebp(Stream stream)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        using var image = Image.NewFromStream(stream);

        var output = new MemoryStream();

        image.WebpsaveStream(output, lossless: true, q: 80);

        if (output.CanSeek)
            output.Position = 0;

        return output;
    }
}
