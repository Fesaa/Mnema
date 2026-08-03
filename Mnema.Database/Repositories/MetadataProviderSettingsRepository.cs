using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Repositories;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Database.Repositories;

public class MetadataProviderSettingsRepository(MnemaDataContext ctx, IMapper mapper) : AbstractEntityEntityRepository<MetadataProviderSettings, MetadataProviderSettingsV2Dto>(ctx, mapper), IMetadataProviderSettingsRepository
{
    public Task<MetadataProviderSettingsV2Dto> GetMetadataProviderSettingsDto(MetadataProvider metadataProvider, CancellationToken cancellationToken)
    {
        return ctx.MetadataProviderSettings
            .Where(s => s.MetadataProvider == metadataProvider)
            .ProjectTo<MetadataProviderSettingsV2Dto>(mapper.ConfigurationProvider)
            .SingleAsync(cancellationToken: cancellationToken);
    }

    public Task<MetadataProviderSettings> GetMetadataProviderSettings(MetadataProvider metadataProvider, CancellationToken cancellationToken)
    {
        return ctx.MetadataProviderSettings
            .Where(s => s.MetadataProvider == metadataProvider)
            .SingleAsync(cancellationToken: cancellationToken);
    }

    public Task<List<MetadataProvider>> GetOrder(CancellationToken cancellationToken)
    {
        return ctx.MetadataProviderSettings
            .OrderBy(s => s.Priority)
            .Select(s => s.MetadataProvider)
            .ToListAsync(cancellationToken);
    }
}
