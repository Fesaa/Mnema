using System.Net;
using HtmlAgilityPack;

namespace Mnema.Providers.Extensions;

internal static class StringExtensions
{
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
}
