using ProtoBuf;

[ProtoContract]
public class BetResponse
{
    [ProtoMember(1)]
    public long UpdatedCoins { get; set; }
}
