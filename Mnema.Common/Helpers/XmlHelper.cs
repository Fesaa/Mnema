using System.IO;
using System.Xml.Serialization;

namespace Mnema.Common.Helpers;

public static class XmlHelper
{
    public static void SerializeToFile<T>(XmlSerializer serializer, T obj, string filePath)
    {
        using var writer = new StreamWriter(filePath);

        serializer.Serialize(writer, obj);
    }

    public static T? Deserialize<T>(XmlSerializer serializer,Stream content)
    {
        var ret = serializer.Deserialize(content);
        return ret == null ? default : (T)ret;
    }
}
