using System.Linq;
using AutoMapper;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.Scanner;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Authentication;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Scanner;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;

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
        CreateMap<ImportScan, ImportScanDto>();
        CreateMap<DirectoryImportResult, DirectoryImportResultDto>();
        CreateMap<ImportError, ImportErrorDto>();
        CreateMap<ImportScan, ImportScanShallowDto>()
            .ForMember(d => d.DirectoryImportResultCount, o
                => o.MapFrom(s => s.DirectoryImportResults.Count(d => d.Status == DirectoryImportStatus.Queued)))
            .ForMember(d => d.ImportErrorCount, o
                => o.MapFrom(s => s.ImportErrors.Count));
    }
}
