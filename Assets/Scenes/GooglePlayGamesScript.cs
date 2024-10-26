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
    private saveData savedata = new saveData(); // ���� �����͸� �����ϴ� ��ü��, ID�� ����(COIN) ������ ����
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } } // SlotPlayer�� �̱��� �ν��Ͻ�. ���� �÷��̾��� ������(ID�� ����)�� �����ϰ� �ε��ϴ� �� ���
    const string  url = "http://155.248.199.174:3001/Players";
    void Awake()
    {
        Debug.Log("awake login");
        PlayGamesPlatform.Activate();
    }

    //���� �α���
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

    // ����̽� ���� ID�� �̿��� �Խ�Ʈ �α���
    public void LoginGuest()
    {
        savedata.ID = SystemInfo.deviceUniqueIdentifier;
        Debug.Log(savedata.ID);
        Get();
    }

    // �ڷ�ƾ�� ���� �񵿱� Login ��û
    public void Get()
    {
        // ������ �񵿱� ����� ���� ���� �����͸� �ҷ����� ���� �ڷ�ƾ�� ����
        StartCoroutine(Login());
    }

    public IEnumerator Login()
    {
        // ������ �α��� ��û�� ������, ���� ������ �������� ���� �ڷ�ƾ
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{savedata.ID}"))
        {
            yield return request.SendWebRequest();

            Debug.Log("start");
            // �������� ���� �����͸� ã�� ���� ��� ȸ������ �ڷ�ƾ�� ���� ������ �����ϰ� �ٽ� GET�� ���� ���� �����͸� �ҷ���
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

            // �������� ���� �����͸� ã�� ��� JSON ���� �Ľ� �� ��ü�� ID�� COIN�� �����ϰ� �κ�� �̵�
            else
            {
                // �������� ���� ���� JSON �����͸� �Ľ�
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