using System.Collections.Generic;
using System.Linq;
using Mnema.API.Content;

namespace Mnema.Providers.Extensions;

public static class ParseResultExtensions
{
    public static List<(List<string> Series, List<ParseResult> Items)> GroupMergingSeries(this IEnumerable<ParseResult> results)
    {
        var list = results.ToList();

        var parent = new Dictionary<string, string>();

        foreach (var r in list)
        {
            if (r.Series.Count == 0) continue;
            var first = r.Series[0];
            Find(first);

            foreach (var s in r.Series.Skip(1))
                Union(first, s);
        }

        var groups = list
            .GroupBy(r => r.Series.Count > 0 ? Find(r.Series[0]) : "")
            .Select(g => (
                Series: g.SelectMany(r => r.Series).Distinct().ToList(),
                Items: g.ToList()
            ))
            .ToList();

        return groups;

        void Union(string a, string b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
                parent[ra] = rb;
        }

        string Find(string x)
        {
            parent.TryAdd(x, x);
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }
    }
}
