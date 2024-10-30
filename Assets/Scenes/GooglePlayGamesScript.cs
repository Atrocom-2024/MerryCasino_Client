using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Mkey;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class GooglePlayGamesScript : MonoBehaviour
{
    private saveData savedata = new saveData(); // 유저 데이터를 저장하는 객체로, ID와 코인(COIN) 정보를 포함
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer의 싱글톤 인스턴스. 현재 플레이어의 데이터(ID와 코인)를 저장하고 로딩하는 데 사용
    const string  url = "http://localhost:3000/api/players";
    void Awake()
    {
        Debug.Log("awake login");
        PlayGamesPlatform.Activate();
    }

    //구글 로그인
    public void LoginGooglePlayGames()
    {
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                savedata.id= Social.localUser.id;
                Get();
            }
            else
            {
                Debug.Log("Failed to retrieve Google play games authorization code");
            }
        });
    }

    // 디바이스 고유 ID를 이용한 게스트 로그인
    public void LoginGuest()
    {
        savedata.id = SystemInfo.deviceUniqueIdentifier;
        Debug.Log(savedata.id);
        Get();
    }

    // 코루틴을 통한 비동기 Login 요청
    public void Get()
    {
        // 서버와 비동기 통신을 통해 유저 데이터를 불러오기 위해 코루틴을 시작
        StartCoroutine(Login());
    }

    public IEnumerator Login()
    {
        // 서버에 로그인 요청을 보내고, 유저 정보를 가져오기 위한 코루틴
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{savedata.id}")) // 메모리 누수 방지를 위해 using 사용
        {
            // yield return을 사용해 비동기적으로 요청을 기다리면서 메인 스레드가 멈추지 않고 다른 작업을 할 수 있게 함
            yield return request.SendWebRequest(); // 코루틴의 일시 정지 역할을 하며, 네트워크 응답이 오면 다시 실행을 재개

            Debug.Log("start");
            // 서버에서 유저 데이터를 찾지 못한 경우 회원가입 코루틴을 통해 유저를 생성하고 다시 GET을 통해 유저 데이터를 불러옴
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 상태 코드가 404인 경우 회원가입 로직으로 넘어가도록 처리
                if (request.responseCode == 404)
                {
                    Debug.Log("유저를 찾지 못했습니다. 회원가입을 진행합니다.");

                    var jsondata = JsonUtility.ToJson(savedata);
                    yield return StartCoroutine(SignUp(jsondata));

                    Get();
                }
                else
                {
                    // 다른 에러 처리
                    Debug.Log($"Error: {request.error})");
                }
                
            }
            // 서버에서 유저 데이터를 찾을 경우 JSON 응답 파싱 후 객체에 ID와 COIN을 저장하고 로비로 이동
            else
            {
                // 서버에서 응답 받은 JSON 데이터를 파싱
                string jsondata = request.downloadHandler.text;
                Debug.Log("else");
                saveData myObject = JsonUtility.FromJson<saveData>(jsondata);
                Debug.Log(jsondata);
                MPlayer.Id = myObject.id;
                MPlayer.Coins = myObject.coins;
                LoadLobby();
            }
        }
    }

    // 유저 데이터를 JSON 형식으로 서버에 보내어 회원가입을 처리하는 코루틴
    IEnumerator SignUp(string jsondata)
    {
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsondata);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errmsg = request.downloadHandler.text;
            Debug.Log(errmsg);
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
        //GoLobby
        Debug.Log("go Lobby");
        SceneLoader.Instance.LoadScene(1); // 씬의 인덱스를 1로 설정
    }

    // 앱이 백그라운드로 전환될 때 호출되는 메서드
    private void OnApplicationPause(bool pause)
    {
        // 앱이 일시정지(pause) 상태로 전환되면 SendRequestAndCloseApp() 코루틴을 호출하여 서버에 데이터 저장 요청
        if (pause)
        {
            StartCoroutine(SendRequestAndCloseApp());
        }
    }

    // 앱이 일시정지 상태로 들어갈 때, 현재 유저의 코인 데이터를 서버에 저장하는 코루틴
    IEnumerator SendRequestAndCloseApp()
    {
        // MPlayer.Coins 값을 savedata.COIN에 저장하고, JSON 형식으로 변환하여 서버에 전송
        var request = new UnityWebRequest($"{url}/Patch", "POST");
        savedata.coins = MPlayer.Coins;
        var jsondata = JsonUtility.ToJson(savedata);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsondata);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errmsg = request.downloadHandler.text;
        }
        else
        {
            string sucmsg = request.downloadHandler.text;
        }
    }

}

public class saveData
{
    public string id;
    public long coins;
    public saveData()
    {
        id = "";
        coins = 0;
    }
}