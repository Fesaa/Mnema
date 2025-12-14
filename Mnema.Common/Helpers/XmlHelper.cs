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
}