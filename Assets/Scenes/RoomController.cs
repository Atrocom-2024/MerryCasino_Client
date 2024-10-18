using Mkey;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
    private double basePayout;

    [SerializeField]
    Text PayoutText;


    double plusPayout;
    private void Awake()
    {
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
        using (UnityWebRequest request = UnityWebRequest.Get($"{payouturl}/{roomNumber}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("fail");
                string errmsg = request.downloadHandler.text;
            }
            else
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
                else
                {
                    string sucmsg = request.downloadHandler.text;
                    double randomProbe = double.Parse(sucmsg) <= 1 ? double.Parse(sucmsg) : 1;
                    double betProbe = (sessionTotalBet / 10000) <= 1 ? (sessionTotalBet / 10000) : 1;
                    resultPayout = (plusPayout / 2) * (randomProbe + betProbe);
                    PayoutText.text=resultPayout.ToString("F2")+"%";
                    double percentage = 1d-resultPayout/ plusPayout;
                    PayoutText.color = new Color(1.0f, (float)percentage, 0.0f);
                }
            }
            yield return new WaitForSeconds(1); // 1초 동안 대기
        }
    }
    private void returnEvent()
    {
        MPlayer.AddCoins((int)sessionTotalBet / 10);
        Debug.Log("sessionTotalBet: " + sessionTotalBet + "return Value: " + (int)sessionTotalBet / 10);
        sessionTotalBet = 1;
        resultPayout = 0;
        returnPopOn();
    }
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