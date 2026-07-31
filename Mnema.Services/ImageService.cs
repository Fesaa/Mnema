using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using NetVips;

namespace Mnema.Services;

internal sealed record ImageConversionSettings(bool Lossless, int Quality);

public class ImageService(ILogger<ImageService> logger, ISettingsService settingsService): IImageService
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
                var settings = await GetImageConversionSettings(cancellationToken);

                using var image = Image.NewFromStream(stream, access: Enums.Access.Sequential);
                if (cancellationToken.IsCancellationRequested) return;

                image.Webpsave(filePath, lossless: settings.Lossless, q: settings.Quality);
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
                var settings = await GetImageConversionSettings();

                using var image = Image.NewFromStream(stream);

                image.WebpsaveStream(outputStream, lossless: settings.Lossless, q: settings.Quality);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private ImageConversionSettings? _settings;
    private readonly SemaphoreSlim _settingsLock = new(1, 1);

    private async Task<ImageConversionSettings> GetImageConversionSettings(
        CancellationToken cancellationToken = default)
    {
        if (_settings != null)
            return _settings;

        using var _ = await _settingsLock.LockAsync(cancellationToken);

        if (_settings != null)
            return _settings;

        var lossless = await settingsService.GetSettingsAsync<bool>(
            ServerSettingKey.ImageConversionLossLess);

        var quality = await settingsService.GetSettingsAsync<int>(
            ServerSettingKey.ImageConversionQuality);

        _settings = new ImageConversionSettings(lossless, quality);
        return _settings;
    }
}

