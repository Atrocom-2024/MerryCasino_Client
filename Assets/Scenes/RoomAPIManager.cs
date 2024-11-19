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
    /// Ư�� ���� ��ǥ ���޷�(targetPayout) ��ȸ
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
