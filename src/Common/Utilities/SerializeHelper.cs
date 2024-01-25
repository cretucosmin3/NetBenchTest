using ProtoBuf;

namespace NetBenchTest.Common.Utilities;

public static class SerializeHelper
{
    public static byte[] Serialize<T>(T obj)
    {
        using (var memoryStream = new System.IO.MemoryStream())
        {
            Serializer.Serialize(memoryStream, obj);
            return memoryStream.ToArray();
        }
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        using (var memoryStream = new System.IO.MemoryStream(bytes))
        {
            return Serializer.Deserialize<T>(memoryStream);
        }
    }
}