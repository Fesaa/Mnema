using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Mnema.Common;

namespace Mnema.Server.Helpers;

public class PaginationParamsModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Metadata.ModelType == typeof(PaginationParams))
            return new BinderTypeModelBinder(typeof(PaginationParamsModelBinder));

        return null;
    }
}

/// <summary>
///     A custom model binder for PaginationParams which assigns the null value when none of the fields are found. Fields
///     are matched case-insensitive.
///     This is needed so we don't get int.MaxValue as pageSize by default everywhere
/// </summary>
public class PaginationParamsModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var query = bindingContext.HttpContext.Request.Query;

        var pageNumberKey = query.Keys.FirstOrDefault(k => k.Equals("PageNumber", StringComparison.OrdinalIgnoreCase));
        var pageSizeKey = query.Keys.FirstOrDefault(k => k.Equals("PageSize", StringComparison.OrdinalIgnoreCase));

        if (pageSizeKey == null && pageNumberKey == null)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var pageNumber = 1;
        var pageSize = int.MaxValue;

        if (pageNumberKey != null && int.TryParse(query[pageNumberKey], out var pn)) pageNumber = pn;

        if (pageSizeKey != null && int.TryParse(query[pageSizeKey], out var ps)) pageSize = ps;

        var result = new PaginationParams
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }
}