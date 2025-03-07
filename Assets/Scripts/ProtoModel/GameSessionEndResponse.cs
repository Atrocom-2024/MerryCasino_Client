using ProtoBuf;

[ProtoContract]
public class GameSessionEndResponse
{
    [ProtoMember(1)]
    public long RewardedCoinsAmount { get; set; }

    [ProtoMember(2)]
    public int RewardCoins { get; set; }
}
