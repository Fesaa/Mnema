using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Repositories;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Scanner;
using Mnema.Models.Entities.Scanner;

namespace Mnema.Database.Repositories;

public class ImportScanRepository(MnemaDataContext ctx, IMapper mapper) : AbstractNavigationalEntityRepository<ImportScan, ImportScanDto, ImportScanIncludes>(ctx, mapper),
    IImportScanRepository
{
    protected override IQueryable<ImportScan> EntityWithIncludes(IQueryable<ImportScan> query, ImportScanIncludes flags)
    {
        if (flags.HasFlag(ImportScanIncludes.DirectoryImports))
        {
            query = query.Include(s => s.DirectoryImportResults);
        }

        if (flags.HasFlag(ImportScanIncludes.ImportErrors))
        {
            query = query.Include(s => s.ImportErrors);
        }

        return query;
    }

    public Task<PagedList<ImportScanShallowDto>> GetShallowScansPaged(PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        return ctx.ImportScans
            .ProjectTo<ImportScanShallowDto>(mapper.ConfigurationProvider)
            .OrderByDescending(x => x.CreatedUtc)
            .AsPagedList(paginationParams, cancellationToken);
    }

    public Task<bool> HasNonFinishedScan(string root, CancellationToken cancellationToken)
    {
        return ctx.ImportScans
            .Where(s => s.RootDir == root)
            .AnyAsync(cancellationToken);
    }

    public Task<HashSet<string>> GetAlreadyLinkedDirectoriesForRoot(string root, CancellationToken cancellationToken)
    {
        return ctx.DirectoryImportResults
            .Where(d => d.ImportScan.RootDir == root)
            .Where(d => d.MonitoredSeriesId != null)
            .Select(d => d.Directory)
            .ToHashSetAsync(cancellationToken);
    }
}
