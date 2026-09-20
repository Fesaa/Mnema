using System;
using System.Collections.Generic;
using System.Linq;
using Mnema.Models.Enums;

namespace Mnema.Models.Entities;

public interface ILinkInfo
{
    string Url { get; }
    string Language { get; }
}

public class LinkFilter(LinkFilterMode mode, LinkFilterType type, string value)
{
    public LinkFilterMode Mode { get; set; } = mode;
    public LinkFilterType Type { get; set; } = type;
    public string Value { get; set; } = value;

    public bool Matches(ILinkInfo link)
    {
        return Type switch
        {
            LinkFilterType.Language => link.Language == Value,
            LinkFilterType.Hostname =>
                Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) &&
                IsSameHostname(uri.Host, Value),
            _ => false
        };
    }

    public static bool IsAllowed(ILinkInfo link, IEnumerable<LinkFilter> filters)
    {
        var matchingFilters = filters.Where(f => f.Matches(link)).ToList();

        if (matchingFilters.Any(f => f.Mode == LinkFilterMode.Include))
            return true;

        return matchingFilters.Count == 0;
    }

    public static bool IsHostnameAllowed(string hostname, IEnumerable<LinkFilter> filters)
    {
        var matching = filters
            .Where(f => f.Type == LinkFilterType.Hostname)
            .Where(f => IsSameHostname(hostname, f.Value))
            .ToList();

        if (matching.Any(f => f.Mode == LinkFilterMode.Include))
            return true;

        return matching.Count == 0;
    }

    /// <summary>
    /// Hostnames are case insensitive, and a filter value copied from the address bar may or may not
    /// carry the www. prefix, so both sides are normalized before comparing.
    /// </summary>
    private static bool IsSameHostname(string first, string second)
    {
        return NormalizeHostname(first).Equals(NormalizeHostname(second), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHostname(string hostname)
    {
        var trimmed = hostname.Trim();

        return trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? trimmed[4..] : trimmed;
    }
}
