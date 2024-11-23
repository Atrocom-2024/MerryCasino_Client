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
    /// �������� ���� �����͸� ������ �ݹ����� ��ȯ
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
                onSuccess.Invoke(coins); // �������� ���� ���� �����͸� ����
            }
            else
            {
                onError.Invoke("Failed to load coins from server: " + request.error); // ���� �� �⺻������ ����
            }
        }
    }

    /// <summary>
    /// ���� ���� ������ �ݿ��ϴ� �޼���
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
    /// Ư�� ���� ��ǥ ���޷�(targetPayout) ��ȸ
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
                    onSuccess.Invoke(plusPayout); // ���� �� plusPayout ���� �ݹ����� ����
                }
                catch (Exception ex)
                {
                    onError.Invoke($"Error parsing target payout: {ex.Message}");
                }
            }
            else
            {
                onError.Invoke("Failed to load target payout from server: " + request.error); // ���� �� ���� �޽��� �ݹ����� ����
            }
        }
    }

    /// <summary>
    /// ������ �ֱ������� ��û�� ���� Ư�� ����(errmsg�� "true"�� ���)�� �߻��ϸ� onReturnEvent()�� ȣ��
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
                        onReturnEvent.Invoke(); // ������ �����Ǹ� onReturnEvent �ݹ� ȣ��
                        break; // �ʿ信 ���� while ������ ������ �� �ֽ��ϴ�
                    }
                    else
                    {
                        Debug.Log("Condition not met: " + errmsg);
                    }
                }
            }
            yield return new WaitForSeconds(5); // 5�� ���� ���
        }
    }

    /// <summary>
    /// Ư�� room�� totalbet ����
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="bet"></param>
    /// <returns></returns>
    public IEnumerator SetTotalBet(int roomNumber, int bet)
    {
        // ��û URL ����
        string requestUrl = $"{roomsApiUrl}/{roomNumber}/bet";

        // POST �����͸� JSON �������� ����
        Dictionary<string, long> data = new Dictionary<string, long> { { "betCoins", bet } };
        string jsonData = JsonConvert.SerializeObject(data);

        UnityWebRequest request = new UnityWebRequest(requestUrl, "POST");

        // Body�� JSON �����͸� �߰�
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ��û ����
        yield return request.SendWebRequest();

        // ��� Ȯ��
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
    /// Ư�� room�� totalPayout ����
    /// </summary>
    /// <param name="roomNumber"></param>
    /// <param name="bet"></param>
    /// <returns></returns>
    public IEnumerator SetTotalPayout(int roomNumber, int bet)
    {
        // ��û URL ����
        string requestUrl = $"{roomsApiUrl}/{roomNumber}/win";

        // POST �����͸� JSON �������� ����
        Dictionary<string, long> data = new Dictionary<string, long> { { "betCoins", bet } };
        string jsonData = JsonConvert.SerializeObject(data);

        UnityWebRequest request = new UnityWebRequest(requestUrl, "POST");

        // Body�� JSON ������ �߰�
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ��û ����
        yield return request.SendWebRequest();

        // ��� Ȯ��
        // ��� Ȯ��
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
