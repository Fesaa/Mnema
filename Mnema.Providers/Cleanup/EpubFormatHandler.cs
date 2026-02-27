using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
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

        UpdateTitle(metadata, info);
        UpdateDescription(metadata, info.Summary);
        UpdatePublisher(metadata, info.Publisher);
        UpdateSubjects(metadata, info.Tags);
        UpdateCreators(metadata, info);
        UpdateSeries(metadata, info);
        UpdateModifiedDate(metadata);
    }

    private static void UpdateTitle(XElement metadata, ComicInfo info)
    {
        var titleEl = metadata.Element(Dc + "title");
        if (titleEl == null) return;

        titleEl.Value = info.Title;
        var titleId = titleEl.Attribute("id")?.Value;

        if (string.IsNullOrEmpty(titleId)) return;

        var sortTitle = metadata.Elements().FirstOrDefault(e =>
            e.Attribute("property")?.Value == "file-as" &&
            e.Attribute("refines")?.Value == $"#{titleId}");

        sortTitle?.Value = info.Title;
    }

    private static void UpdateCreators(XElement metadata, ComicInfo info)
    {
        metadata.Elements(Dc + "creator").Remove();
        metadata.Elements().Where(e => e.Attribute("property")?.Value == "role").Remove();

        var rolesMap = new[]
        {
            (Names: info.Writer, Role: "aut"),
            (Names: info.Penciller, Role: "art"),
            (Names: info.Colorist, Role: "clr"),
            (Names: info.Translator, Role: "trl")
        };

        var idCounter = 1;
        foreach (var entry in rolesMap.Where(x => !string.IsNullOrWhiteSpace(x.Names)))
        {
            var names = entry.Names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in names)
            {
                var creatorId = $"creator{idCounter++}";

                metadata.Add(new XElement(Dc + "creator",
                    new XAttribute("id", creatorId), name));

                metadata.Add(new XElement(Opf + "meta",
                    new XAttribute("property", "role"),
                    new XAttribute("refines", $"#{creatorId}"),
                    new XAttribute("scheme", "marc:relators"),
                    entry.Role));
            }
        }
    }

    private static void UpdateSeries(XElement metadata, ComicInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.Series)) return;

        var seriesMeta = metadata.Elements().FirstOrDefault(e =>
            e.Attribute("property")?.Value == "belongs-to-collection");

        if (seriesMeta == null)
        {
            var newId = "seriesid_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            seriesMeta = new XElement(Opf + "meta",
                new XAttribute("property", "belongs-to-collection"),
                new XAttribute("id", newId),
                info.Series);
            metadata.Add(seriesMeta);
        }
        else
        {
            seriesMeta.Value = info.Series;
        }

        var seriesId = seriesMeta.Attribute("id")?.Value;
        if (string.IsNullOrEmpty(seriesId)) return;

        // Update or Add Position
        var posMeta = metadata.Elements().FirstOrDefault(e =>
            e.Attribute("property")?.Value == "group-position" &&
            e.Attribute("refines")?.Value == $"#{seriesId}");

        if (posMeta == null)
        {
            metadata.Add(new XElement(Opf + "meta",
                new XAttribute("property", "group-position"),
                new XAttribute("refines", $"#{seriesId}"),
                info.Volume));
        }
        else
        {
            posMeta.Value = info.Volume;
        }
    }

    private static void UpdateSubjects(XElement metadata, string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) return;

        metadata.Elements(Dc + "subject").Remove();
        var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var tag in tagList)
        {
            metadata.Add(new XElement(Dc + "subject", tag));
        }
    }

    private static void UpdateDescription(XElement metadata, string? summary) =>
        GetOrAddElement(metadata, Dc + "description").Value = summary ?? string.Empty;

    private static void UpdatePublisher(XElement metadata, string? publisher) =>
        GetOrAddElement(metadata, Dc + "publisher").Value = publisher ?? string.Empty;

    private static void UpdateModifiedDate(XElement metadata)
    {
        var mod = metadata.Elements().FirstOrDefault(e => e.Attribute("property")?.Value == "dcterms:modified");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (mod != null)
        {
            mod.Value = timestamp;
        }
        else
        {
            metadata.Add(new XElement("meta", new XAttribute("property", "dcterms:modified"), timestamp));
        }
    }

    private static XElement GetOrAddElement(XElement parent, XName name)
    {
        var el = parent.Element(name);
        if (el != null) return el;

        var newEl = new XElement(name);
        parent.Add(newEl);
        return newEl;
    }
}
