using System.IO;
using System.Xml.Serialization;

namespace Mnema.Common.Helpers;

public static class XmlHelper
{
    public static void SerializeToFile<T>(T obj, string filePath)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StreamWriter(filePath);

        serializer.Serialize(writer, obj);
    }

    public static T? Deserialize<T>(Stream content)
    {
        var serializer = new XmlSerializer(typeof(T));
        var ret = serializer.Deserialize(content);
        return ret == null ? default : (T) ret;
    }
}