using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class DownloadClientRepository(MnemaDataContext ctx, IMapper mapper):
    AbstractEntityEntityRepository<DownloadClient, DownloadClientDto>(ctx, mapper), IDownloadClientRepository
{

    public Task<List<DownloadClientType>> GetInUseTypesAsync(CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .Select(c => c.Type)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<DownloadClient?> GetDownloadClientAsync(DownloadClientType type, CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .FirstOrDefaultAsync(x => x.Type == type, cancellationToken: cancellationToken);
    }

}
