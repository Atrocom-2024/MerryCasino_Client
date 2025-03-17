using Mkey;
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
    public event Action<BetResponse> OnBetResponse;
    public event Action<AddCoinsResponse> OnAddCoinsResponse;
    public event Action<JackpotWinResponse> OnJackpotWinResponse;
    public event Action<GameState> OnGameState;
    public event Action<GameUserState> OnGameUserState;
    public event Action<GameSessionEnd> OnGameSessionEnd;

    // get/set
    public bool IsConnected { get { return _isConnected; } }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 서버 연결 초기화
    public async Task ConnectToServer(string serverAddress, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverAddress, port);
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

            if (_networkStream != null)
            {
                _networkStream.Close();
                _networkStream.Dispose();
                _networkStream = null;
            }
            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }

            StopAllCoroutines(); // 모든 코루틴 중지
            Debug.Log("[socket] Disconnected from server");
        }
    }

    // 룸 조인 요청
    public async Task SendRoomJoinRequest(string userId, int roomId)
    {
        await SendClientMessagesAsync("JoinRoomRequest", new JoinRoomRequest
        {
            UserId = userId,
            RoomId = roomId
        });
    }

    // 배팅 요청
    public async Task SendBetReqeust(string userId, int betAmount)
    {
        await SendClientMessagesAsync("BetRequest", new BetRequest
        {
            UserId = userId,
            BetAmount = betAmount,
            RoomType = 1
        });
    }

    public async Task SendAddCoinsRequest(string userId, int coins)
    {
        await SendClientMessagesAsync("AddCoinsRequest", new AddCoinsRequest
        {
            UserId = userId,
            AddCoinsAmount = coins
        });
    }

    public async Task SendJackpotWinRequest(string jackpotType, int jackpotCoins)
    {
        await SendClientMessagesAsync("JackpotWinRequest", new JackpotWinRequest
        {
            JackpotType = jackpotType,
            JackpotWinCoins = jackpotCoins
        });
    }

    private async Task SendClientMessagesAsync<T>(string requestType, T requestData)
    {
        if (!_isConnected)
        {
            Debug.LogError($"[socket] Cannot send {requestType}: Not connected to server!");
            return;
        }

        var request = new ClientRequest { RequestType = requestType };

        switch (requestType)
        {
            case "JoinRoomRequest":
                request.JoinRoomData = requestData as JoinRoomRequest;
                break;
            case "BetRequest":
                request.BetData = requestData as BetRequest;
                break;
            case "AddCoinsRequest":
                request.AddCoinsData = requestData as AddCoinsRequest;
                break;
            case "JackpotWinRequest":
                request.JackpotWinData = requestData as JackpotWinRequest;
                break;
            default:
                Debug.LogError($"[socket] Unknown request type: {requestType}");
                return;
        }

        byte[] message = SerializeProtobuf(request);
        await _networkStream.WriteAsync(message, 0, message.Length);

        Debug.Log($"[socket] Sent {requestType}");
    }

    // 서버에서 오는 메세지 수신 (코루틴 버전) -> 서버에서 데이터를 받는 부분은 메인스레드에서 동작해야 함
    private IEnumerator ReceiveServerMessagesCoroutine()
    {
        while (_isConnected)
        {
            // _client 또는 _networkStream이 null이면 종료
            if (_client == null || _networkStream == null)
            {
                Debug.LogWarning("[socket] Network stream is null. Stopping ReceiveServerMessagesCoroutine.");
                yield break; // 코루틴 종료
            }
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
                        case "BetResponse":
                            if (response.BetResponseData != null)
                            {
                                Debug.Log("[socket] BetResponse received");
                                OnBetResponse.Invoke(response.BetResponseData);
                            }
                            break;
                        case "AddCoinsResponse":
                            if (response.AddCoinsResponseData != null)
                            {
                                Debug.Log("[socket] AddCoinsResponse received");
                                OnAddCoinsResponse.Invoke(response.AddCoinsResponseData);
                            }
                            break;
                        case "JackpotWinResponse":
                            if (response.JackpotWinResponseData != null)
                            {
                                Debug.Log("[socket] JackpotWinResponse received");
                                OnJackpotWinResponse.Invoke(response.JackpotWinResponseData);
                            }
                            break;
                        case "GameState":
                            if (response.GameState != null)
                            {
                                Debug.Log($"[socket] GameState received");
                                OnGameState.Invoke(response.GameState);
                            }
                            break;
                        case "GameUserState":
                            if (response.GameUserState != null)
                            {
                                Debug.Log("[socket] GameUserState received");
                                OnGameUserState.Invoke(response.GameUserState);
                            }
                            break;
                        case "GameSessionEnd":
                            if (response.GameSessionEndData != null)
                            {
                                Debug.Log("[socket] GameSessionEndResponse received");
                                OnGameSessionEnd.Invoke(response.GameSessionEndData);
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

    public IEnumerator WaitForConnectionCoroutine()
    {
        while (!_isConnected)
        {
            Debug.Log("[socket] Waiting for server connection...");
            yield return new WaitForSeconds(0.1f); // 0.1초 대기
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
