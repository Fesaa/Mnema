using System.Collections.Generic;
using Flurl;

namespace Mnema.Providers.Extensions;

public static class FlurlExtensions
{
    extension(Url url)
    {
        public Url AddRange(string param, IEnumerable<string> items)
        {
            foreach (var item in items) url.AppendQueryParam($"{param}[]", item);

            return url;
        }
    }
}