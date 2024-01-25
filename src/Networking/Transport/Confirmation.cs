using ProtoBuf;

namespace NetBenchTest.Networking.Transport;

[ProtoContract]
public class Confirmation : NetworkPacket
{
    public Confirmation() : base(MessageType.Confirmation) { }

    [ProtoMember(1)]
    public string Value;
}