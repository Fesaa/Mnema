using Microsoft.EntityFrameworkCore;
using Mnema.Common;

namespace Mnema.Database.Extensions;

public static class PagedListExtensions
{

    extension<T>(IQueryable<T> source)
    {
        public async Task<PagedList<T>> CreateAsync(PaginationParams pagination)
        {
            return await CreateAsync(source, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PagedList<T>> CreateAsync(int pageNumber, int pageSize)
        {
            // NOTE: OrderBy warning being thrown here even if query has the orderby statement
            var count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }

}