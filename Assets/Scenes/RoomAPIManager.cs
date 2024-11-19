using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;

public class RoomAPIManager : MonoBehaviour
{
    public static RoomAPIManager Instance { get; private set; }
    private string apiBaseUrl;

    private void Awake()
    {
        EnvReader.Load(".env");
        apiBaseUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/rooms";

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
    /// 특정 룸의 목표 지급률(targetPayout) 조회
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="basePayout"></param>
    /// <param name="plusPayout"></param>
    /// <returns></returns>
    public IEnumerator GetTargetPayout(int roomNumber, double basePayout, double plusPayout, Action<double> onSuccess, Action<string> onError)
    {
        string reqUrl = $"{apiBaseUrl}/{roomNumber}/target-payout";
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
        string reqUrl = $"{apiBaseUrl}/{roomNumber}/payout-return";
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

    public IEnumerator GetPayout(int roomNumber, Action<string> onSuccess, Action<string> onError)
    {
        string reqUrl = $"{apiBaseUrl}/{roomNumber}/payout";
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
}
