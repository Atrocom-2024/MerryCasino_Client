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
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer의 싱글톤 인스턴스. 현재 플레이어의 데이터(ID와 코인)를 저장하고 로딩하는 데 사용
    
    private readonly UserInfo userInfo = new UserInfo(); // 유저 데이터를 저장하는 객체로, ID와 코인(COIN) 정보를 포함
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

    //구글 로그인 -> 구글 플레이 앱 등록 후 구현
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

    // 디바이스 고유 ID를 이용한 게스트 로그인
    public void LoginGuest()
    {
        Debug.Log("Attempting guest login");

        ShowLoadingPopup();

        userInfo.id = SystemInfo.deviceUniqueIdentifier;
        StartCoroutine(SignIn("guest"));
    }

    public IEnumerator SignIn(string provider)
    {
        // 서버에 로그인 요청을 보내고, 유저 정보를 가져오기 위한 코루틴
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{userInfo.id}")) // 메모리 누수 방지를 위해 using 사용
        {
            // yield return을 사용해 비동기적으로 요청을 기다리면서 메인 스레드가 멈추지 않고 다른 작업을 할 수 있게 함
            yield return request.SendWebRequest(); // 코루틴의 일시 정지 역할을 하며, 네트워크 응답이 오면 다시 실행을 재개

            // 서버에서 유저 데이터를 찾지 못한 경우 회원가입 코루틴을 통해 유저를 생성하고 다시 GET을 통해 유저 데이터를 불러옴
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 상태 코드가 404인 경우 회원가입 로직으로 넘어가도록 처리
                if (request.responseCode == 404)
                {
                    Debug.Log("유저를 찾지 못했습니다. 회원가입을 진행합니다.");
                    yield return StartCoroutine(SignUp(provider));

                    // 회원가입 후 로그인
                    StartCoroutine(SignIn(provider));
                }
                else
                {
                    // 다른 에러 처리
                    Debug.Log($"Error: {request.error})");

                    CloseLoadingPopup();
                }
                
            }
            // 서버에서 유저 데이터를 찾을 경우 JSON 응답 파싱 후 객체에 ID와 COIN을 저장하고 로비로 이동
            else
            {
                // 서버에서 응답 받은 JSON 데이터를 파싱
                string jsondata = request.downloadHandler.text;
                LoginResponseDataType myObject = JsonUtility.FromJson<LoginResponseDataType>(jsondata);
                Debug.Log(jsondata);
                MPlayer.Id = myObject.userId;
                MPlayer.Coins = myObject.coins;
                LoadLobby();
            }
        }
    }

    // 유저 데이터를 JSON 형식으로 서버에 보내어 회원가입을 처리하는 코루틴
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

    // 로비 씬으로 이동하는 메서드
    private void LoadLobby()
    {
        Debug.Log("go Lobby");
        SceneLoader.Instance.LoadScene(1); // 씬의 인덱스를 1로 설정
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
