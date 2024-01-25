using ProtoBuf;

namespace NetBenchTest.Networking.Transport;

[ProtoContract]
public class Message : NetworkPacket
{
    public Message() : base(MessageType.Message) { }

    [ProtoMember(1)]
    public string Text;

    [ProtoMember(2)]
    public int Value;
}