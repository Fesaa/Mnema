using System.Collections.Generic;
using System.Xml.Serialization;

namespace Mnema.Providers.Webtoon;

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

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("lastBuildDate")]
    public string LastBuildDate { get; set; }

    [XmlElement("image")]
    public ChannelImage Image { get; set; }

    [XmlElement("item")]
    public List<Item> Items { get; set; }
}

public class ChannelImage
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("url")]
    public string Url { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("width")]
    public int Width { get; set; }

    [XmlElement("height")]
    public int Height { get; set; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; set; }

    [XmlElement("link")]
    public string Link { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }

    [XmlElement("pubDate")]
    public string PubDate { get; set; }

    [XmlElement("author")]
    public string Author { get; set; }
}
