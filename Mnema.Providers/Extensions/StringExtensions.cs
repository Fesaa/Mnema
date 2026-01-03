using System;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Mnema.Providers.Extensions;

internal static class StringExtensions
{

    private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.Compiled;
    
    private static readonly Regex ImportRegex = new("""import\s*\{[^}]*\}\s*from\s*["'](?<path>\./[^"']+)["']""", RegexOptions, TimeSpan.FromMilliseconds(200));
    private static readonly Regex ObjectRegex = new(@"const\s+\w+\s*=\s*(\{.*?\})\s*;", RegexOptions, TimeSpan.FromMilliseconds(200));
    private static readonly Regex JsObjectReplacerRegex = new(@"(?<=\{|,)\s*(\w+)\s*:", RegexOptions, TimeSpan.FromMilliseconds(200));

    extension(string html)
    {
        internal HtmlDocument ToHtmlDocument()
        {
            var cleaned = WebUtility.HtmlDecode(html);
            
            var doc = new HtmlDocument();
            doc.LoadHtml(cleaned);
            
            return doc;
        }
    }
    
    extension(string js)
    {
        internal string? FindJsImport(string contains)
        {

            foreach (Match match in ImportRegex.Matches(js))
            {
                var path = match.Groups["path"].Value;
                if (path.Contains(contains, StringComparison.Ordinal))
                    return path;
            }

            return null;
        }
        
        internal string ExtractObjectLiteral()
        {
            var match = ObjectRegex.Match(js);
            return !match.Success ? string.Empty : match.Groups[1].Value;
        }
        
        internal string JsObjectToJson()
        {
            var quotedKeys = JsObjectReplacerRegex.Replace(js, @"""$1"":");

            return quotedKeys;
        }
    }
}