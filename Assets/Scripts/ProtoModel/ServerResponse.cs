using ProtoBuf;

[ProtoContract]
public class ServerResponse
{
    [ProtoMember(1)]
    public string Message { get; set; } = string.Empty;

    [ProtoMember(2)]
    public GameSession? SessionData { get; set; }
}
