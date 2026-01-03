using Microsoft.EntityFrameworkCore;
using Mnema.Common;

namespace Mnema.Database.Extensions;

public static class PagedListExtensions
{

    extension<T>(IOrderedQueryable<T> source)
    {
        public async Task<PagedList<T>> AsPagedList(PaginationParams pagination, CancellationToken cancellationToken = default)
        {
            return await AsPagedList(source, pagination.PageNumber, pagination.PageSize, cancellationToken);
        }

        public async Task<PagedList<T>> AsPagedList(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source.Skip(pageNumber * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }

}