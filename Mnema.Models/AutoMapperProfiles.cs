using System.Linq;
using AutoMapper;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Authentication;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Models;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<Page, PageDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<Preferences, PreferencesDto>();
        CreateMap<Connection, ConnectionDto>();
        CreateMap<ContentRelease, ContentReleaseDto>();
        CreateMap<DownloadClient, DownloadClientDto>();
        CreateMap<MonitoredSeries, MonitoredSeriesDto>()
            .ForMember(dest => dest.Chapters, opt
                => opt.MapFrom(src
                    => src.Chapters.OrderBy(c => c.SortOrder)
            ));
        CreateMap<MonitoredChapter, MonitoredChapterDto>()
            .ForMember(dest => dest.SeriesTitle, opt
                => opt.MapFrom(src => src.Series.Title));
        CreateMap<AuthKey, AuthKeyDto>();
        CreateMap<MetadataProviderSettings, MetadataProviderSettingsV2Dto>();
    }
}
