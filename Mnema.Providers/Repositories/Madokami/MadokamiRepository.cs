using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using Flurl;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Providers.Extensions;

namespace Mnema.Providers.Repositories.Madokami;

internal class MadokamiRepository(IUnitOfWork unitOfWork, IParserService parserService, IHttpClientFactory clientFactory, IDistributedCache cache) : IRepository, IConfigurationProvider
{

    internal static readonly IMetadataKey<string> BasicAuthUsername = MetadataKeys.String("basicAuthUsername");
    internal static readonly IMetadataKey<string> BasicAuthPassword = MetadataKeys.String("basicAuthPassword");
    internal static readonly string BasicAuthCacheKey = "BasicAuth";

    private HttpClient Client => clientFactory.CreateClient(nameof(Provider.MadoKami));

    public  async Task<PagedList<SearchResult>> Search(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken)
    {
        var url = "search".SetQueryParam("q", request.Query);

        var result = await Client.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to search: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var resultNodes = document.DocumentNode.QuerySelectorAll("tbody td a");
        if (resultNodes == null) return PagedList<SearchResult>.Empty();

        var baseUrl = Client.BaseAddress?.ToString().TrimEnd('/');
        var all = resultNodes.ToList();

        var items = all
            .Skip(pagination.PageNumber * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(node => new SearchResult
        {
            Id = node.GetAttributeValue("href", string.Empty),
            Name = node.InnerText,
            Url = $"{baseUrl}{node.GetAttributeValue("href", string.Empty)}",
            Provider = Provider.MadoKami
        });

        return new PagedList<SearchResult>(items, all.Count, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IList<ContentRelease>> GetRecentlyUpdated(CancellationToken cancellationToken)
    {
        var result = await Client.GetCachedStringAsync("recent", cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve recently updated: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument();

        var nodes = document.DocumentNode.QuerySelectorAll("tbody td:first-child")?.ToList();
        if (nodes == null) return new List<ContentRelease>();

        return nodes.Select(node =>
        {
            var id = node.QuerySelector("a")?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(id)) return null;

            var text = node.InnerText.ReplaceLineEndings(string.Empty).Trim();

            return new ContentRelease
            {
                Provider = Provider.MadoKami,
                ReleaseId = text, // There is no real id we can use. This is hopefully sufficient
                ReleaseName = text,
                ContentId = id,
            };
        }).WhereNotNull().ToList();
    }

    public Task<List<FormControlDefinition>> DownloadMetadata(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
              Key  = RequestConstants.HardcoverSeriesIdKey.Key,
              Type = FormType.Text,
            },
            new FormControlDefinition
            {
                Key  = RequestConstants.MangaBakaKey.Key,
                Type = FormType.Text,
            },
            new FormControlDefinition
            {
                Key  = RequestConstants.IgnoreNonMatchedVolumes.Key,
                Type = FormType.Switch,
                Advanced = true,
            },
            new FormControlDefinition
            {
                Key  = RequestConstants.TitleOverride.Key,
                Type = FormType.Text,
                Advanced = true,
            },
            new FormControlDefinition
            {
                Key  = RequestConstants.DownloadOneShotKey.Key,
                Type = FormType.Switch,
                DefaultOption = "true",
                Advanced = true,
            },
            new FormControlDefinition
            {
                Key = RequestConstants.ContentFormatKey.Key,
                Type = FormType.DropDown,
                Options = Enum.GetValues<ContentFormat>()
                    .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                    .ToList(),
                DefaultOption = ContentFormat.Manga,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = RequestConstants.FormatKey.Key,
                Type = FormType.DropDown,
                Options = Enum.GetValues<Format>()
                    .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                    .ToList(),
                DefaultOption = Format.Archive,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = RequestConstants.HardcoverSeriesIdKey.Key,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = RequestConstants.MangaBakaKey.Key,
                Type = FormType.Text
            }
        ]);
    }

    public Task<List<FormControlDefinition>> Modifiers(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([]);
    }

    public async Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken)
    {
        var url = request.Id;
        var contentFormat = request.GetKey(RequestConstants.ContentFormatKey);

        var result = await Client.GetCachedStringAsync(url, cache, cancellationToken: cancellationToken);
        if (result.IsErr)
            throw new MnemaException($"Failed to retrieve series info: {result.Error?.Message}", result.Error);

        var document = result.Unwrap().ToHtmlDocument().DocumentNode;

        return new Series
        {
            Id = request.Id,
            Title = document.SelectSingleNode("/html/body/div[3]/div[2]/div[2]/div/h2/span[1]")?.InnerText ?? string.Empty,
            Summary = string.Empty,
            Status = PublicationStatus.Unknown,
            Tags = [],
            People = [],
            Links = [],
            Chapters = document.QuerySelectorAll("tbody tr td:first-child").Select(node =>
            {
                var id = node.QuerySelector("a")?.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(id) || !Path.HasExtension(id)) return null;

                var parseResult = parserService.FullParse(node.InnerText, contentFormat);

                return new Chapter
                {
                    Id = id,
                    Title = node.InnerText.ReplaceLineEndings(string.Empty).Trim().Trim('"'),
                    RefUrl = null,
                    VolumeMarker = parserService.IsLooseLeafVolume(parseResult.Volume.Value) ? string.Empty : parseResult.Volume.Value,
                    ChapterMarker = parseResult.Chapter.Value,
                    Tags = [],
                    People = [],
                    TranslationGroups = []
                };
            }).WhereNotNull().ToList()
        };
    }

    public Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken)
    {
        var baseUrl = Client.BaseAddress?.ToString().TrimEnd('/');
        return Task.FromResult<IList<DownloadUrl>>(new List<DownloadUrl>
        {
            new($"{baseUrl}{chapter.Id}", $"{baseUrl}{chapter.Id}")
        });
    }

    public Task<List<FormControlDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = BasicAuthUsername.Key ,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = BasicAuthPassword.Key ,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .Build()
            }
        ]);
    }

    public async Task ReloadConfiguration(CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(BasicAuthCacheKey, cancellationToken);
    }
}
