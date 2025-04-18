using System;
using System.Collections;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Mkey;
using Newtonsoft.Json;

public class GooglePlayGamesScript : MonoBehaviour
{
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer�� �̱��� �ν��Ͻ�. ���� �÷��̾��� ������(ID�� ����)�� �����ϰ� �ε��ϴ� �� ���
    
    private readonly UserInfo userInfo = new(); // ���� �����͸� �����ϴ� ��ü��, ID�� ����(COIN) ������ ����
    private string authApiUrl;

    private GuiController MGUI { get { return GuiController.Instance; } }

    void Awake()
    {
        EnvReader.Load(".env");
        authApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/auth";
    }

    void Start()
    {
        Debug.Log("Starting Google Play Games initialization");

        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Activate();
            Debug.Log("PlayGamesPlatform activated");
        }
    }

    //���� �α��� -> ���� �÷��� �� ��� �� ����
    public void LoginGooglePlayGames()
    {
        Debug.Log("Attempting Google Play Games login");

        SceneLoader.Instance.ShowLoadingPopup();

        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login successful. User ID: " + Social.localUser.id);
                userInfo.id = Social.localUser.id;

                // Request server auth code
                PlayGamesPlatform.Instance.RequestServerSideAccess(false, (code) =>
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        Debug.LogError("Failed to get server auth code");
                        SceneLoader.Instance.CloseLoadingPopup();
                        return;
                    }

                    StartCoroutine(AuthWithGooglePlayPlayGames(code));
                });
            }
            else
            {
                Debug.LogError("Login failed. Error: " + SignInStatus.InternalError);

                SceneLoader.Instance.CloseLoadingPopup();
                MGUI.ShowMessageWithCloseButton("Login faild", "\n\nPlease check your google play login status or google play profile.", () => { });
            }
        });
    }

    // ����̽� ���� ID�� �̿��� �Խ�Ʈ �α���
    public void LoginGuest()
    {
        Debug.Log("Attempting guest login");

        SceneLoader.Instance.ShowLoadingPopup();

        userInfo.id = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(AuthWithGuest());
    }

    public IEnumerator AuthWithGuest()
    {
        var requestUrl = $"{authApiUrl}/guest"; // ��û url
        var bodyData = new GuestAuthRequestBody
        {
            UserId = userInfo.id
        };
        var bodyJsonData = JsonConvert.SerializeObject(bodyData);

        // APIManager�� ���� GET ��û ����
        yield return StartCoroutine(APIManager.Instance.PostRequest(
            requestUrl,
            bodyJsonData,
            onSuccess: (jsonData) =>
            {
                // ������� JSON ������ �Ľ� �� ó��
                Debug.Log("Login successful. User ID: " + userInfo.id);

                var responseData = JsonConvert.DeserializeObject<LoginResponseDataType>(jsonData);
                MPlayer.Id = responseData.userId;
                MPlayer.Coins = responseData.coins;
                MPlayer.SetMinWinCoins((int)(MPlayer.Coins * 0.01M));
                LoadLobby();
            },
            onError: (request) =>
            {
                Debug.Log($"Error: {request.error}");
                SceneLoader.Instance.CloseLoadingPopup();
            }
        ));
    }

    public IEnumerator AuthWithGooglePlayPlayGames(string authCode)
    {
        var requestUrl = $"{authApiUrl}/google"; // ��û url
        var bodyData = new GoogleAuthRequestBody {
            UserId = userInfo.id,
            AuthCode = authCode
        };
        var bodyJsonData = JsonConvert.SerializeObject(bodyData);
        yield return StartCoroutine(APIManager.Instance.PostRequest(
            requestUrl,
            bodyJsonData,
            onSuccess: (jsonData) =>
            {
                Debug.Log("Login successful. User ID: " + Social.localUser.id);

                var userInfo = JsonConvert.DeserializeObject<LoginResponseDataType>(jsonData);
                MPlayer.Id = userInfo.userId;
                MPlayer.Coins = userInfo.coins;
                MPlayer.SetMinWinCoins((int)(MPlayer.Coins * 0.01M));
                LoadLobby();
            },
            onError: (request) =>
            {
                Debug.Log($"Error: {request.error}");
                SceneLoader.Instance.CloseLoadingPopup();
            }
        ));
    }

    // �κ� ������ �̵��ϴ� �޼���
    private void LoadLobby()
    {
        SceneLoader.Instance.LoadScene(1); // ���� �ε����� 1�� ����
    }

    public class UserInfo
    {
        public string id;
        public long coins;
        public UserInfo()
        {
            id = "";
            coins = 0;
        }
    }

    public class LoginResponseDataType
    {
        public string userId;
        public string nickname;
        public int level;
        public long coins;
    }

    public class GoogleAuthRequestBody
    {
        public string UserId;
        public string AuthCode;
    }

    public class GuestAuthRequestBody
    {
        public string UserId;
    }

    public class SignUpData
    {
        public string userId;
        public string provider;
    }
}
