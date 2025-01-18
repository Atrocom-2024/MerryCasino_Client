using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Mkey;

public class GooglePlayGamesScript : MonoBehaviour
{
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer�� �̱��� �ν��Ͻ�. ���� �÷��̾��� ������(ID�� ����)�� �����ϰ� �ε��ϴ� �� ���
    
    private readonly UserInfo userInfo = new UserInfo(); // ���� �����͸� �����ϴ� ��ü��, ID�� ����(COIN) ������ ����
    private string url;
    private PopUpsController loadingPopup;

    void Awake()
    {
        EnvReader.Load(".env");
        url = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/users";
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
        // ������ �α��� ��û�� ������, ���� ������ �������� ���� �ڷ�ƾ
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{userInfo.id}")) // �޸� ���� ������ ���� using ���
        {
            // yield return�� ����� �񵿱������� ��û�� ��ٸ��鼭 ���� �����尡 ������ �ʰ� �ٸ� �۾��� �� �� �ְ� ��
            yield return request.SendWebRequest(); // �ڷ�ƾ�� �Ͻ� ���� ������ �ϸ�, ��Ʈ��ũ ������ ���� �ٽ� ������ �簳

            // �������� ���� �����͸� ã�� ���� ��� ȸ������ �ڷ�ƾ�� ���� ������ �����ϰ� �ٽ� GET�� ���� ���� �����͸� �ҷ���
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // ���� �ڵ尡 404�� ��� ȸ������ �������� �Ѿ���� ó��
                if (request.responseCode == 404)
                {
                    Debug.Log("������ ã�� ���߽��ϴ�. ȸ�������� �����մϴ�.");
                    yield return StartCoroutine(SignUp(provider));

                    // ȸ������ �� �α���
                    StartCoroutine(SignIn(provider));
                }
                else
                {
                    // �ٸ� ���� ó��
                    Debug.Log($"Error: {request.error})");

                    CloseLoadingPopup();
                }
                
            }
            // �������� ���� �����͸� ã�� ��� JSON ���� �Ľ� �� ��ü�� ID�� COIN�� �����ϰ� �κ�� �̵�
            else
            {
                // �������� ���� ���� JSON �����͸� �Ľ�
                string jsondata = request.downloadHandler.text;
                LoginResponseDataType myObject = JsonUtility.FromJson<LoginResponseDataType>(jsondata);
                Debug.Log(jsondata);
                MPlayer.Id = myObject.userId;
                MPlayer.Coins = myObject.coins;
                LoadLobby();
            }
        }
    }

    // ���� �����͸� JSON �������� ������ ������ ȸ�������� ó���ϴ� �ڷ�ƾ
    IEnumerator SignUp(string provider)
    {
        SignUpData data = new SignUpData
        {
            userId = userInfo.id,
            provider = provider,
            deviceId = SystemInfo.deviceUniqueIdentifier
        };

        var requestData = JsonUtility.ToJson(data);

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errmsg = request.downloadHandler.text;
            Debug.Log(errmsg);

            CloseLoadingPopup();
        }
        else
        {
            string sucmsg = request.downloadHandler.text;
            Debug.Log(sucmsg);
        }
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
            Mkey.GuiController guiController = FindObjectOfType<Mkey.GuiController>();
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
