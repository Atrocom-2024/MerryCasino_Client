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

    // GetCoins �޼���: �������� ���� �����͸� ������ �ݹ����� ��ȯ
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
                onSuccess.Invoke(coins); // �������� ���� ���� �����͸� ����
            }
            else
            {
                onError.Invoke("Failed to load coins from server: " + request.error); // ���� �� �⺻������ ����
            }
        }
    }

    // SetCoins �޼���: ���� ���� ������ �ݿ��ϴ� �޼���
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
