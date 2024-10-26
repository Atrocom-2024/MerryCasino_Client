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
    const string  url = "http://155.248.199.174:3001/Players";
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
                savedata.ID= Social.localUser.id;
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
        savedata.ID = SystemInfo.deviceUniqueIdentifier;
        Debug.Log(savedata.ID);
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
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{savedata.ID}"))
        {
            yield return request.SendWebRequest();

            Debug.Log("start");
            // 서버에서 유저 데이터를 찾지 못한 경우 회원가입 코루틴을 통해 유저를 생성하고 다시 GET을 통해 유저 데이터를 불러옴
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {

                string errmsg = request.downloadHandler.text;

                Debug.Log(errmsg);
                if (errmsg.Equals("not exist ID"))
                {
                    var jsondata = JsonUtility.ToJson(savedata);
                    yield return StartCoroutine(SignUp(jsondata));

                    Get();
                }
            }

            // 서버에서 유저 데이터를 찾을 경우 JSON 응답 파싱 후 객체에 ID와 COIN을 저장하고 로비로 이동
            else
            {
                // 서버에서 응답 받은 JSON 데이터를 파싱
                string jsondata = request.downloadHandler.text;
                Debug.Log("else");
                saveData myObject = JsonUtility.FromJson<saveData>(jsondata);
                MPlayer.Id = myObject.ID;
                MPlayer.Coins = myObject.COIN;
                LoadLobby();
            }
        }
    }

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

    private void LoadLobby()
    {
        //GoLobby
        Debug.Log("go Lobby");
        SceneLoader.Instance.LoadScene(1);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            StartCoroutine(SendRequestAndCloseApp());
        }
    }


    IEnumerator SendRequestAndCloseApp()
    {
        var request = new UnityWebRequest($"{url}/Patch", "POST");
        savedata.COIN = MPlayer.Coins;
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
    public string ID;
    public long COIN;
    public saveData()
    {
        ID = "";
        COIN = 0;
    }
}