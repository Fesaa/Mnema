using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class DownloadClientRepository(MnemaDataContext ctx, IMapper mapper): IDownloadClientRepository
{
    public Task<PagedList<DownloadClientDto>> GetAllDownloadClientsAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .ProjectTo<DownloadClientDto>(mapper.ConfigurationProvider)
            .OrderBy(c => c.Id)
            .AsPagedList(paginationParams, cancellationToken);
    }

    public Task<List<DownloadClientType>> GetInUseTypesAsync(CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .Select(c => c.Type)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<DownloadClient?> GetDownloadClientAsync(Guid id, CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
    }

    public Task<DownloadClient?> GetDownloadClientAsync(DownloadClientType type, CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .FirstOrDefaultAsync(x => x.Type == type, cancellationToken: cancellationToken);
    }

    public Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return ctx.DownloadClients
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(DownloadClient downloadClient)
    {
        ctx.DownloadClients.Add(downloadClient).State = EntityState.Added;
    }

    public void Update(DownloadClient downloadClient)
    {
        ctx.DownloadClients.Update(downloadClient).State = EntityState.Modified;
    }

}
