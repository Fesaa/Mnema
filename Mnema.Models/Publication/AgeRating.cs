using System.ComponentModel;
using System.Xml.Serialization;

namespace Mnema.Models.Publication;

/// <summary>
///     Represents Age Rating for content.
/// </summary>
/// <remarks>Based on ComicInfo.xml v2.1 https://github.com/anansi-project/comicinfo/blob/main/drafts/v2.1/ComicInfo.xsd</remarks>
/// <remarks>Copied from https://github.com/Kareadita/Kavita/blob/develop/API/Entities/Enums/AgeRating.cs</remarks>
public enum AgeRating
{
    [XmlEnum("Unknown")] [Description("Unknown")]
    Unknown = 0,

    [XmlEnum("Rating Pending")] [Description("Rating Pending")]
    RatingPending = 1,

    [XmlEnum("Early Childhood")] [Description("Early Childhood")]
    EarlyChildhood = 2,

    [XmlEnum("Everyone")] [Description("Everyone")]
    Everyone = 3,

    [XmlEnum("G")] [Description("G")] G = 4,

    [XmlEnum("Everyone 10+")] [Description("Everyone 10+")]
    Everyone10Plus = 5,

    [XmlEnum("PG")] [Description("PG")]
    // ReSharper disable once InconsistentNaming
    PG = 6,

    [XmlEnum("Kids to Adults")] [Description("Kids to Adults")]
    KidsToAdults = 7,

    [XmlEnum("Teen")] [Description("Teen")]
    Teen = 8,

    [XmlEnum("MA15+")] [Description("MA15+")]
    Mature15Plus = 9,

    [XmlEnum("Mature 17+")] [Description("Mature 17+")]
    Mature17Plus = 10,

    [XmlEnum("M")] [Description("M")] Mature = 11,

    [XmlEnum("R18+")] [Description("R18+")]
    R18Plus = 12,

    [XmlEnum("Adults Only 18+")] [Description("Adults Only 18+")]
    AdultsOnly = 13,

    [XmlEnum("X18+")] [Description("X18+")]
    X18Plus = 14
}