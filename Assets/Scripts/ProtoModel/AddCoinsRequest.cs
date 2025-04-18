using ProtoBuf;

[ProtoContract]
public class AddCoinsRequest
{
    [ProtoMember(1)]
    public string UserId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public int AddCoinsAmount { get; set; }
}
