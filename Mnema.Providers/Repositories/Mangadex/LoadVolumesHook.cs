using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;
using Mnema.Providers.Managers.Publication;

namespace Mnema.Providers.Mangadex;

internal class LoadVolumesHook : IPreDownloadHook
{
    public async Task PreDownloadHook(Publication publication, IServiceScope scope, CancellationToken cancellationToken)
    {
        if (publication.Series == null) return;

        // Reset cover pointing to proxy
        publication.Series.CoverUrl = string.Empty;

        var mangadexRepository =
            (MangadexRepository)scope.ServiceProvider.GetRequiredKeyedService<IRepository>(Provider.Mangadex);

        var coverImages = await mangadexRepository.GetCoverImages(publication.Series.Id, cancellationToken);

        var requestLanguage = publication.Request.GetKey(RequestConstants.LanguageKey);
        var seriesLanguage = publication.Series.OriginalLanguage;

        var firstCover = SortedCovers().FirstOrDefault();
        var lastCover = SortedCovers().LastOrDefault();

        var coversByVolume = coverImages.Data
            .GroupBy(c => string.IsNullOrEmpty(c.Attributes.Volume) ? string.Empty : c.Attributes.Volume)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var chapter in publication.Series.Chapters)
        {
            if (!string.IsNullOrEmpty(chapter.VolumeMarker) &&
                coversByVolume.TryGetValue(chapter.VolumeMarker, out var covers))
            {
                var cover = covers.FirstOrDefault(LangFilter(requestLanguage))
                            ?? covers.FirstOrDefault(LangFilter(seriesLanguage))
                            ?? covers.FirstOrDefault();
                if (cover != null)
                {
                    chapter.CoverUrl = cover.Url(publication.Series.Id);
                    continue;
                }
            }

            switch (publication.Preferences.CoverFallbackMethod)
            {
                case CoverFallbackMethod.First:
                    chapter.CoverUrl = firstCover?.Url(publication.Series.Id);
                    break;
                case CoverFallbackMethod.Last:
                    chapter.CoverUrl = lastCover?.Url(publication.Series.Id);
                    break;
                case CoverFallbackMethod.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(publication.Preferences.CoverFallbackMethod));
            }
        }

        return;

        IOrderedEnumerable<CoverData> SortedCovers()
        {
            return coverImages.Data.OrderBy(c => c.Attributes.Volume.AsFloat(int.MaxValue))
                .ThenBy(c => c.Attributes.Locale == requestLanguage)
                .ThenBy(c => c.Attributes.Volume == seriesLanguage);
        }

        Func<CoverData, bool> LangFilter(string lang)
        {
            return data => string.IsNullOrEmpty(lang) || data.Attributes.Locale == lang;
        }
    }
}
