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
        StartCoroutine(RoomAPIManager.Instance.GetTargetPayout(roomNumber, basePayout, plusPayout,
            onSuccess: plusPayout => Debug.Log($"Plus Payout: {plusPayout}"),
            onError: error => Debug.LogError(error)));

        if (Instance == null) Instance = this;
        sessionTotalBet = 1;
        resultPayout = 0;
    }

    private void Start()
    {
        StartCoroutine(RoomAPIManager.Instance.GetServer(
            roomNumber,
            returnEvent // 조건이 충족되면 호출될 메서드
        ));
        StartCoroutine(PayoutRoutine());
    }

    private IEnumerator PayoutRoutine()
    {
        while (true)
        {
            yield return RoomAPIManager.Instance.GetPayout(
                roomNumber,
                sucmsg =>
                {
                    // Success callback - call calculResultPayout logic
                    calculResultPayout(sucmsg);
                },
                errmsg =>
                {
                    // Error callback - log or handle error
                    Debug.LogError(errmsg);
                }
            );

            yield return new WaitForSeconds(1); // 1초 대기
        }
    }

    private void calculResultPayout(string sucmsg)
    {
        Debug.Log($"특정 room payout은 {sucmsg}입니다.");
        // Process result payout logic here
        double randomProbe = double.Parse(sucmsg) <= 1 ? double.Parse(sucmsg) : 1;
        double betProbe = (sessionTotalBet / 10000) <= 1 ? (sessionTotalBet / 10000) : 1;
        resultPayout = (plusPayout / 2) * (randomProbe + betProbe);
        Debug.Log($"result Payout: {resultPayout}");

        // Update the UI
        PayoutText.text = resultPayout.ToString("F2") + "%";
        double percentage = 1d - resultPayout / plusPayout;
        PayoutText.color = new Color(1.0f, (float)percentage, 0.0f);
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