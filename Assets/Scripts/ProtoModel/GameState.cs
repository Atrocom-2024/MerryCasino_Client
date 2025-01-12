using ProtoBuf;

[ProtoContract]
public class GameState
{
    [ProtoMember(1)]
    public long TotalJackpotAmount { get; set; }

    [ProtoMember(2)]
    public bool IsJackpot { get; set; }
}
