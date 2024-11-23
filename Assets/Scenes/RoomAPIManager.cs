using Mkey;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

public class RoomAPIManager : MonoBehaviour
{
    public static RoomAPIManager Instance { get; private set; }
    private string playersApiUrl;
    private string roomsApiUrl;

    private void Awake()
    {
        EnvReader.Load(".env");
        playersApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/players";
        roomsApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/rooms";

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keeps the instance between scenes
        }
        else
        {
            Destroy(gameObject); // Ensures only one instance exists
        }
    }

    /// <summary>
    /// 서버에서 코인 데이터를 가져와 콜백으로 반환
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public IEnumerator GetCoins(string playerId, Action<long> onSuccess, Action<string> onError)
    {
        string url = $"{playersApiUrl}/{SlotPlayer.Instance.Id}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            Debug.Log(request.downloadHandler.text);
            if (request.result == UnityWebRequest.Result.Success)
            {
                long coins = long.Parse(request.downloadHandler.text);
                onSuccess.Invoke(coins); // 서버에서 받은 코인 데이터를 설정
            }
            else
            {
                onError.Invoke("Failed to load coins from server: " + request.error); // 실패 시 기본값으로 설정
            }
        }
    }

    /// <summary>
    /// 코인 수를 서버에 반영하는 메서드
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="coins"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public IEnumerator SetCoins(string playerId, long coins, Action onSuccess, Action<string> onError)
    {
        string url = $"{playersApiUrl}/{SlotPlayer.Instance.Id}";
        var data = new Dictionary<string, long> { { "coins", coins } };
        var jsonData = JsonConvert.SerializeObject(data);
        var request = new UnityWebRequest(url, "PATCH");

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess.Invoke();
        }
        else
        {
            onError.Invoke("Failed to update coins on server: " + request.error);
        }
    }

    /// <summary>
    /// 특정 룸의 목표 지급률(targetPayout) 조회
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="basePayout"></param>
    /// <param name="plusPayout"></param>
    /// <returns></returns>
    public IEnumerator GetPayout(int roomNumber, Action<string> onSuccess, Action<string> onError)
    {
        string reqUrl = $"{roomsApiUrl}/{roomNumber}/payout";
        using (UnityWebRequest request = UnityWebRequest.Get(reqUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                string errmsg = request.downloadHandler.text;
                onError?.Invoke($"Payout request error: {errmsg}");
            }
            else
            {
                string sucmsg = request.downloadHandler.text;
                Debug.Log($"Success Massage: {sucmsg}");
                onSuccess?.Invoke(sucmsg);
            }
        }
    }

    public IEnumerator GetTargetPayout(int roomNumber, double basePayout, double plusPayout, Action<double> onSuccess, Action<string> onError)
    {
        string reqUrl = $"{roomsApiUrl}/{roomNumber}/target-payout";
        using (UnityWebRequest request = UnityWebRequest.Get(reqUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    double targetPayout = double.Parse(request.downloadHandler.text);
                    plusPayout = targetPayout - basePayout;
                    onSuccess.Invoke(plusPayout); // 성공 시 plusPayout 값을 콜백으로 전달
                }
                catch (Exception ex)
                {
                    onError.Invoke($"Error parsing target payout: {ex.Message}");
                }
            }
            else
            {
                onError.Invoke("Failed to load target payout from server: " + request.error); // 실패 시 오류 메시지 콜백으로 전달
            }
        }
    }

    /// <summary>
    /// 서버에 주기적으로 요청을 보내 특정 조건(errmsg가 "true"인 경우)이 발생하면 onReturnEvent()를 호출
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="onReturnEvent"></param>
    /// <returns></returns>
    public IEnumerator GetServer(int roomNumber, Action onReturnEvent)
    {
        string reqUrl = $"{roomsApiUrl}/{roomNumber}/payout-return";
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(reqUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errmsg = request.downloadHandler.text;
                    Debug.LogError($"Server error: {errmsg}");
                }
                else
                {
                    string errmsg = request.downloadHandler.text;
                    if (errmsg == "true")
                    {
                        Debug.Log("Condition met: triggering return event");
                        onReturnEvent.Invoke(); // 조건이 충족되면 onReturnEvent 콜백 호출
                        break; // 필요에 따라 while 루프를 종료할 수 있습니다
                    }
                    else
                    {
                        Debug.Log("Condition not met: " + errmsg);
                    }
                }
            }
            yield return new WaitForSeconds(5); // 5초 동안 대기
        }
    }

    /// <summary>
    /// 특정 room의 totalbet 갱신
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="bet"></param>
    /// <returns></returns>
    public IEnumerator SetTotalBet(int roomNumber, int bet)
    {
        // 요청 URL 설정
        string requestUrl = $"{roomsApiUrl}/{roomNumber}/bet";

        // POST 데이터를 JSON 형식으로 생성
        Dictionary<string, long> data = new Dictionary<string, long> { { "betCoins", bet } };
        string jsonData = JsonConvert.SerializeObject(data);

        UnityWebRequest request = new UnityWebRequest(requestUrl, "POST");

        // Body에 JSON 데이터를 추가
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송
        yield return request.SendWebRequest();

        // 결과 확인
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request succeeded");
            Debug.Log($"Server Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Request failed: {request.error}");
            Debug.LogError($"Server Response: {request.downloadHandler.text}");
        }
    }

    /// <summary>
    /// 특정 room의 totalPayout 갱신
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="bet"></param>
    /// <returns></returns>
    public IEnumerator SetTotalPayout(int roomNumber, int bet)
    {
        // 요청 URL 설정
        string requestUrl = $"{roomsApiUrl}/{roomNumber}/win";

        // POST 데이터를 JSON 형식으로 생성
        Dictionary<string, long> data = new Dictionary<string, long> { { "betCoins", bet } };
        string jsonData = JsonConvert.SerializeObject(data);

        UnityWebRequest request = new UnityWebRequest(requestUrl, "POST");

        // Body에 JSON 데이터 추가
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송
        yield return request.SendWebRequest();

        // 결과 확인
        // 결과 확인
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request succeeded");
            Debug.Log($"Server Response: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Request failed: {request.error}");
            Debug.LogError($"Server Response: {request.downloadHandler.text}");
        }
    }
}
