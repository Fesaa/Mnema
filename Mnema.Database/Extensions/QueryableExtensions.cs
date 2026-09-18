using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.Common;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Database.Extensions;

public static class QueryableExtensions
{
    extension<T>(IOrderedQueryable<T> source)
    {
        public async Task<PagedList<T>> AsPagedList(PaginationParams pagination,
            CancellationToken cancellationToken = default)
        {
            return await source.AsPagedList(pagination.PageNumber, pagination.PageSize, cancellationToken);
        }

        public async Task<PagedList<T>> AsPagedList(int pageNumber, int pageSize,
            CancellationToken cancellationToken = default)
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source.Skip(pageNumber * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }

    extension<T>(IQueryable<T> source)
    {
        public IQueryable<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        {
            return condition ? source.Where(predicate) : source;
        }
    }

    public static async IAsyncEnumerable<List<T>> BatchAsync<T>(this IQueryable<T> source, int batchSize, [EnumeratorCancellation] CancellationToken ct = default)
        where T : IDatabaseEntity
    {
        var currentCursor = 0;
        var hasMore = true;

        while (hasMore)
        {
            var items = await source
                .OrderBy(s => s.Id)
                .Skip(currentCursor)
                .Take(batchSize)
                .ToListAsync(ct);

            if (items.Count == 0)
                yield break;

            yield return items;

            currentCursor += items.Count;
            hasMore = items.Count == batchSize;
        }
    }
}
