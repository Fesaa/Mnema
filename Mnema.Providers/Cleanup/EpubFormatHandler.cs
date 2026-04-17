using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.External;

namespace Mnema.Providers.Cleanup;

internal class EpubFormatHandler(ILogger<EpubFormatHandler> logger, IFileSystem fileSystem): IFormatHandler
{

    private static readonly XNamespace Cn = "urn:oasis:names:tc:opendocument:xmlns:container";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Opf = "http://www.idpf.org/2007/opf";

    public Format SupportedFormat => Format.Epub;
    public async Task HandleAsync(FormatHandlerContext context)
    {
        if (fileSystem.File.Exists(context.DestinationPath))
            fileSystem.File.Delete(context.DestinationPath);

        fileSystem.File.Copy(context.SourceFile, context.DestinationPath);

        if (context.ComicInfo == null) return;

        await using var stream = fileSystem.File.Open(context.DestinationPath, FileMode.Open, FileAccess.ReadWrite);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Update);

        var containerEntry = archive.GetEntry("META-INF/container.xml");
        if (containerEntry == null) return;

        XDocument containerDoc;
        await using (var r = await containerEntry.OpenAsync())
        {
            containerDoc = XDocument.Load(r);
        }

        var opfPath = containerDoc.Descendants(Cn + "rootfile")
            .Select(x => x.Attribute("full-path")?.Value)
            .FirstOrDefault(x => x != null && x.EndsWith(".opf"));

        if (string.IsNullOrEmpty(opfPath))
        {
            logger.LogDebug("Downloaded EPUB file {FileName} does not contain an OPF file.", context.DestinationPath);
            return;
        }

        var opfEntry = archive.GetEntry(opfPath);
        if (opfEntry == null) return;

        XDocument opfDoc;
        await using (var r = await opfEntry.OpenAsync())
        {
            opfDoc = XDocument.Load(r);
        }

        SyncWithComicInfo(opfDoc, context.ComicInfo);

        await using (var w = await opfEntry.OpenAsync())
        {
            w.SetLength(0);
            opfDoc.Save(w);
        }
    }

    private static void SyncWithComicInfo(XDocument doc, ComicInfo info)
    {
        var metadata = doc.Root?.Element(Opf + "metadata") ?? doc.Root?.Element("metadata");
        if (metadata == null) return;

        metadata.SetElementValue(Dc + "title", info.Title);
        metadata.SetElementValue(Dc + "description", info.Summary);
        metadata.SetElementValue(Dc + "publisher", info.Publisher);

        var titleId = metadata.Element(Dc + "title")?.Attribute("id")?.Value;
        if (!string.IsNullOrEmpty(titleId))
        {
            metadata.SetRefinedMetadata(Opf, "file-as", titleId, info.Title);
        }

        UpdateCreators(metadata, info);
        UpdateSeries(metadata, info);
        UpdateTags(metadata, info);

        metadata.GetOrCreateMeta(Opf, "dcterms:modified").Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private static void UpdateSeries(XElement metadata, ComicInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Series)) return;

        // Legacy Calibre support
        metadata.SetOrAddMetaValue("calibre:series", info.Series);
        metadata.SetOrAddMetaValue("calibre:series_index", info.Volume);

        // Modern EPUB 3 Collection logic
        var seriesMeta = metadata.Elements()
            .FirstOrDefault(e => e.Attribute("property")?.Value == "belongs-to-collection");
        if (seriesMeta == null)
        {
            seriesMeta = metadata.GetOrCreateMeta(Opf, "belongs-to-collection");
            seriesMeta.Add(new XAttribute("id", "series_" + Guid.NewGuid().ToString("N")[..8]));
        }
        seriesMeta.Value = info.Series;

        var seriesId = seriesMeta.Attribute("id")?.Value;
        if (seriesId != null)
        {
            metadata.SetRefinedMetadata(Opf, "collection-type", seriesId, "series");
            metadata.SetRefinedMetadata(Opf, "group-position", seriesId, info.Volume);
        }
    }

    private static void UpdateCreators(XElement metadata, ComicInfo info)
    {
        // Clean existing
        metadata.Elements(Dc + "creator").Remove();
        metadata.Elements().Where(e => e.Attribute("property")?.Value == "role").Remove();

        var roles = new[] {
            (info.Writer, "aut"), (info.Penciller, "art"), (info.Colorist, "clr"), (info.Translator, "trl")
        };

        var i = 0;
        foreach (var (names, role) in roles.Where(r => !string.IsNullOrWhiteSpace(r.Item1)))
        {
            foreach (var name in names!.Split(',', StringSplitOptions.TrimEntries))
            {
                var id = $"cr{i++}";
                var creator = new XElement(Dc + "creator", new XAttribute("id", id), name);
                metadata.Add(creator);
                metadata.SetRefinedMetadata(Opf, "role", id, role);
            }
        }
    }

    private static void UpdateTags(XElement metadata, ComicInfo info)
    {
        metadata.Elements(Dc + "subject").Remove();

        if (string.IsNullOrWhiteSpace(info.Genre)) return;

        var genres = info.Genre.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var genre in genres)
        {
            metadata.Add(new XElement(Dc + "subject", genre));
        }
    }
}
