using System.Collections.Generic;
using System.Linq;
using Mnema.Common.Extensions;
using Mnema.Models.External;
using Mnema.Models.Publication;

namespace Mnema.Models.DTOs.Content;

public sealed class FileMetadataDto
{
    public string Title { get; set; }
    public string Summary { get; set; }
    public string Volume { get; set; }
    public string Chapter { get; set; }
    public AgeRating AgeRating { get; set; }

    public List<string> Genres { get; set; }
    public List<string> Tags { get; set; }
    public List<WebLink> WebLinks { get; set; }

    public List<string> Writers { get; set; }
    public List<string> Colorists { get; set; }
    public List<string> Letterers { get; set; }
    public List<string> Translators { get; set; }
    public List<string> Publishers { get; set; }

    public string Isbn { get; set; }
    public int Count { get; set; }

    public static FileMetadataDto? FromComicInfo(ComicInfo? comicInfo)
    {
        if (comicInfo == null) return null;

        return new FileMetadataDto
        {
            Title = comicInfo.Title,
            Summary = comicInfo.Summary,
            Volume = comicInfo.Volume,
            Chapter = comicInfo.Number,
            AgeRating = comicInfo.AgeRating,
            Genres = comicInfo.Genre.SplitNonEmpty(","),
            Tags = comicInfo.Tags.SplitNonEmpty(","),
            WebLinks = comicInfo.Web.SplitNonEmpty(",").Select(url => new WebLink(url)).ToList(),
            Writers = comicInfo.Writer.SplitNonEmpty(","),
            Colorists = comicInfo.Colorist.SplitNonEmpty(","),
            Letterers = comicInfo.Letterer.SplitNonEmpty(","),
            Translators = comicInfo.Translator.SplitNonEmpty(","),
            Publishers = comicInfo.Publisher.SplitNonEmpty(","),
            Isbn = comicInfo.Isbn,
            Count = comicInfo.Count
        };
    }
}

public sealed record WebLink(string Url);
