using ProtoBuf;

[ProtoContract]
public class GameUserState
{
    [ProtoMember(1)]
    public decimal CurrentPayout { get; set; }

    [ProtoMember(2)]
    public decimal JackpotProb { get; set; }
}
