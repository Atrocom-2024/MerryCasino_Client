using ProtoBuf;

[ProtoContract]
public class AddCoinsResponse
{
    [ProtoMember(1)]
    public long AddedCoinsAmount { get; set; }
}
