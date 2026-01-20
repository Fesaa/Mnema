using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Interfaces;

namespace Mnema.Database.Interceptors;
public sealed class NormalizationInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Normalize(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Normalize(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Normalize(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var type = entity.GetType();

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = prop.GetCustomAttribute<NormalizedFromAttribute>();
                if (attr == null) continue;

                var sourceProp = type.GetProperty(attr.PropertyName);
                if (sourceProp == null) continue;

                var sourceValue = sourceProp.GetValue(entity) as string;
                if (string.IsNullOrWhiteSpace(sourceValue)) continue;

                prop.SetValue(entity, sourceValue.ToNormalized());
            }
        }
    }
}

