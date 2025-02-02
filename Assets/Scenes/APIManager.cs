using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }

    private void Awake()
    {
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

    public IEnumerator GetRequest(string url, Action<string> onSuccess, Action<UnityWebRequest> onError)
    {
        using var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            onError.Invoke(request);
        }
        else
        {
            onSuccess.Invoke(request.downloadHandler.text);
        }
    }

    public IEnumerator PostRequest(string requestUrl, string jsonData, Action<string> onSuccess, Action<UnityWebRequest> onError)
    {
        var request = new UnityWebRequest(requestUrl, "POST");

        // 요청 바디 설정
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // 에러 처리
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            onError.Invoke(request);
        }
        else
        {
            onSuccess.Invoke(request.downloadHandler.text);
        }
    }

    public IEnumerator ProcessGooglePayment(string requestUrl, string userId, string receipt, Action<long> onSuccess, Action<string> onFailure)
    {
        // 영수증 JSON 파싱
        var googleReceiptRoot = JsonConvert.DeserializeObject<GooglePlayReceipt>(receipt) ?? throw new JsonException("Failed to deserialize Google Play receipt.");
        var googleReceiptPayload = JsonConvert.DeserializeObject<GooglePlayReceiptPayload>(googleReceiptRoot.Payload) ?? throw new JsonException("Failed to deserialize Google Play receipt.");
        var googleReceipt = JsonConvert.DeserializeObject<GooglePlayReceiptJson>(googleReceiptPayload.json) ?? throw new JsonException("Failed to deserialize Google Play receipt.");
        

        // 요청 데이터 생성
        var bodyData = new ProcessGooglePaymentRequest
        {
            UserId = userId,
            Receipt = googleReceipt,
            Store = "Google"
        };

        var jsonBodyData = JsonUtility.ToJson(bodyData);

        var request = new UnityWebRequest(requestUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBodyData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // 응답 처리
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 서버 응답 검증
                var processPaymentResponse = JsonConvert.DeserializeObject<ProcessGooglePaymentResponse>(request.downloadHandler.text);
                if (processPaymentResponse.IsProcessed)
                {
                    onSuccess.Invoke(processPaymentResponse.ProcessedResultCoins);
                }
                else
                {
                    onFailure.Invoke("Purchase validation failed on the server.");
                }
            }
            else
            {
                onFailure.Invoke($"Request Error: {request.error}");
            }

        }
        catch (Exception ex)
        {
            onFailure.Invoke("Error validating receipt: " + ex.Message);
        }
    }
}

[Serializable]
public class ProcessGooglePaymentRequest
{
    public string UserId;
    public GooglePlayReceiptJson Receipt;
    public string Store;
}

[Serializable]
public class ProcessGooglePaymentResponse
{
    public bool IsProcessed;
    public string TransactionId;
    public long ProcessedResultCoins;
    public string Message;
}

[Serializable]
public class GooglePlayReceipt
{
    public string Payload;
}

[Serializable]
public class GooglePlayReceiptPayload
{
    public string json;
    public string signature;
}

[Serializable]
public class GooglePlayReceiptJson
{
    public string orderId;
    public string packageName;
    public string productId;
    public string purchaseToken;
}