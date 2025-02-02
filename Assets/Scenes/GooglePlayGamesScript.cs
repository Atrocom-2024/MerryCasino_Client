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
    
    private readonly UserInfo userInfo = new UserInfo(); // ���� �����͸� �����ϴ� ��ü��, ID�� ����(COIN) ������ ����
    private string apiUrl;
    private PopUpsController loadingPopup;

    void Awake()
    {
        EnvReader.Load(".env");
        apiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/users";
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

        ShowLoadingPopup();

        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.Authenticate((success) =>
            {
                if (success == SignInStatus.Success)
                {
                    Debug.Log("Login successful. User ID: " + Social.localUser.id);
                    userInfo.id = Social.localUser.id;
                    StartCoroutine(SignIn("google"));
                }
                else
                {
                    Debug.LogError("Login failed. Error: " + SignInStatus.InternalError);
                    CloseLoadingPopup();
                }
            });
        }
        else
        {
            Debug.Log("Already authenticated");
            userInfo.id = Social.localUser.id;
            StartCoroutine(SignIn("google"));
        }
    }

    // ����̽� ���� ID�� �̿��� �Խ�Ʈ �α���
    public void LoginGuest()
    {
        Debug.Log("Attempting guest login");

        ShowLoadingPopup();

        userInfo.id = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(SignIn("guest"));
    }

    public IEnumerator SignIn(string provider)
    {
        // ��û url
        string requestUrl = $"{apiUrl}/{userInfo.id}";

        // APIManager�� ���� GET ��û ����
        yield return StartCoroutine(APIManager.Instance.GetRequest(
            requestUrl,
            onSuccess: (jsonData) =>
            {
                // ������� JSON ������ �Ľ� �� ó��
                Debug.Log("�α��� ����");
                var userInfo = JsonConvert.DeserializeObject<LoginResponseDataType>(jsonData);
                MPlayer.Id = userInfo.userId;
                MPlayer.Coins = userInfo.coins;
                LoadLobby();
            },
            onError: (request) =>
            {
                // ������ �߻��� ��� HTTP ���� �ڵ� Ȯ��
                if (request.responseCode == 404)
                {
                    Debug.Log("������ ã�� ���߽��ϴ�. ȸ�������� �����մϴ�.");

                    // ȸ������ �ڷ�ƾ ���� �� ��α��� �õ�
                    StartCoroutine(SignUp(provider));
                }
                else
                {
                    Debug.Log($"Error: {request.error}");
                    CloseLoadingPopup();
                }
            }
        ));
    }

    // ���� �����͸� JSON �������� ������ ������ ȸ�������� ó���ϴ� �ڷ�ƾ
    private IEnumerator SignUp(string provider)
    {
        // ȸ������ ������ ����
        SignUpData data = new SignUpData
        {
            userId = userInfo.id,
            provider = provider,
            deviceId = SystemInfo.deviceUniqueIdentifier
        };

        // JSON ���ڿ��� ��ȯ
        var requestData = JsonUtility.ToJson(data);

        // APIManager�� ���� POST ��û ����
        yield return APIManager.Instance.PostRequest(
            apiUrl,
            requestData,
            onSuccess: (sucmsg) =>
            {
                Debug.Log("ȸ������ ����");
                StartCoroutine(SignIn(provider));
            },
            onError: (request) =>
            {
                string errmsg = request.downloadHandler.text;
                Debug.Log($"ȸ������ ����: {errmsg}");
                CloseLoadingPopup();
            }
        );
    }

    // �κ� ������ �̵��ϴ� �޼���
    private void LoadLobby()
    {
        Debug.Log("go Lobby");
        SceneLoader.Instance.LoadScene(1); // ���� �ε����� 1�� ����
    }

    private void ShowLoadingPopup()
    {
        if (loadingPopup == null)
        {
            loadingPopup = SceneLoader.Instance.LoadGroupPrefab;
            GuiController guiController = FindObjectOfType<GuiController>();
            guiController.ShowPopUp(loadingPopup);
        }
    }

    private void CloseLoadingPopup()
    {
        if (loadingPopup != null)
        {
            loadingPopup.CloseWindow();
            loadingPopup = null;
        }
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

    public class SignUpData
    {
        public string userId;
        public string provider;
        public string? deviceId;
    }
}
