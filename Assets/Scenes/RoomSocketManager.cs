using ProtoBuf;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class RoomSocketManager : MonoBehaviour
{
    public static RoomSocketManager Instance { get; private set; } // �̱��� �ν��Ͻ�

    private TcpClient _client;
    private NetworkStream _networkStream;
    private bool _isConnected = false;

    // �̺�Ʈ ����
    public event Action<GameUserState> OnRoomJoinResponse;
    public event Action<GameSession> OnGameStateUpdate;

    private void Awake()
    {
        // �̱��� �ʱ�ȭ
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Unity ���� �� Ŭ���̾�Ʈ ���� ����
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    // ���� ���� �ʱ�ȭ
    public async Task ConnectToServer(string ipAddress, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ipAddress, port);
            _networkStream = _client.GetStream();
            _isConnected = true;

            Debug.Log("[socket] Connected to the server!");

            // �������� ���� �޼��� ���� ����
            //_ = Task.Run(ReceiveServerMessagesAsync);
            StartCoroutine(ReceiveServerMessagesCoroutine());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[socket] Error connecting to server: {ex.Message}");
        }
    }

    // Ŭ���̾�Ʈ ���� ����
    public void Disconnect()
    {
        if (_isConnected)
        {
            _isConnected = false;
            _networkStream.Close();
            _client.Close();
            Debug.Log("[socket] Disconnected from server");
        }
    }

    // �� ���� ��û
    public async Task SendRoomJoinRequest(string userId, int roomId)
    {
        if (!_isConnected)
        {
            Debug.LogError("[socket] Not connected to server!");
            return;
        }

        var reqeust = new ClientRequest
        {
            RequestType = "JoinRoom",
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

    // ���� ��û
    public async Task SendBetReqeust(string userId, int betAmount)
    {
        if (!_isConnected) return;

        var reqeust = new ClientRequest
        {
            RequestType = "Bet",
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

    // �������� ���� �޼��� ���� (�ڷ�ƾ ����) -> �������� �����͸� �޴� �κ��� ���ν����忡�� �����ؾ� ��
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
                                OnGameStateUpdate.Invoke(response.GameState);
                            }
                            break;
                        case "GameUserState":
                            if (response.GameUserState != null)
                            {
                                Debug.Log("[socket] GameUserState received");
                                OnRoomJoinResponse.Invoke(response.GameUserState);
                            }
                            break;
                        default:
                            Debug.LogWarning($"[socket] Unknown response type received");
                            break;
                    }
                }
            }

            // ��� �� �ݺ� (0.1�� ���)
            yield return new WaitForSeconds(0.1f);
        }
    }

    public async Task WaitForConnection()
    {
        while (!_isConnected)
        {
            Debug.Log("[socket] Waiting for server connection...");
            await Task.Delay(100); // 0.1�� ���
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
