using System;
using System.Collections.Generic;
using System.Globalization;

namespace Mnema.Common.Helpers;

/// <summary>
/// Handles all things parsing of External Ids (weblinks, not set checks, anilist:X)
/// </summary>
/// <remarks>Copied and adjusted from Kavita</remarks>
public static class ExternalIdParser
{
    private const string HardcoverStaffWebsite = "https://hardcover.app/id/authors/";
    private const string HardcoverSeriesWebsite = "https://hardcover.app/id/series/";
    private const string HardcoverBookWebsite = "https://hardcover.app/id/book/";
    private const string MangaBakaWebsite = "https://mangabaka.org/";

    private static readonly Dictionary<string, int> WeblinkExtractionMap = new()
    {
        {HardcoverSeriesWebsite, 0},
        {HardcoverBookWebsite, 0},
        {HardcoverStaffWebsite, 0},
        {MangaBakaWebsite, 0},
    };

    public static int GetMangaBakaId(string? weblinks)
    {
        return ExtractId<int?>(weblinks, MangaBakaWebsite) ?? 0;
    }

    public static int GetHardcoverSeriesId(string? weblinks)
    {
        return ExtractId<int?>(weblinks, HardcoverSeriesWebsite) ?? 0;
    }

    public static int GetHardcoverBookId(string? weblinks)
    {
        return ExtractId<int?>(weblinks, HardcoverBookWebsite) ?? 0;
    }

    public static string GetHardcoverStaffId(string? url)
    {
        try
        {
            return ExtractId<string?>(url, HardcoverStaffWebsite) ?? string.Empty;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Extract an ID from a given weblink
    /// </summary>
    /// <param name="webLinks"></param>
    /// <param name="website"></param>
    /// <returns></returns>
    private static T? ExtractId<T>(string? webLinks, string website)
    {
        if (string.IsNullOrEmpty(webLinks)) return default;

        var index = WeblinkExtractionMap[website];
        foreach (var webLink in webLinks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!webLink.StartsWith(website)) continue;

            var tokens = webLink.Split(website)[1].Split('/');
            var value = tokens[index];

            if (typeof(T) == typeof(int?))
            {
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var intValue)) return (T)(object)intValue;
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value, CultureInfo.InvariantCulture, out var intValue)) return (T)(object)intValue;

                return default;
            }
            else if (typeof(T) == typeof(long?))
            {
                if (long.TryParse(value, CultureInfo.InvariantCulture, out var longValue)) return (T)(object)longValue;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }
        }

        return default;
    }


    /// <summary>
    /// Generate a URL from a given ID and website
    /// </summary>
    /// <typeparam name="T">Type of the ID (e.g., int, long, string)</typeparam>
    /// <param name="id">The ID to embed in the URL</param>
    /// <param name="website">The base website URL</param>
    /// <returns>The generated URL or null if the website is not supported</returns>
    public static string? GenerateUrl<T>(T id, string website)
    {
        if (!WeblinkExtractionMap.ContainsKey(website))
        {
            return null; // Unsupported website
        }

        if (Equals(id, default(T)))
        {
            throw new ArgumentNullException(nameof(id), "ID cannot be null.");
        }

        // Ensure the type of the ID matches supported types
        if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(string))
        {
            return $"{website}{id}";
        }

        throw new ArgumentException("Unsupported ID type. Supported types are int, long, and string.", nameof(id));
    }
}
