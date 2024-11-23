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
    private SocketIOUnity socket; // Socket.IO Unity 클라이언트 객체

    private string domain; // 서버 도메인

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
        Debug.Log("소켓 서버 연결 시도!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    }

    private void OnDestroy()
    {
        // WebSocket 연결 종료
        DisconnectFromSocket();
    }

    /// <summary>
    /// 게임 룸 입장 시 WebSocket 초기화 및 연결
    /// </summary>
    private void ConnectToSocket()
    {
        // Socket.IOUnity 객체 초기화
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
    /// 게임 룸 퇴장 시 WebSocket 연결 해제
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
    /// 코인 데이터를 서버에 반영
    /// </summary>
    public IEnumerator SetCoins(string playerId, long coins, Action onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            onError?.Invoke("Player ID가 비어 있습니다.");
            yield break;
        }

        // 서버에 요청 데이터 생성
        var data = new { playerId, coins };
        bool responseReceived = false;
        string responseData = null;

        // 서버에 요청 전송
        socket.EmitAsync("updatePlayer", data);

        // 응답 이벤트 등록
        socket.On("setCoinsResponse", response =>
        {
            responseData = response.ToString();
            responseReceived = true;
        });

        // 응답이 올 때까지 대기
        yield return new WaitUntil(() => responseReceived);

        // 응답 처리
        if (responseData == "success")
        {
            onSuccess?.Invoke(); // 성공 콜백 호출
        }
        else
        {
            onError?.Invoke("Failed to update coins on server."); // 실패 콜백 호출
        }
    }

    /// <summary>
    /// 특정 룸의 지급률(payout) 조회
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
    /// 특정 룸의 목표 지급률(targetPayout) 조회
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
    /// 특정 룸의 Total Bet 갱신
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
    /// 특정 룸의 Total Payout 갱신
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
    /// 서버 조건을 감시하며 특정 조건 발생 시 이벤트 호출
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
