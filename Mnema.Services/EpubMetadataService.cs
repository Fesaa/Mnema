using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.External;

namespace Mnema.Services;

public class EpubMetadataService(IFileSystem fileSystem): IEpubMetadataService
{
    private static readonly XNamespace Cn = "urn:oasis:names:tc:opendocument:xmlns:container";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Opf = "http://www.idpf.org/2007/opf";

    public ComicInfo? ReadComicInfo(string filePath, CancellationToken cancellationToken)
    {
        if (!fileSystem.File.Exists(filePath)) return null;

        using var stream = fileSystem.File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var opfDoc = LoadOpfDocument(archive, out _);
        if (opfDoc == null) return null;

        cancellationToken.ThrowIfCancellationRequested();

        var metadata = opfDoc.Root?.Element(Opf + "metadata") ?? opfDoc.Root?.Element("metadata");
        if (metadata == null) return null;

        var info = new ComicInfo
        {
            Title = metadata.Element(Dc + "title")?.Value ?? string.Empty,
            Summary = metadata.Element(Dc + "description")?.Value ?? string.Empty,
            Publisher = metadata.Element(Dc + "publisher")?.Value ?? string.Empty,
        };

        ReadSeries(metadata, info);
        ReadTags(metadata, info);
        ReadWebLinks(metadata, info);
        ReadIsbn(metadata, info);
        ReadCreators(metadata, info);

        return info;
    }

    public async Task WriteComicInfo(ComicInfo comicInfo, string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("EPUB file not found.", filePath);

        await using var stream = fileSystem.File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
        await using var archive = new ZipArchive(stream, ZipArchiveMode.Update);

        var opfDoc = LoadOpfDocument(archive, out var opfPath);
        if (opfDoc == null || opfPath == null) return;

        cancellationToken.ThrowIfCancellationRequested();

        SyncWithComicInfo(opfDoc, comicInfo);

        var opfEntry = archive.GetEntry(opfPath);
        if (opfEntry == null) return;

        await using var writer = await opfEntry.OpenAsync(cancellationToken);
        writer.SetLength(0);
        opfDoc.Save(writer);
    }

