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
    : IEntityRepository<TEntity,TEntityDto>
    where TEntity : class, IDatabaseEntity
    where TEntityDto : IDatabaseEntity
{

    protected DbSet<TEntity> DbSet => ctx.Set<TEntity>();

    public Task<TEntity?> GetById(Guid id, CancellationToken ct = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<PagedList<TEntity>> GetAllPaged(PaginationParams paginationParams, CancellationToken ct = default)
    {
        return DbSet
            .OrderBy(x => x.Id)
            .AsPagedList(paginationParams, ct);
    }

    public Task<List<TEntity>> GetAll(CancellationToken ct = default)
    {
        return DbSet.ToListAsync(ct);
    }

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
