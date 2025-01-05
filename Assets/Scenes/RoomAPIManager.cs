using Mkey;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif

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

    public IEnumerator GetPlayerInfo(string playerId, Action<PlayerData> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            onError?.Invoke("Player ID cannot be null or empty.");
            yield break;
        }

        // Construct the URL for the API endpoint
        string url = $"{playersApiUrl}/{playerId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");

            // Send the request
            yield return request.SendWebRequest();

            // Check for network or server errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                string errorMessage = $"Error retrieving player data: {request.error}";
                onError?.Invoke(errorMessage);
                Debug.LogError(errorMessage);
            }
            else
            {
                // Successfully received a response
                try
                {
                    string responseText = request.downloadHandler.text;
                    PlayerData playerData = JsonUtility.FromJson<PlayerData>(responseText);

                    onSuccess?.Invoke(playerData); // Callback with parsed player data
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Error parsing player data: {ex.Message}");
                }
            }
        }
    }
}
