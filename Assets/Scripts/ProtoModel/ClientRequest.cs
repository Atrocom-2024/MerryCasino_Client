using ProtoBuf;

[ProtoContract]
public class ClientRequest
{
    [ProtoMember(1)]
    public string RequestType { get; set; } = string.Empty; // JoinRoomRequest, BetRequest, AddCoinsRequest

    [ProtoMember(2)]
    public JoinRoomRequest? JoinRoomData { get; set; }

    [ProtoMember(3)]
    public BetRequest? BetData { get; set; }

    [ProtoMember(4)]
    public AddCoinsRequest? AddCoinsData { get; set; }
}
