using Mnema.API;
using Mnema.Models.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace Mnema.Database.Extensions;

public static class IncludeExtensions
{

    public static IQueryable<MnemaUser> Includes(this IQueryable<MnemaUser> query, UserIncludes includes)
    {

        if (includes.HasFlag(UserIncludes.Subscriptions))
        {
            query = query.Include(u => u.Subscriptions);
        }

        if (includes.HasFlag(UserIncludes.Pages))
        {
            query = query.Include(u => u.Pages);
        }

        if (includes.HasFlag(UserIncludes.Preferences))
        {
            query = query.Include(u => u.Preferences);
        }

        return query;
    }
    
}