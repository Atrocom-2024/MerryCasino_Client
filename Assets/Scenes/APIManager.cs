using Mkey;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }
    private string usersApiUrl;
    private string paymentsApiUrl;

    private void Awake()
    {
        EnvReader.Load(".env");
        usersApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/users";
        paymentsApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/payments";

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

    public IEnumerator GetPlayerInfo(string userId, Action<UserData> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(userId))
        {
            onError.Invoke("User ID cannot be null or empty.");
            yield break;
        }

        // Construct the URL for the API endpoint
        string url = $"{usersApiUrl}/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for network or server errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                string errorMessage = $"Error retrieving user data: {request.error}";
                onError?.Invoke(errorMessage);
                Debug.LogError(errorMessage);
            }
            else
            {
                // Successfully received a response
                try
                {
                    string responseText = request.downloadHandler.text;
                    UserData userData = JsonUtility.FromJson<UserData>(responseText);

                    onSuccess?.Invoke(userData); // Callback with parsed player data
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Error parsing user data: {ex.Message}");
                }
            }
        }
    }

    public IEnumerator PutUserAddCoins(string userId, int addCoins, Action<UserData> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(userId))
        {
            onError.Invoke("User ID cannot be null or empty.");
            yield break;
        }

        string url = $"{usersApiUrl}/{userId}";
        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        PutUserRequest bodyData = new PutUserRequest
        {
            UserId = userId,
            AddCoins = addCoins
        };
        string jsonBodyData = JsonUtility.ToJson(bodyData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBodyData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청을 보낸 후 응답 기다리기
        yield return request.SendWebRequest();

        // 응답 처리
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("PUT request completed successfully!");
            try
            {
                UserData responseData = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
                onSuccess.Invoke(responseData);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse response: " + ex.Message);
                onError.Invoke("Failed to parse response data.");
            }
        }
        else
        {
            Debug.LogError("PUT request failed: " + request.error);
            onError.Invoke(request.error);
        }
    }

    public IEnumerator ValidateGoogleReceipt(string receipt, string productId, Action onSuccess, Action<string> onFailure)
    {
        // 서버 URL 설정
        string url = $"{paymentsApiUrl}/validate-receipt";
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        // 요청 데이터 생성
        var bodyData = new
        {
            Receipt = receipt,
            ProductId = productId,
            Store = "Google"
        };

        // JSON 데이터 직렬화
        string jsonBodyData = JsonUtility.ToJson(bodyData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBodyData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // 응답 처리
        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Server Response: " + request.downloadHandler.text);

                // 서버 응답 검증
                var validationResponse = JsonUtility.FromJson<ValidationResponse>(request.downloadHandler.text);
                if (validationResponse.IsValid)
                {
                    onSuccess.Invoke();
                }
                else
                {
                    onFailure.Invoke("Purchase validation failed on the server.");
                }
            }
            else
            {
                onFailure.Invoke(request.error);
            }

        }
        catch (Exception ex)
        {
            onFailure.Invoke("Error validating receipt: " + ex.Message);
        }
    }
}

[Serializable]
public class PutUserRequest
{
    public string UserId;
    public int AddCoins;
}

[Serializable]
public class ValidationResponse
{
    public bool IsValid;
    public string TransactionId;
    public int PurchasedCoins;
}