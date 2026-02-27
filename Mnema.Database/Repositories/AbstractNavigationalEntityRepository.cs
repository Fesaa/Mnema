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

public abstract class AbstractNavigationalEntityRepository<TEntity, TEntityDto, TFlags>(MnemaDataContext ctx, IMapper mapper)
    : INavigationalEntityRepository<TEntity, TEntityDto, TFlags>
    where TEntity : class, IDatabaseEntity
    where TEntityDto : IDatabaseEntity
    where TFlags : struct, Enum
{

    protected DbSet<TEntity> DbSet => ctx.Set<TEntity>();

    protected abstract IQueryable<TEntity> EntityWithIncludes(IQueryable<TEntity> query, TFlags flags);

    public Task<TEntity?> GetById(Guid id, TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<PagedList<TEntity>> GetAllPaged(PaginationParams paginationParams, TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .OrderBy(x => x.Id)
            .AsPagedList(paginationParams, ct);
    }

    public Task<List<TEntity>> GetAll(TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .ToListAsync(ct);
    }

    public Task<TEntityDto?> GetDtoById(Guid id, TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .ProjectTo<TEntityDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<PagedList<TEntityDto>> GetAllDtosPaged(PaginationParams paginationParams, TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .ProjectTo<TEntityDto>(mapper.ConfigurationProvider)
            .OrderBy(x => x.Id)
            .AsPagedList(paginationParams, ct);
    }

    public Task<List<TEntityDto>> GetAllDtos(TFlags flags = default, CancellationToken ct = default)
    {
        return EntityWithIncludes(DbSet, flags)
            .ProjectTo<TEntityDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);
    }

    public Task<bool> Exists(Guid id, CancellationToken ct = default)
    {
        return DbSet.AnyAsync(x => x.Id == id, ct);
    }

    public Task DeleteById(Guid id, CancellationToken ct = default)
    {
        return DbSet.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }

    public void Add(TEntity entity)
    {
        DbSet.Add(entity).State = EntityState.Added;
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        DbSet.AddRange(entities);
    }

    public void Update(TEntity entity)
    {
        DbSet.Update(entity).State = EntityState.Modified;
    }

    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity).State = EntityState.Deleted;
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }
}
