
using ProtoBuf;

namespace NetBenchTest.Networking;

[ProtoContract]
public class NetworkPacket
{
    public MessageType Type;

    public NetworkPacket() {}

    public NetworkPacket(MessageType type)
    {
        Type = type;
    }
}