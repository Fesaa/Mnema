using System.Linq;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Database.Repositories;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Database.Extensions;

public static class IncludeExtensions
{
    public static IQueryable<MonitoredSeries> Includes(this IQueryable<MonitoredSeries> query,
        MonitoredSeriesIncludes includes)
    {
        if (includes.HasFlag(MonitoredSeriesIncludes.Chapters))
        {
            query = query.Include(s => s.Chapters);
        }

        return query;
    }
}
