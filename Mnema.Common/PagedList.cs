using System;
using System.Collections.Generic;

namespace Mnema.Common;

public class PagedList<T>(IEnumerable<T> items, int count, int pageNumber, int pageSize)
{
    public IEnumerable<T> Items { get; set; } = items;
    public int CurrentPage { get; set; } = pageNumber;
    public int TotalPages { get; set; } = (int)Math.Ceiling(count / (double)pageSize);
    public int PageSize { get; set; } = pageSize;
    public int TotalCount { get; set; } = count;

    public static PagedList<T> Empty()
    {
        return new PagedList<T>([], 0, 0, 0);
    }
}