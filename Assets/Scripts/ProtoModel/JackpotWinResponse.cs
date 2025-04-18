using ProtoBuf;

[ProtoContract]
public class JackpotWinResponse
{
    [ProtoMember(1)]
    public long AddedCoinsAmount { get; set; }
}
