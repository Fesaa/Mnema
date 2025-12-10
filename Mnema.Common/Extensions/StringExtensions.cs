using System.Text.RegularExpressions;

namespace Mnema.Common.Extensions;

public static class StringExtensions
{
    
    private const RegexOptions MatchOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);
    
    private static readonly Regex NormalizeRegex = new(@"[^\p{L}0-9\+!＊！＋]",
        MatchOptions, RegexTimeout);

    extension(string? s)
    {
        
        public string ToNormalized()
        {
            return string.IsNullOrEmpty(s) ? string.Empty : NormalizeRegex.Replace(s, string.Empty).Trim().ToLower();
        }
        

        public string OrNonEmpty(params string?[] other)
        {
            if (!string.IsNullOrEmpty(s)) return s;

            foreach (var s1 in other)
            {
                if (!string.IsNullOrEmpty(s1)) return s1;
            }

            return string.Empty;
        }
        
    }
    
}