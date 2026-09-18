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
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Database.Repositories;

public abstract class AbstractEntityEntityRepository<TEntity, TEntityDto>(MnemaDataContext ctx, IMapper mapper)
    : AbstractDbOnlyEntityRepository<TEntity>(ctx) ,IEntityRepository<TEntity,TEntityDto>
    where TEntity : class, IDatabaseEntity
    where TEntityDto : IDatabaseEntity
{

    public Task<TEntityDto?> GetDtoById(Guid id, CancellationToken ct = default)
    {
        return DbSet
            .Where(x => x.Id == id)
            .ProjectTo<TEntityDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(ct);
    }

    public Task<PagedList<TEntityDto>> GetAllDtosPaged(PaginationParams paginationParams, CancellationToken ct = default)
    {
        return DbSet
            .ProjectTo<TEntityDto>(mapper.ConfigurationProvider)
            .OrderBy(x => x.Id)
            .AsPagedList(paginationParams, ct);
    }

    public Task<List<TEntityDto>> GetAllDtos(CancellationToken ct = default)
    {
        return DbSet.ProjectTo<TEntityDto>(mapper.ConfigurationProvider).ToListAsync(ct);
    }
}
