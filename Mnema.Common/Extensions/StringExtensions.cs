using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mnema.Common.Extensions;

public static class StringExtensions
{
    private const RegexOptions MatchOptions =
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly Regex NormalizeRegex = new(@"[^\p{L}0-9\+!＊！＋]",
        MatchOptions, RegexTimeout);

    extension(string? s)
    {
        public string ToNormalized()
        {
            return string.IsNullOrEmpty(s) ? string.Empty : NormalizeRegex.Replace(s, string.Empty).Trim().ToLower();
        }

        public string CleanForLogging()
        {
            return string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\n", string.Empty).Replace("\r", string.Empty);
        }

        public string OrNonEmpty(params string[] other)
        {
            if (!string.IsNullOrEmpty(s)) return s;

            foreach (var s1 in other)
                if (!string.IsNullOrEmpty(s1))
                    return s1;

            return string.Empty;
        }

        public string PadFloat(int n)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var parts = s.Split(".");
            if (parts.Length < 2) return s.PadLeft(n, '0');

            return parts[0].PadLeft(n, '0') + "." + parts[1];
        }

        public string Limit(int n)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            if (s.Length < n) return s;

            return s[(n - 3)..] + "...";
        }
    }

    extension(string s)
    {
        public string RemovePrefix(string other)
        {
            if (string.IsNullOrEmpty(other) || s.Length < other.Length) return s;

            if (!s.StartsWith(other)) return s;

            return s[other.Length..];
        }

        public string RemoveSuffix(string other)
        {
            if (string.IsNullOrEmpty(other) || s.Length < other.Length) return s;

            if (!s.EndsWith(other)) return s;

            return s[..^other.Length];
        }

        public int AsInt()
        {
            if (int.TryParse(s, out var result)) return result;

            return 0;
        }

        public DateTime? AsDateTime(string format)
        {
            return DateTime.TryParseExact(s.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var result)
                ? result.ToUniversalTime()
                : null;
        }
    }
}