    private static XDocument? LoadOpfDocument(ZipArchive archive, out string? opfPath)
    {
        opfPath = null;

        var containerEntry = archive.GetEntry("META-INF/container.xml");
        if (containerEntry == null) return null;

        XDocument containerDoc;
        using (var r = containerEntry.Open())
        {
            containerDoc = XDocument.Load(r);
        }

        opfPath = containerDoc.Descendants(Cn + "rootfile")
            .Select(x => x.Attribute("full-path")?.Value)
            .FirstOrDefault(x => x != null && x.EndsWith(".opf"));

        if (string.IsNullOrEmpty(opfPath)) return null;

        var opfEntry = archive.GetEntry(opfPath);
        if (opfEntry == null) return null;

        using (var r = opfEntry.Open())
        {
            return XDocument.Load(r);
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
        UpdateWebLinks(metadata, info);
        UpdateIsbn(metadata, info);

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
            (info.Writer, "aut"), (info.Penciller, "art"), (info.Colorist, "clr"), (info.Translator, "trl"),
            (info.Publisher, "pbl"), (info.Editor, "edt")
        };

        var i = 0;
        foreach (var (names, role) in roles.Where(r => !string.IsNullOrWhiteSpace(r.Item1)))
        {
            foreach (var name in names.Split(',', StringSplitOptions.TrimEntries))
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

    private static void UpdateWebLinks(XElement metadata, ComicInfo info)
    {
        RemoveIdentifiersByScheme(metadata, "url");

        if (string.IsNullOrWhiteSpace(info.Web)) return;

        var links = info.Web
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct();

        var i = 0;
        foreach (var link in links)
        {
            metadata.Add(new XElement(Dc + "identifier",
                new XAttribute("id", $"weblink{i++}"),
                new XAttribute(Opf + "scheme", "url"),
                link));
        }
    }

    private static void UpdateIsbn(XElement metadata, ComicInfo info)
    {
        RemoveIdentifiersByScheme(metadata, "isbn");

        if (string.IsNullOrWhiteSpace(info.Isbn)) return;

        var normalized = info.Isbn.Replace("-", string.Empty).Trim();
        metadata.Add(new XElement(Dc + "identifier",
            new XAttribute("id", "isbn0"),
            new XAttribute(Opf + "scheme", "ISBN"),
            normalized));
    }

    private static void RemoveIdentifiersByScheme(XElement metadata, string scheme)
    {
        metadata.Elements(Dc + "identifier")
            .Where(e => string.Equals(e.Attribute(Opf + "scheme")?.Value, scheme, StringComparison.OrdinalIgnoreCase))
            .Remove();
    }

    private static void ReadSeries(XElement metadata, ComicInfo info)
    {
        // Prefer EPUB3 collection metadata
        var seriesMeta = metadata.Elements()
            .FirstOrDefault(e => e.Attribute("property")?.Value == "belongs-to-collection");

        if (seriesMeta != null)
        {
            info.Series = seriesMeta.Value;

            var seriesId = seriesMeta.Attribute("id")?.Value;
            if (!string.IsNullOrEmpty(seriesId))
            {
                var group = GetRefinedValue(metadata, seriesId, "group-position");
                if (!string.IsNullOrEmpty(group)) info.Volume = group.RemoveSuffix(".0");
            }

            return;
        }

        // Fallback to legacy Calibre meta
        var calibreSeries = GetLegacyMetaContent(metadata, "calibre:series");
        if (!string.IsNullOrEmpty(calibreSeries))
        {
            info.Series = calibreSeries;
            var idx = GetLegacyMetaContent(metadata, "calibre:series_index");
            if (!string.IsNullOrEmpty(idx)) info.Volume = idx.RemoveSuffix(".0");
        }
    }

    private static void ReadTags(XElement metadata, ComicInfo info)
    {
        var subjects = metadata.Elements(Dc + "subject")
            .Select(e => e.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (subjects.Count > 0)
            info.Genre = string.Join(", ", subjects);
    }

    private static void ReadWebLinks(XElement metadata, ComicInfo info)
    {
        var links = metadata.Elements(Dc + "identifier")
            .Where(e => string.Equals(e.Attribute(Opf + "scheme")?.Value, "url", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (links.Count > 0)
            info.Web = string.Join(",", links);
    }

    private static void ReadIsbn(XElement metadata, ComicInfo info)
    {
        var isbn = metadata.Elements(Dc + "identifier")
            .FirstOrDefault(e => string.Equals(e.Attribute(Opf + "scheme")?.Value, "isbn", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(isbn))
            info.Isbn = isbn;
    }

    private static void ReadCreators(XElement metadata, ComicInfo info)
    {
        var roleMap = new Dictionary<string, List<string>>();

        foreach (var creator in metadata.Elements(Dc + "creator"))
        {
            var id = creator.Attribute("id")?.Value;
            var name = creator.Value;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(id)) continue;

            var role = GetRefinedValue(metadata, id, "role");
            if (string.IsNullOrEmpty(role)) continue;

            if (!roleMap.TryGetValue(role, out var names))
            {
                names = [];
                roleMap[role] = names;
            }
            names.Add(name);
        }

        if (roleMap.TryGetValue("aut", out var writers))
            info.Writer = string.Join(", ", writers);

        if (roleMap.TryGetValue("art", out var pencillers))
            info.Penciller = string.Join(", ", pencillers);

        if (roleMap.TryGetValue("clr", out var colorists))
            info.Colorist = string.Join(", ", colorists);

        if (roleMap.TryGetValue("trl", out var translators))
            info.Translator = string.Join(", ", translators);

        if (roleMap.TryGetValue("edt", out var editors))
            info.Editor = string.Join(", ", editors);

        // Publisher is normally already set from dc:publisher; only fall back
        // to the "pbl" creator role if that element was missing.
        if (string.IsNullOrWhiteSpace(info.Publisher) && roleMap.TryGetValue("pbl", out var publishers))
            info.Publisher = string.Join(", ", publishers);
    }

    private static string? GetRefinedValue(XElement metadata, string targetId, string property)
    {
        return metadata.Elements()
            .Where(e => e.Name.LocalName == "meta")
            .FirstOrDefault(e =>
                e.Attribute("refines")?.Value == "#" + targetId &&
                e.Attribute("property")?.Value == property)
            ?.Value;
    }

    private static string? GetLegacyMetaContent(XElement metadata, string name)
    {
        return metadata.Elements()
            .Where(e => e.Name.LocalName == "meta")
            .FirstOrDefault(e => e.Attribute("name")?.Value == name)
            ?.Attribute("content")?.Value;
    }
}
