using Newtonsoft.Json;
using SocketIOClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class RoomSocketManager : MonoBehaviour
{
    public static RoomSocketManager Instance { get; private set; }
    private SocketIOUnity socket; // Socket.IO Unity Ŭ���̾�Ʈ ��ü

    private string domain; // ���� ������

    private void Awake()
    {
        EnvReader.Load(".env");
        domain = Environment.GetEnvironmentVariable("API_DOMAIN");

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Avoid duplicates
            return;
        }
    }

    void Start()
    {
        ConnectToSocket();
        Debug.Log("���� ���� ���� �õ�!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    }

    private void OnDestroy()
    {
        // WebSocket ���� ����
        DisconnectFromSocket();
    }

    /// <summary>
    /// ���� �� ���� �� WebSocket �ʱ�ȭ �� ����
    /// </summary>
    private void ConnectToSocket()
    {
        // Socket.IOUnity ��ü �ʱ�ȭ
        socket = new SocketIOUnity(domain, new SocketIOOptions
        {
            Reconnection = true,
            ReconnectionAttempts = 5,
            ReconnectionDelay = 2000,
            //Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("WebSocket connected to server!");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("WebSocket disconnected from server.");
        };

        socket.Connect();
    }

    /// <summary>
    /// ���� �� ���� �� WebSocket ���� ����
    /// </summary>
    public void DisconnectFromSocket()
    {
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
            Debug.Log("WebSocket connection closed.");
        }
    }

    /// <summary>
    /// ���� �����͸� ������ �ݿ�
    /// </summary>
    public IEnumerator SetCoins(string playerId, long coins, Action onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            onError?.Invoke("Player ID�� ��� �ֽ��ϴ�.");
            yield break;
        }

        // ������ ��û ������ ����
        var data = new { playerId, coins };
        bool responseReceived = false;
        string responseData = null;

        // ������ ��û ����
        socket.EmitAsync("updatePlayer", data);

        // ���� �̺�Ʈ ���
        socket.On("setCoinsResponse", response =>
        {
            responseData = response.ToString();
            responseReceived = true;
        });

        // ������ �� ������ ���
        yield return new WaitUntil(() => responseReceived);

        // ���� ó��
        if (responseData == "success")
        {
            onSuccess?.Invoke(); // ���� �ݹ� ȣ��
        }
        else
        {
            onError?.Invoke("Failed to update coins on server."); // ���� �ݹ� ȣ��
        }
    }

    /// <summary>
    /// Ư�� ���� ���޷�(payout) ��ȸ
    /// </summary>
    public void GetPayout(int roomNumber, Action<string> onSuccess, Action<string> onError)
    {
        socket.EmitAsync("getPayout", roomNumber);

        socket.On("getPayoutResponse", response =>
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());
                if (data.ContainsKey("resultplusPercent"))
                {
                    string payout = data["resultplusPercent"].ToString();
                    onSuccess?.Invoke(payout);
                }
                else
                {
                    onError?.Invoke("Invalid response format.");
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error parsing response: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Ư�� ���� ��ǥ ���޷�(targetPayout) ��ȸ
    /// </summary>
    public void GetTargetPayout(int roomNumber, Action<double> onSuccess, Action<string> onError)
    {
        socket.EmitAsync("getTargetPayout", roomNumber);

        socket.On("getTargetPayoutResponse", response =>
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());
                if (data.ContainsKey("targetPayout"))
                {
                    double targetPayout = Convert.ToDouble(data["targetPayout"]);
                    onSuccess?.Invoke(targetPayout);
                }
                else
                {
                    onError?.Invoke("Invalid response format.");
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error parsing response: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Ư�� ���� Total Bet ����
    /// </summary>
    public void SetTotalBet(int roomNumber, int bet)
    {
        var data = new { roomNumber, betCoin = bet };
        socket.EmitAsync("updateTotalBet", data);

        socket.On("updateTotalBetResponse", response =>
        {
            Debug.Log($"Total bet updated response: {response}");
        });
    }

    /// <summary>
    /// Ư�� ���� Total Payout ����
    /// </summary>
    public void SetTotalPayout(int roomNumber, int payout)
    {
        var data = new { roomNumber, payoutCoin = payout };
        socket.EmitAsync("updateTotalPayout", data);

        socket.On("updateTotalPayoutResponse", response =>
        {
            Debug.Log($"Total payout updated response: {response}");
        });
    }

    /// <summary>
    /// ���� ������ �����ϸ� Ư�� ���� �߻� �� �̺�Ʈ ȣ��
    /// </summary>
    public void MonitorServerCondition(int roomNumber, Action onReturnEvent)
    {
        socket.EmitAsync("monitorCondition", roomNumber);

        socket.On("monitorConditionResponse", response =>
        {
            if (response.ToString() == "true")
            {
                Debug.Log("Condition met: triggering return event");
                onReturnEvent?.Invoke();
            }
            else
            {
                Debug.Log("Condition not met: " + response);
            }
        });
    }
}
