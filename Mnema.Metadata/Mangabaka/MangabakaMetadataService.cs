using System.Diagnostics;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Metadata;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Common.StringFormatter;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Mangabaka;

internal class MangabakaMetadataService(
    ILogger<MangabakaMetadataService> logger,
    IUnitOfWork unitOfWork,
    MangabakaDbContext ctx,
    [FromKeyedServices(key: MetadataProvider.Mangabaka)] SearcherManager searcherManager,
    IHttpClientFactory httpClientFactory,
    IDistributedCache cache
): IMangabakaMetadataService
{
    private HttpClient HttpClient => httpClientFactory.CreateClient(nameof(MetadataProvider.Mangabaka));

    private static readonly DistributedCacheEntryOptions CollectionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
    };

    private static readonly DistributedCacheEntryOptions WorksCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
    };

    public async Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var searcher = searcherManager.Acquire();
        try
        {
            var parser = new MultiFieldQueryParser(MangabakaScheduler.Version, MangabakaFields.TitleFields, MangabakaFields.PerFieldAnalyzer())
            {
                AllowLeadingWildcard = true,
                DefaultOperator = Operator.OR,
            };

            var query = BuildSearchQuery(parser, search);

            var hitsToFetch = (paginationParams.PageNumber + 1) * paginationParams.PageSize;
            var topDocs = searcher.Search(query, hitsToFetch);

            logger.LogDebug("Search for {@SearchOptions} - {Query} took {Elapsed}, found {Total} items - {@Pagination}",
                search, query, sw.Elapsed.ToReadableString(), topDocs.TotalHits, paginationParams);

            var scoreDocs = topDocs.ScoreDocs;
            var totalHits = topDocs.TotalHits;

            var results = scoreDocs
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToDictionary(d => int.Parse(searcher.Doc(d.Doc).Get(MangabakaFields.Id)), d => d.Score);

            var seriesData = await ctx.Series
                .Where(s => results.Keys.Contains(s.Id))
                .Where(s => s.MergedWith == null)
                .ToListAsync(cancellationToken);

            var monitoredSeriesById = (await unitOfWork.MonitoredSeriesRepository
                    .GetByMangaBakaIds(seriesData.Select(s => s.Id.ToString()).ToList(), cancellationToken))
                .GroupBy(s => s.MangaBakaId)
                .ToDictionary(s => s.Key, s => s.Select(m => m.Id).ToList());

            var preferences = await unitOfWork.SettingsRepository.GetPreferencesAsync(cancellationToken);
            var metadataPreferences = await unitOfWork.MetadataProviderSettingsRepository.GetMetadataProviderSettings(MetadataProvider.Mangabaka, cancellationToken);

            var sortedResults = seriesData
                .OrderByDescending(s => results[s.Id])
                .Select(s => ConvertToSeries(s, monitoredSeriesById, metadataPreferences, preferences, []))
                .ToList();

            return new PagedList<MetadataSearchResult>(sortedResults, totalHits, paginationParams.PageNumber, paginationParams.PageSize);
        }
        finally
        {
            searcherManager.Release(searcher);
        }
    }

    private static List<string> TokenizeWithAnalyzer(Analyzer analyzer, string field, string text)
    {
        var tokens = new List<string>();
        using var tokenStream = analyzer.GetTokenStream(field, text);
        var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();

        tokenStream.Reset();
        while (tokenStream.IncrementToken())
        {
            tokens.Add(termAttr.ToString());
        }
        tokenStream.End();

        return tokens;
    }

    private static BooleanQuery BuildSearchQuery(MultiFieldQueryParser parser, MetadataSearchDto search)
    {
        var query = new BooleanQuery();

        var isCjk = search.Query.Any(c => c >= 0x3000 && c <= 0xD7A3);
        if (isCjk)
        {
            var exactQuery = parser.Parse(QueryParserBase.Escape(search.Query.Trim()));
            exactQuery.Boost = 10;

            query.Add(exactQuery, Occur.SHOULD);
            return query;
        }

        var trimmed = search.Query.Trim().ToLowerInvariant();

        var analyzer = MangabakaFields.PerFieldAnalyzer();
        var terms = TokenizeWithAnalyzer(analyzer, MangabakaFields.Title, trimmed)
            .Select(QueryParserBase.Escape)
            .ToList();

        // Exact match, big boost
        var phraseQuery = new PhraseQuery { Boost = 10 };
        foreach (var term in terms.Where(t => !string.IsNullOrEmpty(t)))
        {
            phraseQuery.Add(new Term(MangabakaFields.Title, term));
        }
        query.Add(phraseQuery, Occur.SHOULD);

        // Fuzzy match, some spelling mistakes, medium boost
        var fuzzyBool = new BooleanQuery { Boost = 3 };
        foreach (var term in terms)
        {
            var maxEdits = term.Length > 5 ? 2 : 1;
            var fuzzy = new FuzzyQuery(new Term(MangabakaFields.Title, term), maxEdits);
            fuzzyBool.Add(fuzzy, Occur.SHOULD);
        }
        query.Add(fuzzyBool, Occur.SHOULD);

        // Wildcard terms match, no boost
        var termsQuery = new BooleanQuery();
        foreach (var term in terms)
            termsQuery.Add(new WildcardQuery(new Term(MangabakaFields.Title, $"{term}*")), Occur.MUST);
        query.Add(termsQuery, Occur.SHOULD);

        return query;
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken ct)
    {
        if (!int.TryParse(externalId, out var seriesId))
            return null;

        var preferences = await unitOfWork.SettingsRepository.GetPreferencesAsync(ct);
        var metadataPreferences = await unitOfWork.MetadataProviderSettingsRepository.GetMetadataProviderSettings(MetadataProvider.Mangabaka, ct);

        var series = await ctx.Series.FirstOrDefaultAsync(s => s.Id == seriesId, ct);
        if (series == null) return null;

        var monitoredSeriesById = (await unitOfWork.MonitoredSeriesRepository
                .GetByMangaBakaIds([series.Id.ToString()], ct))
            .GroupBy(s => s.MangaBakaId)
            .ToDictionary(s => s.Key, s => s.Select(m => m.Id).ToList());

        List<Chapter> chapters = [];
        if (metadataPreferences.Chapters)
        {
            chapters = await GetChapters(series, metadataPreferences, ct);
        }

        return ConvertToSeries(series, monitoredSeriesById, metadataPreferences, preferences, chapters);
    }

    private async Task<List<Chapter>> GetChapters(MangabakaSeries series, MetadataProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var collections = await GetCollections(series.Id, cancellationToken);
        var collection = SelectCollection(collections,
            settings.GetKey(MangaBakaMetadataConfiguration.CollectionMedium), series.NativeLanguage);

        if (collection == null)
            return [];

        var works = await GetWorks(collection.Id, cancellationToken);

        return works
            .Select(work => ToChapter(work, collection))
            .ToList();
    }

    private async Task<List<MangabakaCollection>> GetCollections(int seriesId, CancellationToken cancellationToken)
    {
        var collections = new List<MangabakaCollection>();
        var pagination = new Pagination
        {
            Next = $"/v1/series/{seriesId}/collections?page=1&limit=50"
        };

        while (!string.IsNullOrEmpty(pagination?.Next))
        {
            var response = await HttpClient.GetCachedAsync<PaginatedResponse<MangabakaCollection>>(pagination.Next,
                cache, CollectionCacheOptions, cancellationToken);

            if (response.IsErr)
            {
                logger.LogWarning("Failed to load Mangabaka collections for series {SeriesId}: {Error}", seriesId,
                    response.Error?.Message);
                return collections;
            }

            var paginatedResponse = response.Unwrap();
            if (paginatedResponse.Status != 200)
            {
                logger.LogWarning("Failed to load Mangabaka collections for series {SeriesId}: {Status} - {Reason}",
                    seriesId, paginatedResponse.Status, paginatedResponse.Reason);
                return collections;
            }

            if (paginatedResponse.Data != null)
                collections.AddRange(paginatedResponse.Data);

            pagination = paginatedResponse.Pagination;

            if (!string.IsNullOrEmpty(pagination?.Next))
                await Task.Delay(100, cancellationToken);
        }

        return collections;
    }

    private async Task<List<MangabakaWork>> GetWorks(string collectionId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetCachedAsync<MangabakaCollectionFullResponse>(
            $"/v1/collections/{collectionId}/full", cache, WorksCacheOptions, cancellationToken);

        if (response.IsErr)
        {
            logger.LogWarning("Failed to load Mangabaka works for collection {CollectionId}: {Error}", collectionId,
                response.Error?.Message);
            return [];
        }

        var fullResponse = response.Unwrap();
        if (fullResponse.Status != 200)
        {
            logger.LogWarning("Failed to load Mangabaka works for collection {CollectionId}: {Status}", collectionId,
                fullResponse.Status);
            return [];
        }

        return fullResponse.Data?.Works ?? [];
    }

    internal static MangabakaCollection? SelectCollection(IEnumerable<MangabakaCollection> collections,
        MangabakaCollectionMedium medium, string? nativeLanguage)
    {
        return collections
            .Where(c => c.CountMain + c.CountExtra + c.CountOther > 0)
            .OrderBy(c => MediumRank(c.Medium, medium))
            .ThenBy(c => TypeRank(c.Type))
            .ThenBy(c => LanguageRank(c.Language?.Iso, nativeLanguage))
            .ThenByDescending(c => c.CountMain + c.CountExtra + c.CountOther)
            .FirstOrDefault();
    }

    internal static Chapter ToChapter(MangabakaWork work, MangabakaCollection collection)
    {
        var volumeMarker = collection.Type switch
        {
            "volume" => work.SequenceString ?? string.Empty,
            "chapter" => work.PartOfVolume ?? string.Empty,
            "one-shot" => string.Empty,
            _ => work.PartOfVolume ?? work.SequenceString ?? string.Empty,
        };

        var chapterMarker = collection.Type == "chapter" ? work.SequenceString ?? string.Empty : string.Empty;

        var image = work.Images?.FirstOrDefault(i => i.Type == "volume");
        var imageFormat = image?.Image?.RawImage?.Format;

        return new Chapter
        {
            Id = work.Id,
            Title = work.SubTitle ?? string.Empty,
            Summary = work.Description?.Desc ?? string.Empty,
            VolumeMarker = volumeMarker,
            ChapterMarker = chapterMarker,
            SortOrder = work.SequenceNumeric,
            ReleaseDate = work.ReleaseDate is { } releaseDate
                ? DateTime.SpecifyKind(releaseDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                : null,
            CoverUrl = image?.Image?.RawImage?.Url,
            CoverFileFormat = string.IsNullOrEmpty(imageFormat) ? null : "." + imageFormat,
            Isbn = work.Identifiers?.FirstOrDefault(i => i.Name == "isbn")?.Id,
            Tags = [],
            People = [],
            TranslationGroups = [],
        };
    }

    private static int MediumRank(string? medium, MangabakaCollectionMedium preference)
    {
        var isPhysical = medium is "paperback" or "hardcover";
        var isDigital = medium == "digital";

        return preference switch
        {
            MangabakaCollectionMedium.PreferPhysical when isPhysical => 0,
            MangabakaCollectionMedium.PreferPhysical when isDigital => 1,
            MangabakaCollectionMedium.PreferDigital when isDigital => 0,
            MangabakaCollectionMedium.PreferDigital when isPhysical => 1,
            MangabakaCollectionMedium.Any => 0,
            _ => 2,
        };
    }

    private static int TypeRank(string? type)
    {
        return type switch
        {
            "volume" => 0,
            "chapter" => 1,
            "one-shot" => 2,
            _ => 3,
        };
    }

    private static int LanguageRank(string? language, string? nativeLanguage)
    {
        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (!string.IsNullOrEmpty(nativeLanguage) &&
            string.Equals(language, nativeLanguage, StringComparison.OrdinalIgnoreCase))
            return 1;

        return 2;
    }

    public async Task<List<Cover>> GetCovers(string externalId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(externalId, out var seriesId))
            return [];

        var series = await ctx.Series.FirstOrDefaultAsync(s => s.Id == seriesId, cancellationToken);
        if (series == null) return [];

        List<MangabakaCover> covers = [];

        var pagination = new Pagination
        {
            Next = $"/v1/series/{externalId}/images?page=1&limit=50"
        };

        while (!string.IsNullOrEmpty(pagination?.Next))
        {
            var response = await HttpClient.GetCachedAsync<PaginatedResponse<MangabakaCover>>(pagination.Next,
                cache, cancellationToken: cancellationToken);

            if (response.IsErr)
                throw new MnemaException($"Failed to load covers: {response.Error?.Message}", response.Error);

            var paginatedResponse = response.Unwrap();

            if (paginatedResponse.Status != 200)
                throw new MnemaException($"Failed to load covers: {paginatedResponse.Status} - {paginatedResponse.Reason}");

            pagination = paginatedResponse.Pagination;

            if (paginatedResponse.Data != null)
                covers.AddRange(paginatedResponse.Data.Where(cover => cover.Type == "volume"));

            if (!string.IsNullOrEmpty(pagination?.Next))
                await Task.Delay(100, cancellationToken);
        }

        var languages = series.Titles?
            .Where(t => t.Traits.Contains("native"))
            .Select(t => t.Language)
            .Distinct().ToList() ?? [];

        return covers.GroupBy(c => c.Type + c.Index).Select(g =>
        {
            var preferredCover = g
                .OrderByDescending(cover => cover.Language == "en")
                .ThenByDescending(cover => languages.Contains(cover.Language))
                .FirstOrDefault();

            if (preferredCover == null)
                return null;

            return new Cover
            {
                Extension = preferredCover.Image.RawImage.Format,
                Url = preferredCover.Image.RawImage.Url,
                Volume = preferredCover.Index,
            };
        }).WhereNotNull().ToList();
    }

    private static MetadataSearchResult ConvertToSeries(MangabakaSeries series, Dictionary<string, List<Guid>> monitoredSeriesIds, MetadataProviderSettings settings, Preferences preferences, IList<Chapter> chapters)
    {
        var publishers = series.Publishers?
            .Where(p => p.Type == MangabakaPublisher.Original || p.Type == MangabakaPublisher.English)
            .Select(p => Person.Create(p.Name, PersonRole.Publisher)) ?? [];
        var writers = series.Authors?
            .Select(p => Person.Create(p, PersonRole.Writer)) ?? [];
        var artists = series.Artists?
            .Select(p => Person.Create(p, PersonRole.Colorist)) ?? [];

        var contentRating = series.TagsV2?
            .Select(t => (MangabakaContentRating?)t.ContentRating)
            .Append(series.ContentRating)
            .DefaultIfEmpty(null)
            .Max();

        return new MetadataSearchResult
        {
            Id = series.Id.ToString(),
            MonitoredSeriesId = monitoredSeriesIds.GetValueOrDefault(series.Id.ToString()) ?? [],
            Title = FindTitleByPriority(series, settings.GetKey(MangaBakaMetadataConfiguration.SeriesNameLanguagePriority).ToList()),
            LocalizedSeries = FindTitleByPriority(series, settings.GetKey(MangaBakaMetadataConfiguration.LocalizedSeriesNameLanguagePriority).ToList(), true),
            Summary = series.Description ?? string.Empty,
            Status = FromMangabakaPublicationStatus(series.Status),
            RefUrl = $"https://mangabaka.org/{series.Id}",
            Tags = series.TagsV2?
                .Where(t => t.Weight < settings.GetKey(MangaBakaMetadataConfiguration.MinimumTagWeight))
                .Select(t => new Tag(t.Name, t.IsGenre))
                .ToList() ?? [],
            AgeRating = FromMangaBakaContentRating(contentRating),
            People = publishers.Concat(writers).Concat(artists).ToList(),
            Links = CollectLinks(series, preferences),
            CoverUrl = series.CoverX350X3,
            Year = series.StartDate?.Year,
            HighestVolumeNumber = series.Status.HasFinalCount() ? series.FinalVolume.AsFloat() : null,
            HighestChapterNumber = series.Status.HasFinalCount() ? series.FinalChapter.AsFloat() : null,
            Chapters = chapters,
            ContentFormat = series.Type switch
            {
                MangabakaType.Manga or MangabakaType.Manhua or MangabakaType.Manhwa => ContentFormat.Manga,
                MangabakaType.Novel => ContentFormat.LightNovel,
                _ => null
            }
        };
    }

    public Task<int?> FindMangabakaSeries(int? aniListId, string? malId, CancellationToken cancellationToken)
    {
        return ctx.Series
            .Where(s => (aniListId != null && s.SourceAnilistId == aniListId) ||
                        (malId != null && s.SourceMyAnimeListId == malId))
            .Select(s => (int?) s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public IStringFormatter<string> NativeLanguageFormatter => NativeLanguagePlaceholder;

    private static readonly StringFormatter<string> NativeLanguagePlaceholder = new StringFormatter<string>()
        .WithVariable("Native", s => s);

    internal static List<string> CollectLinks(MangabakaSeries series, Preferences preferences)
    {
        var nativeLanguage = series.NativeLanguage;

        // Resolve {Native} into copies instead of the preference itself: one Preferences instance is shared
        // by every series in a search, so rewriting the value would pin the filter to the first series seen.
        var filters = preferences.LinkFilters
            .Select(f => f.Type == LinkFilterType.Language && !string.IsNullOrEmpty(nativeLanguage)
                ? new LinkFilter(f.Mode, f.Type, NativeLanguagePlaceholder.Apply(f.Value, nativeLanguage))
                : f)
            .ToList();

        var links = (series.LinksV2 ?? [])
            .Where(link => LinkFilter.IsAllowed(link, filters))
            .Select(link => link.Url)
            .ToList();

        if (series.SourceAnilistId != null && LinkFilter.IsHostnameAllowed("anilist.co", filters))
        {
            links.Add($"https://anilist.co/manga/{series.SourceAnilistId}");
        }

        if (!string.IsNullOrEmpty(series.SourceMyAnimeListId) &&
            LinkFilter.IsHostnameAllowed("myanimelist.net", filters))
        {
            links.Add($"https://myanimelist.net/manga/{series.SourceMyAnimeListId}");
        }

        if (!string.IsNullOrEmpty(series.SourceMangaUpdatesId) &&
            LinkFilter.IsHostnameAllowed("www.mangaupdates.com", filters))
        {
            links.Add($"https://www.mangaupdates.com/series/{series.SourceMangaUpdatesId}");
        }

        return links;
    }

    /// <summary>
    /// Resolves the title based on a comma-separated language priority list.
    /// Example setting string: "en, {Native}, ja-latn, fr"
    /// </summary>
    public static string FindTitleByPriority(MangabakaSeries series, List<string> priorities, bool isLocalized = false)
    {
        var titles = series.Titles;
        if (titles == null || titles.Count == 0) return string.Empty;

        if (priorities.Count == 0)
        {
            return isLocalized ? titles.FindBestNativeTitle() : titles.FindBestTitle();
        }

        var nativeLanguage = series.NativeLanguage;
        if (!string.IsNullOrEmpty(nativeLanguage))
            priorities = priorities.Select(p => NativeLanguagePlaceholder.Apply(p, nativeLanguage)).ToList();

        foreach (var priority in priorities)
        {
            var matchingTitle = titles
                .Where(t => string.Equals(t.Language, priority, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.IsPrimary)
                .FirstOrDefault();

            if (matchingTitle != null)
            {
                return matchingTitle.Title;
            }
        }

        return isLocalized ? titles.FindBestNativeTitle() : titles.FindBestTitle();
    }

    private static PublicationStatus FromMangabakaPublicationStatus(MangabakaPublicationStatus publicationStatus)
    {
        return publicationStatus switch
        {
            MangabakaPublicationStatus.Completed => PublicationStatus.Completed,
            MangabakaPublicationStatus.Releasing => PublicationStatus.Ongoing,
            MangabakaPublicationStatus.Cancelled => PublicationStatus.Cancelled,
            MangabakaPublicationStatus.Hiatus => PublicationStatus.Paused,
            MangabakaPublicationStatus.Upcoming => PublicationStatus.Ongoing, // Close enough
            MangabakaPublicationStatus.Unknown => PublicationStatus.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(publicationStatus), publicationStatus, null)
        };
    }

    private static AgeRating FromMangaBakaContentRating(MangabakaContentRating? contentRating)
    {
        return contentRating switch
        {
            MangabakaContentRating.Safe => AgeRating.Everyone,
            MangabakaContentRating.Suggestive => AgeRating.Mature15Plus,
            MangabakaContentRating.Erotica => AgeRating.Mature17Plus,
            MangabakaContentRating.Pornographic => AgeRating.R18Plus,
            null => AgeRating.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(contentRating), contentRating, null)
        };
    }
}
