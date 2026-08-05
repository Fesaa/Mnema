using System;

namespace Mnema.Common.Extensions;

public static class TimeSpanExtensions
{
    extension(TimeSpan ts)
    {
        /// <summary>
        /// Returns a human-readable representation of the TimeSpan using the most appropriate unit.
        /// Returns "null" if the TimeSpan is null.
        /// </summary>
        public string ToReadableString()
        {
            if (ts == null)
                return "null";

            var isNegative = ts < TimeSpan.Zero;
            ts = ts.Duration();
            var prefix = isNegative ? "-" : "";

            if (ts.TotalSeconds < 2)
                return $"{prefix}{ts.TotalMilliseconds:F0}ms";

            if (ts.TotalMinutes < 2)
                return $"{prefix}{ts.TotalSeconds:F2}s";

            if (ts.TotalHours < 2)
                return $"{prefix}{ts.TotalMinutes:F2}m";

            if (ts.TotalDays < 2)
                return $"{prefix}{ts.TotalHours:F2}h";

            return $"{prefix}{ts.TotalDays:F2}d";
        }
    }
}
