using Mkey;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// 특정 룸의 지급률을 계산하고, 관련된 정보를 관리하는 클래스
public class RoomController : MonoBehaviour
{
    const string probeurl = "http://155.248.199.174:80/Payout";
    const string url = "http://155.248.199.174:80/Payoutreturn";
    const string payouturl = "http://155.248.199.174:80/targetPayout";
    public static RoomController Instance;
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
    public double resultPayout;
    public double sessionTotalBet;
    public int roomNumber;

    [SerializeField]
    private double basePayout; // 기본 지급률

    [SerializeField]
    Text PayoutText;


    double plusPayout; // 추가 지급률

    private void Awake()
    {
        // 인스턴스를 설정하고 초기 값을 설정
        StartCoroutine(getTargetPayout());
        if (Instance == null) Instance = this;
        sessionTotalBet = 1;
        resultPayout = 0;
    }

    private void Start()
    {
        StartCoroutine(getServer());
        StartCoroutine(calculResultPayout());
        
    }
    IEnumerator getTargetPayout()
    {
        // 특정 룸의 목표 지급률(targetPayout)을 가져온다.
        using (UnityWebRequest request = UnityWebRequest.Get($"{payouturl}/{roomNumber}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("fail");
                string errmsg = request.downloadHandler.text;
            }
            else // 성공적으로 데이터를 받으면 plusPayout 값을 계산합니다 (targetPayout - basePayout)
            {
                string sucmsg = request.downloadHandler.text;
                double targetPayout=double.Parse(sucmsg);
                Debug.Log(targetPayout);
                plusPayout = targetPayout - basePayout;
            }

        }
    }
    IEnumerator getServer()
    {
        // 서버에 주기적으로 요청을 보내 특정 조건(errmsg가 true인 경우)이 발생하면 returnEvent()를 호출
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{roomNumber}"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errmsg = request.downloadHandler.text;
                }
                else
                {
                    string errmsg = request.downloadHandler.text;
                    if (errmsg == "true")
                    {
                        returnEvent();
                        Debug.Log(errmsg);
                    }
                    else
                    {
                        Debug.Log(errmsg);
                    }
                }
            }
            yield return new WaitForSeconds(5); // 5초 동안 대기
        }
    }
    IEnumerator calculResultPayout()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get($"{probeurl}/{roomNumber}"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errmsg = request.downloadHandler.text;
                }
                else // randomProbe와 betProbe 값을 사용하여 resultPayout을 계산하고, 이를 PayoutText UI 요소에 표시
                {
                    string sucmsg = request.downloadHandler.text;
                    double randomProbe = double.Parse(sucmsg) <= 1 ? double.Parse(sucmsg) : 1;
                    double betProbe = (sessionTotalBet / 10000) <= 1 ? (sessionTotalBet / 10000) : 1;
                    resultPayout = (plusPayout / 2) * (randomProbe + betProbe);
                    PayoutText.text=resultPayout.ToString("F2") + "%";
                    double percentage = 1d - resultPayout / plusPayout;
                    PayoutText.color = new Color(1.0f, (float)percentage, 0.0f);
                }
            }
            yield return new WaitForSeconds(1); // 1초 동안 대기
        }
    }
    private void returnEvent()
    {
        // 플레이어의 코인을 추가하고, sessionTotalBet과 resultPayout 값을 초기화
        MPlayer.AddCoins((int)sessionTotalBet / 10);
        Debug.Log("sessionTotalBet: " + sessionTotalBet + "return Value: " + (int)sessionTotalBet / 10);
        sessionTotalBet = 1;
        resultPayout = 0;
        returnPopOn();
    }

    // returnPopOn()과 returnPopOff() 메서드는 지급률 관련 UI 팝업을 활성화하거나 비활성화
    public void returnPopOn()
    {
        if (Payoutinfo._returnPopup.activeSelf == false)
            Payoutinfo._returnPopup.SetActive(true);
        Invoke("returnPopOff", 1);
    }

    public void returnPopOff()
    {
        Payoutinfo._returnPopup.SetActive(false);
    }
    //public void OnPayOutInfo()
    //{
    //    Payoutinfo._infoPopup.SetActive(true);
    //}


}