using ProtoBuf;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class RoomSocketManager : MonoBehaviour
{
    public static RoomSocketManager Instance { get; private set; } // 싱글톤 인스턴스

    private TcpClient _client;
    private NetworkStream _networkStream;
    private bool _isConnected = false;

    // 이벤트 정의
    public event Action<GameUserState> OnGameUserStateResponse;
    public event Action<GameState> OnGameStateResponsee;
    public event Action<BetResponse> OnBetResponse;
    public event Action<AddCoinsResponse> onAddCoinsResponse;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 서버 연결 초기화
    public async Task ConnectToServer(string ipAddress, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ipAddress, port);
            _networkStream = _client.GetStream();
            _isConnected = true;

            Debug.Log("[socket] Connected to the server!");

            // 서버에서 오는 메세지 수신 시작
            StartCoroutine(ReceiveServerMessagesCoroutine());
        }
        catch (Exception ex)
        {
            _isConnected = false; // 연결 실패 시 플래그 설정
            Debug.LogError($"[socket] Error connecting to server: {ex.Message}");
        }
    }

    // 클라이언트 연결 해제
    public void Disconnect()
    {
        if (_isConnected)
        {
            _isConnected = false;
            _networkStream?.Close();
            _client?.Close();

            StopAllCoroutines(); // 모든 코루틴 중지
            Debug.Log("[socket] Disconnected from server");
        }
    }

    // 룸 조인 요청
    public async Task SendRoomJoinRequest(string userId, int roomId)
    {
        if (!_isConnected)
        {
            Debug.LogError("[socket] Not connected to server!");
            return;
        }

        var reqeust = new ClientRequest
        {
            RequestType = "JoinRoomRequest",
            JoinRoomData = new JoinRoomRequest
            {
                UserId = userId,
                RoomId = roomId
            }
        };

        Debug.Log($"[client] Sending RoomJoinReqeust for RoomId: {roomId}");

        byte[] message = SerializeProtobuf(reqeust);
        await _networkStream.WriteAsync(message, 0, message.Length);
    }

    // 배팅 요청
    public async Task SendBetReqeust(string userId, int betAmount)
    {
        if (!_isConnected) return;

        var reqeust = new ClientRequest
        {
            RequestType = "BetRequest",
            BetData = new BetRequest
            {
                UserId = userId,
                BetAmount = betAmount,
                RoomType = 1
            }
        };

        Debug.Log($"[socket] Sending BetRequest with BetAmount: {betAmount}");

        byte[] message = SerializeProtobuf(reqeust);
        await _networkStream.WriteAsync(message, 0, message.Length);
    }

    public async Task SendAddCoinsRequest(string userId, int coins)
    {
        if (!_isConnected) return;

        var request = new ClientRequest
        {
            RequestType = "AddCoinsRequest",
            AddCoinsData = new AddCoinsRequest
            {
                UserId = userId,
                AddCoinsAmount = coins
            }
        };

        Debug.Log($"[socket] Sending AddCoinsRequest with CoinsAmount: {coins}");

        byte[] message = SerializeProtobuf(request);
        await _networkStream.WriteAsync(message, 0, message.Length);
    }

    // 서버에서 오는 메세지 수신 (코루틴 버전) -> 서버에서 데이터를 받는 부분은 메인스레드에서 동작해야 함
    private IEnumerator ReceiveServerMessagesCoroutine()
    {
        while (_isConnected)
        {
            if (_networkStream.DataAvailable)
            {
                ClientResponse response = null;
                try
                {
                    response = DeserializeProtobuf<ClientResponse>(_networkStream);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[socket] Error deserializing server response: {ex.Message}");
                }

                if (response != null)
                {
                    switch (response.ResponseType)
                    {
                        case "GameState":
                            if (response.GameState != null)
                            {
                                Debug.Log($"[socket] GameState received");
                                OnGameStateResponsee.Invoke(response.GameState);
                            }
                            break;
                        case "GameUserState":
                            if (response.GameUserState != null)
                            {
                                Debug.Log("[socket] GameUserState received");
                                OnGameUserStateResponse.Invoke(response.GameUserState);
                            }
                            break;
                        case "BetResponse":
                            if (response.BetResponseData != null)
                            {
                                OnBetResponse.Invoke(response.BetResponseData);
                            }
                            break;
                        case "AddCoinsResponse":
                            if (response.AddCoinsResponseData != null)
                            {
                                Debug.Log("[socket] AddCoinsResponse received");
                                onAddCoinsResponse.Invoke(response.AddCoinsResponseData);
                            }
                            break;
                        default:
                            Debug.LogWarning($"[socket] Unknown response type received");
                            break;
                    }
                }
            }

            // 대기 후 반복 (0.1초 대기)
            yield return new WaitForSeconds(0.1f);
        }
    }

    public async Task WaitForConnection()
    {
        while (!_isConnected)
        {
            Debug.Log("[socket] Waiting for server connection...");
            await Task.Delay(100); // 0.1초 대기
        }
        Debug.Log("[socket] Server connection established.");
    }

    private static byte[] SerializeProtobuf<T>(T obj)
    {
        using var memoryStream = new MemoryStream();
        Serializer.SerializeWithLengthPrefix(memoryStream, obj, PrefixStyle.Base128);
        return memoryStream.ToArray();
    }

    private static T DeserializeProtobuf<T>(NetworkStream networkStream)
    {
        return Serializer.DeserializeWithLengthPrefix<T>(networkStream, PrefixStyle.Base128);
    }
}
