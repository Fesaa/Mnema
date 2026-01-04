using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Server.Configuration;

namespace Mnema.Server.Controllers;

public class ProxyController(ILogger<ProxyController> logger, IHttpClientFactory httpClientFactory) : BaseApiController
{
    private static readonly FileExtensionContentTypeProvider FileTypeProvider = new();

    [HttpGet("mangadex/covers/{id}/{fileName}")]
    [OutputCache(PolicyName = CacheProfiles.OneWeek, VaryByRouteValueNames = ["id", "fileName"])]
    public async Task<IActionResult> GetMangadexCover(string id, string fileName)
    {
        FileTypeProvider.TryGetContentType(fileName, out var contentType);

        var url = $"https://uploads.mangadex.org/covers/{id}/{fileName}";
        var client = httpClientFactory.CreateClient(nameof(Provider.Mangadex));

        try
        {
            var response = await client.GetStreamAsync(url);

            return File(response, contentType ?? "image/jpeg");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get mangadex cover @ {Url}", url.CleanForLogging());

            // TODO: Use fallback image
            throw new MnemaException("Failed to get image from mangadex", ex);
        }
    }

    [HttpGet("webtoon/covers/{date}/{id}/{fileName}")]
    [OutputCache(PolicyName = CacheProfiles.OneWeek, VaryByRouteValueNames = ["id", "fileName", "date"])]
    public async Task<IActionResult> GetWebtoonCover(string id, string fileName, string date)
    {
        FileTypeProvider.TryGetContentType(fileName, out var contentType);

        var url = $"{SharedConstants.WebtoonImageBase}{date}/{id}/{fileName}";
        var client = httpClientFactory.CreateClient(nameof(Provider.Webtoons));

        try
        {
            var response = await client.GetStreamAsync(url);

            return File(response, contentType ?? "image/jpeg");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get webtoon cover @ {Url}", url.CleanForLogging());

            // TODO: Use fallback image
            throw new MnemaException("Failed to get image from webtoon", ex);
        }
    }
}