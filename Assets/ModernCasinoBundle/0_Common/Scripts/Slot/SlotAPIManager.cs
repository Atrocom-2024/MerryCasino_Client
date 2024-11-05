using Mkey;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SlotAPIManager: MonoBehaviour
{
    public static SlotAPIManager Instance { get; private set; }
    private const string apiBaseUrl = "http://localhost:3000/api/players";

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
        }
    }

    // GetCoins 메서드: 서버에서 코인 데이터를 가져와 콜백으로 반환
    public IEnumerator GetCoins(string playerId, Action<long> onSuccess, Action<string> onError)
    {
        string url = $"{apiBaseUrl}/{SlotPlayer.Instance.Id}";
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

    // SetCoins 메서드: 코인 수를 서버에 반영하는 메서드
    public IEnumerator SetCoins(string playerId, long coins, Action onSuccess, Action<string> onError)
    {
        string url = $"{apiBaseUrl}/{SlotPlayer.Instance.Id}";
        var data = new Dictionary<string, long>{ { "coins", coins } };
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
}
