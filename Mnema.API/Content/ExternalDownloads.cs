using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IExternalDownloadRepository : IEntityRepository<ExternalDownload, ExternalDownloadDto>
{
    Task<ExternalDownload?> GetByExternalId(string externalId, CancellationToken ct = default);
    Task<Dictionary<string, ExternalDownload>> GetByExternalIds(IEnumerable<string> ids, CancellationToken ct = default);

    Task DeleteByExternalId(string externalId, CancellationToken ct = default);
}
