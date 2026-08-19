using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mnema.API.Content;
using Mnema.Models.Enums;

namespace Mnema.Services;

public class GroupedReleaseDetector: IGroupedReleaseDetector
{
    private const RegexOptions MatchOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly IReadOnlyList<Regex> NyaaGroupedReleasesRegex = [
        new(
            @"^(?:\(Partial\)\s*)?(?:Weekly|Monthly)\s+.+?\s+(?:Chapter|Volume)s?\s+Updates?\s*-\s*" +
            @"(?:Week\s+\d+(?:[+\-]\d+)*|[A-Za-z]+)\s+\d{4}" +
            @"(?:\s*\(Digital\))?(?:\s*\([^)]+\))?$",
            MatchOptions, RegexTimeout),
    ];

    public bool IsGroupedRelease(Provider provider, string releaseName)
    {
        var regexes = provider switch
        {
            Provider.Nyaa => NyaaGroupedReleasesRegex,
            _ => []
        };

        return regexes.Any(r => r.IsMatch(releaseName));
    }


}
