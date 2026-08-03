using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.API.Repositories;

public interface IMetadataProviderSettingsRepository : IEntityRepository<MetadataProviderSettings, MetadataProviderSettingsV2Dto>
{
    Task<MetadataProviderSettingsV2Dto> GetMetadataProviderSettingsDto(MetadataProvider metadataProvider,
        CancellationToken cancellationToken);

    Task<MetadataProviderSettings> GetMetadataProviderSettings(MetadataProvider metadataProvider,
        CancellationToken cancellationToken);

    Task<List<MetadataProvider>> GetOrder(CancellationToken cancellationToken);
}
