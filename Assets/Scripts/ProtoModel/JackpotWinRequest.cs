using ProtoBuf;


[ProtoContract]
public class JackpotWinRequest
{
    [ProtoMember(1)]
    public string JackpotType { get; set; } = string.Empty;

    [ProtoMember(2)]
    public int JackpotWinCoins { get; set; }
}
