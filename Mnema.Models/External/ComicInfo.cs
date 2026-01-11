using System;
using System.Xml.Serialization;
using Mnema.Models.Publication;

namespace Mnema.Models.External;

public class ComicInfo
{
    public ComicInfo()
    {
        Xmlns = new XmlSerializerNamespaces();
        Xmlns.Add("xsd", "http://www.w3.org/2001/XMLSchema");
        Xmlns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
    }

    [XmlNamespaceDeclarations] public XmlSerializerNamespaces Xmlns { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public string LocalizedSeries { get; set; } = string.Empty;
    public string SeriesSort { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public int Count { get; set; } = 0;
    public string Volume { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string LanguageISO { get; set; } = string.Empty;

    public string Web { get; set; } = string.Empty;
    public int Day { get; set; } = 0;
    public int Month { get; set; } = 0;
    public int Year { get; set; } = 0;


    public AgeRating AgeRating { get; set; } = AgeRating.Unknown;

    public string StoryArc { get; set; } = string.Empty;
    public string StoryArcNumber { get; set; } = string.Empty;
    public string AlternateNumber { get; set; } = string.Empty;
    public string AlternateSeries { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;

    public string Writer { get; set; } = string.Empty;
    public string Penciller { get; set; } = string.Empty;
    public string Inker { get; set; } = string.Empty;
    public string Colorist { get; set; } = string.Empty;
    public string Letterer { get; set; } = string.Empty;
    public string CoverArtist { get; set; } = string.Empty;
    public string Editor { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Imprint { get; set; } = string.Empty;
    public string Characters { get; set; } = string.Empty;
    public string Teams { get; set; } = string.Empty;
    public string Locations { get; set; } = string.Empty;
    public string Translator { get; set; } = string.Empty;

    public void SetForRole(string value, PersonRole role)
    {
        switch (role)
        {
            case PersonRole.Writer:
                Writer = value;
                break;
            case PersonRole.Penciller:
                Penciller = value;
                break;
            case PersonRole.Inker:
                Inker = value;
                break;
            case PersonRole.Colorist:
                Colorist = value;
                break;
            case PersonRole.Letterer:
                Letterer = value;
                break;
            case PersonRole.CoverArtist:
                CoverArtist = value;
                break;
            case PersonRole.Editor:
                Editor = value;
                break;
            case PersonRole.Translator:
                Translator = value;
                break;
            case PersonRole.Publisher:
                Publisher = value;
                break;
            case PersonRole.Imprint:
                Imprint = value;
                break;
            case PersonRole.Character:
                Characters = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
    }
}
