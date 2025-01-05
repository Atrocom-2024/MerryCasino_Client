using ProtoBuf;

[ProtoContract]
public class ClientRequest
{
    [ProtoMember(1)]
    public string RequestType { get; set; } = string.Empty; // JoinRoom, Bet

    [ProtoMember(2)]
    public JoinRoomRequest? JoinRoomData { get; set; }

    [ProtoMember(3)]
    public BetRequest? BetData { get; set; }
}
