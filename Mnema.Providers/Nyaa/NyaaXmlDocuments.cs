using System.Collections.Generic;
using System.Xml.Serialization;

namespace Mnema.Providers.Nyaa;

[XmlRoot("rss")]
public class RssFeed
{
    [XmlAttribute("version")]
    public string Version { get; set; }

    [XmlElement("channel")]
    public Channel Channel { get; set; }
}

public class Channel
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("item")]
    public List<Item> Items { get; set; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("guid")]
    public Guid Guid { get; set; }

    [XmlElement("pubDate")]
    public string PubDate { get; set; }

    [XmlElement("seeders", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public int Seeders { get; set; }

    [XmlElement("leechers", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public int Leechers { get; set; }

    [XmlElement("downloads", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public int Downloads { get; set; }

    [XmlElement("infoHash", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string InfoHash { get; set; }

    [XmlElement("categoryId", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string CategoryId { get; set; }

    [XmlElement("category", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Category { get; set; }

    [XmlElement("size", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Size { get; set; }

    [XmlElement("comments", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public int Comments { get; set; }

    [XmlElement("trusted", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Trusted { get; set; }

    [XmlElement("remake", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Remake { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }
}

public class Guid
{
    [XmlAttribute("isPermaLink")]
    public string IsPermaLink { get; set; }

    [XmlText]
    public string Value { get; set; }
}
