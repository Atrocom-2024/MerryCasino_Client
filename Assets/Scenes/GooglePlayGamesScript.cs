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
    const string  url = "http://localhost:3000/api/players";
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
                savedata.id= Social.localUser.id;
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
        savedata.id = SystemInfo.deviceUniqueIdentifier;
        Debug.Log(savedata.id);
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
        using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{savedata.id}")) // �޸� ���� ������ ���� using ���
        {
            // yield return�� ����� �񵿱������� ��û�� ��ٸ��鼭 ���� �����尡 ������ �ʰ� �ٸ� �۾��� �� �� �ְ� ��
            yield return request.SendWebRequest(); // �ڷ�ƾ�� �Ͻ� ���� ������ �ϸ�, ��Ʈ��ũ ������ ���� �ٽ� ������ �簳

            Debug.Log("start");
            // �������� ���� �����͸� ã�� ���� ��� ȸ������ �ڷ�ƾ�� ���� ������ �����ϰ� �ٽ� GET�� ���� ���� �����͸� �ҷ���
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                // ���� �ڵ尡 404�� ��� ȸ������ �������� �Ѿ���� ó��
                if (request.responseCode == 404)
                {
                    Debug.Log("������ ã�� ���߽��ϴ�. ȸ�������� �����մϴ�.");

                    var jsondata = JsonUtility.ToJson(savedata);
                    yield return StartCoroutine(SignUp(jsondata));

                    Get();
                }
                else
                {
                    // �ٸ� ���� ó��
                    Debug.Log($"Error: {request.error})");
                }
                
            }
            // �������� ���� �����͸� ã�� ��� JSON ���� �Ľ� �� ��ü�� ID�� COIN�� �����ϰ� �κ�� �̵�
            else
            {
                // �������� ���� ���� JSON �����͸� �Ľ�
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

    // ���� �����͸� JSON �������� ������ ������ ȸ�������� ó���ϴ� �ڷ�ƾ
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

    // �κ� ������ �̵��ϴ� �޼���
    private void LoadLobby()
    {
        //GoLobby
        Debug.Log("go Lobby");
        SceneLoader.Instance.LoadScene(1); // ���� �ε����� 1�� ����
    }

    // ���� ��׶���� ��ȯ�� �� ȣ��Ǵ� �޼���
    private void OnApplicationPause(bool pause)
    {
        // ���� �Ͻ�����(pause) ���·� ��ȯ�Ǹ� SendRequestAndCloseApp() �ڷ�ƾ�� ȣ���Ͽ� ������ ������ ���� ��û
        if (pause)
        {
            StartCoroutine(SendRequestAndCloseApp());
        }
    }

    // ���� �Ͻ����� ���·� �� ��, ���� ������ ���� �����͸� ������ �����ϴ� �ڷ�ƾ
    IEnumerator SendRequestAndCloseApp()
    {
        // MPlayer.Coins ���� savedata.COIN�� �����ϰ�, JSON �������� ��ȯ�Ͽ� ������ ����
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