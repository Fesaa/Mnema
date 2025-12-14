using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.StaticFiles;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

public class ProxyController(ILogger<ProxyController> logger, IHttpClientFactory httpClientFactory): BaseApiController
{

    private static readonly FileExtensionContentTypeProvider FileTypeProvider = new ();

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
            logger.LogError(ex, "Failed to get mangadex cover @ {Url}", url);

            throw new MnemaException("Failed to get image from mangadex", ex);
        }
    }

}