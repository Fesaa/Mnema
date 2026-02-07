using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.API;

public interface IEntityRepository<TEntity, TEntityDto>
    where TEntity : IDatabaseEntity
    where TEntityDto : IDatabaseEntity
{
    Task<TEntity?> GetById(Guid id, CancellationToken ct = default);
    Task<PagedList<TEntity>> GetAllPaged(PaginationParams paginationParams, CancellationToken ct = default);
    Task<List<TEntity>> GetAll(CancellationToken ct = default);

    Task<TEntityDto?> GetDtoById(Guid id, CancellationToken ct = default);
    Task<PagedList<TEntityDto>> GetAllDtosPaged(PaginationParams paginationParams, CancellationToken ct = default);
    Task<List<TEntityDto>> GetAllDtos(CancellationToken ct = default);

    Task<bool> Exists(Guid id, CancellationToken ct = default);
    Task DeleteById(Guid id, CancellationToken ct = default);

    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);

}
