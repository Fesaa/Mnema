using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Enums;

namespace Mnema.Services;

internal sealed partial class UrlParserService(IServiceProvider serviceProvider) : IUrlParserService
{
    public async Task<ParsedSeriesUrlDto?> ParseUrl(string url, CancellationToken cancellationToken = default)
    {
        var target = ParseTarget(url);
        if (target is null) return null;

        return target.MetadataProvider is { } metadataProvider
            ? await ResolveMetadataAsync(metadataProvider, target.ExternalId, cancellationToken)
            : await ResolveContentAsync(target.Provider!.Value, target.ExternalId, cancellationToken);
    }

    private async Task<ParsedSeriesUrlDto?> ResolveMetadataAsync(MetadataProvider metadataProvider, string externalId,
        CancellationToken cancellationToken)
    {
        var metadataService = serviceProvider.GetKeyedService<IMetadataProviderService>(metadataProvider);
        if (metadataService is null) return null;

        var series = await metadataService.GetSeries(externalId, cancellationToken);
        if (series is null) return null;

        return new ParsedSeriesUrlDto
        {
            MetadataProvider = metadataProvider,
            ExternalId = series.Id,
            SeriesName = series.Title,
            Description = series.Summary,
        };
    }

    private async Task<ParsedSeriesUrlDto?> ResolveContentAsync(Provider provider, string externalId,
        CancellationToken cancellationToken)
    {
        var repository = serviceProvider.GetKeyedService<IRepository>(provider);
        if (repository is null) return null;

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.AllowPartialChapterData, true);

        var series = await repository.SeriesInfo(new DownloadRequestDto
        {
            Provider = provider,
            Id = externalId,
            BaseDir = string.Empty,
            TempTitle = string.Empty,
            Metadata = metadata,
        }, cancellationToken);

        return new ParsedSeriesUrlDto
        {
            Provider = provider,
            ExternalId = string.IsNullOrEmpty(series.Id) ? externalId : series.Id,
            SeriesName = series.Title,
            Description = series.Summary,
        };
    }

    private static UrlTarget? ParseTarget(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        url = url.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;

        var host = uri.Host.ToLowerInvariant();
        var path = uri.AbsolutePath;

        if (host is "mangadex.org" or "www.mangadex.org")
        {
            var match = MangadexPathRegex().Match(path);
            return match.Success
                ? new UrlTarget(Provider.Mangadex, null, match.Groups["id"].Value)
                : null;
        }

        if (host is "dynasty-scans.com" or "www.dynasty-scans.com")
        {
            var match = DynastyPathRegex().Match(path);
            return match.Success
                ? new UrlTarget(Provider.Dynasty, null, $"/series/{match.Groups["slug"].Value}")
                : null;
        }

        if (host == "webtoons.com" || host.EndsWith(".webtoons.com", StringComparison.Ordinal))
        {
            return ParseWebtoons(uri);
        }

        if (host is "mangabaka.org" or "www.mangabaka.org")
        {
            var match = MangabakaPathRegex().Match(path);
            return match.Success
                ? new UrlTarget(null, MetadataProvider.Mangabaka, match.Groups["id"].Value)
                : null;
        }

        if (host is "hardcover.app" or "www.hardcover.app")
        {
            return ParseHardcover(path);
        }

        return null;
    }

    private static UrlTarget? ParseWebtoons(Uri uri)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4 || !segments[0].Equals("en", StringComparison.OrdinalIgnoreCase)) return null;

        var isList = segments.Length == 4 && segments[3].Equals("list", StringComparison.OrdinalIgnoreCase);
        var isEpisode = segments.Length == 5
                        && segments[3].StartsWith("episode-", StringComparison.OrdinalIgnoreCase)
                        && segments[4].Equals("viewer", StringComparison.OrdinalIgnoreCase);

        if (!isList && !isEpisode) return null;

        var titleNo = WebtoonsTitleNoRegex().Match(uri.Query);
        if (!titleNo.Success) return null;

        return new UrlTarget(Provider.Webtoons, null,
            $"en/{segments[1]}/{segments[2]}/list?title_no={titleNo.Groups["id"].Value}");
    }

    private static UrlTarget? ParseHardcover(string path)
    {
        var match = HardcoverIdRegex().Match(path);
        if (match.Success)
            return new UrlTarget(null, MetadataProvider.Hardcover, match.Groups["id"].Value);

        match = HardcoverEditIdRegex().Match(path);
        if (match.Success)
            return new UrlTarget(null, MetadataProvider.Hardcover, match.Groups["id"].Value);

        match = HardcoverSlugRegex().Match(path);
        return match.Success
            ? new UrlTarget(null, MetadataProvider.Hardcover, match.Groups["slug"].Value)
            : null;
    }

    [GeneratedRegex(@"^/title/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:/|$)")]
    private static partial Regex MangadexPathRegex();

    [GeneratedRegex(@"^/series/(?<slug>[^/]+)/?$")]
    private static partial Regex DynastyPathRegex();

    [GeneratedRegex(@"[?&]title_no=(?<id>\d+)")]
    private static partial Regex WebtoonsTitleNoRegex();

    [GeneratedRegex(@"^/(?:[^/]+/)?(?<id>\d+)(?:/|$)")]
    private static partial Regex MangabakaPathRegex();

    [GeneratedRegex(@"^/id/series/(?<id>\d+)(?:/|$)")]
    private static partial Regex HardcoverIdRegex();

    [GeneratedRegex(@"^/series/(?<id>\d+)(?:/edit)?/?$")]
    private static partial Regex HardcoverEditIdRegex();

    [GeneratedRegex(@"^/series/(?<slug>[^/]+)/?$")]
    private static partial Regex HardcoverSlugRegex();

    private sealed record UrlTarget(Provider? Provider, MetadataProvider? MetadataProvider, string ExternalId);
}
