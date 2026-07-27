using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class ExternalDownloadRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractEntityEntityRepository<ExternalDownload, ExternalDownloadDto>(ctx, mapper), IExternalDownloadRepository
{
    public Task<ExternalDownload?> GetByExternalId(string externalId, CancellationToken ct = default)
    {
        return ctx.ExternalDownloads
            .FirstOrDefaultAsync(d => d.ExternalId == externalId, ct);
    }

    public Task<Dictionary<string, ExternalDownload>> GetByExternalIds(IEnumerable<string> ids,
        CancellationToken ct = default)
    {
        return ctx.ExternalDownloads
            .Where(d => ids.Contains(d.ExternalId))
            .ToDictionaryAsync(d => d.ExternalId, ct);
    }

    public Task DeleteByExternalId(string externalId, CancellationToken ct = default)
    {
        return ctx.ExternalDownloads
            .Where(d => d.ExternalId == externalId)
            .ExecuteDeleteAsync(ct);
    }
}
