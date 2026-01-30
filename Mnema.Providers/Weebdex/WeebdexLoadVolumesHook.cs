using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Providers.Weebdex;

internal class WeebdexLoadVolumesHook : IPreDownloadHook
{
    public async Task PreDownloadHook(Publication publication, IServiceScope scope, CancellationToken cancellationToken)
    {
        if (publication.Series == null) return;

        var mangadexRepository =
            (WeebdexRepository)scope.ServiceProvider.GetRequiredKeyedService<IRepository>(Provider.Weebdex);

        var coverImages = await mangadexRepository.GetCoverImages(publication.Series.Id, cancellationToken);

        var lang = publication.Request.GetStringOrDefault(RequestConstants.LanguageKey, "en");

        var firstCover = coverImages.FirstOrDefault(LangFilter) ?? coverImages.FirstOrDefault();
        var lastCover = coverImages.LastOrDefault(LangFilter) ?? coverImages.LastOrDefault();

        var coversByVolume = coverImages
            .GroupBy(c => string.IsNullOrEmpty(c.Volume) ? string.Empty : c.Volume)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var chapter in publication.Series.Chapters)
        {
            if (!string.IsNullOrEmpty(chapter.VolumeMarker) &&
                coversByVolume.TryGetValue(chapter.VolumeMarker, out var covers))
            {
                var cover = covers.FirstOrDefault(LangFilter) ?? covers.FirstOrDefault();
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

        bool LangFilter(Cover data)
        {
            return data.Language == lang;
        }
    }
}
