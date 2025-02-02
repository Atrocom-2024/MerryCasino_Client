using System;
using System.Collections;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Mkey;
using Newtonsoft.Json;

public class GooglePlayGamesScript : MonoBehaviour
{
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer의 싱글톤 인스턴스. 현재 플레이어의 데이터(ID와 코인)를 저장하고 로딩하는 데 사용
    
    private readonly UserInfo userInfo = new UserInfo(); // 유저 데이터를 저장하는 객체로, ID와 코인(COIN) 정보를 포함
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
        // 요청 url
        string requestUrl = $"{apiUrl}/{userInfo.id}";

        // APIManager를 통해 GET 요청 실행
        yield return StartCoroutine(APIManager.Instance.GetRequest(
            requestUrl,
            onSuccess: (jsonData) =>
            {
                // 응답받은 JSON 데이터 파싱 후 처리
                Debug.Log("로그인 성공");
                var userInfo = JsonConvert.DeserializeObject<LoginResponseDataType>(jsonData);
                MPlayer.Id = userInfo.userId;
                MPlayer.Coins = userInfo.coins;
                LoadLobby();
            },
            onError: (request) =>
            {
                // 에러가 발생한 경우 HTTP 응답 코드 확인
                if (request.responseCode == 404)
                {
                    Debug.Log("유저를 찾지 못했습니다. 회원가입을 진행합니다.");

                    // 회원가입 코루틴 실행 후 재로그인 시도
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

    // 유저 데이터를 JSON 형식으로 서버에 보내어 회원가입을 처리하는 코루틴
    private IEnumerator SignUp(string provider)
    {
        // 회원가입 데이터 생성
        SignUpData data = new SignUpData
        {
            userId = userInfo.id,
            provider = provider,
            deviceId = SystemInfo.deviceUniqueIdentifier
        };

        // JSON 문자열로 변환
        var requestData = JsonUtility.ToJson(data);

        // APIManager를 통해 POST 요청 실행
        yield return APIManager.Instance.PostRequest(
            apiUrl,
            requestData,
            onSuccess: (sucmsg) =>
            {
                Debug.Log("회원가입 성공");
                StartCoroutine(SignIn(provider));
            },
            onError: (request) =>
            {
                string errmsg = request.downloadHandler.text;
                Debug.Log($"회원가입 에러: {errmsg}");
                CloseLoadingPopup();
            }
        );
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
