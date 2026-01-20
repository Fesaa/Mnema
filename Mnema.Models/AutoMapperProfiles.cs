using System.Linq;
using AutoMapper;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.UI;
using Mnema.Models.Entities.User;

namespace Mnema.Models;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<Page, PageDto>();
        CreateMap<MnemaUser, UserDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<UserPreferences, UserPreferencesDto>();
        CreateMap<Connection, ExternalConnectionDto>();
        CreateMap<ContentRelease, ContentReleaseDto>();
        CreateMap<DownloadClient, DownloadClientDto>();
        CreateMap<MonitoredSeries, MonitoredSeriesDto>()
            .ForMember(dest => dest.Chapters, opt
                => opt.MapFrom(src
                    => src.Chapters.OrderBy(c => c.Volume).ThenBy(c => c.Chapter)
            ));
        CreateMap<MonitoredChapter, MonitoredChapterDto>();
    }
}
