using Mkey;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Ư�� ���� ���޷��� ����ϰ�, ���õ� ������ �����ϴ� Ŭ����
public class RoomController : MonoBehaviour
{
    public static RoomController Instance;
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
    public double resultPayout;
    public double sessionTotalBet;
    public int roomNumber;

    [SerializeField]
    private double basePayout; // �⺻ ���޷�

    [SerializeField]
    Text PayoutText;


    double plusPayout; // �߰� ���޷�

    private void Awake()
    {
        // �ν��Ͻ��� �����ϰ� �ʱ� ���� ����
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
            returnEvent // ������ �����Ǹ� ȣ��� �޼���
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

            yield return new WaitForSeconds(1); // 1�� ���
        }
    }

    private void calculResultPayout(string sucmsg)
    {
        Debug.Log($"Ư�� room payout�� {sucmsg}�Դϴ�.");
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
        // �÷��̾��� ������ �߰��ϰ�, sessionTotalBet�� resultPayout ���� �ʱ�ȭ
        MPlayer.AddCoins((int)sessionTotalBet / 10);
        Debug.Log("sessionTotalBet: " + sessionTotalBet + "return Value: " + (int)sessionTotalBet / 10);
        sessionTotalBet = 1;
        resultPayout = 0;
        returnPopOn();
    }

    // returnPopOn()�� returnPopOff() �޼���� ���޷� ���� UI �˾��� Ȱ��ȭ�ϰų� ��Ȱ��ȭ
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