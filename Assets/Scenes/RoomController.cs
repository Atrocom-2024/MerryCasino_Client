using System.Collections;
using System.Threading.Tasks;
using Mkey;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

// Ư�� ���� ���޷��� ����ϰ�, ���õ� ������ �����ϴ� Ŭ����
public class RoomController : MonoBehaviour
{
    public static RoomController Instance;
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }

    [SerializeField]
    private SlotControls controls;

    public double resultPayout;
    //public double sessionTotalBet;
    public int roomNumber;

    //[SerializeField]
    //private double basePayout; // �⺻ ���޷�

    [SerializeField]
    Text PayoutText;


    double plusPayout; // �߰� ���޷�

    private void Awake()
    {
        // �ν��Ͻ��� �����ϰ� �ʱ� ���� ����
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        //sessionTotalBet = 1;
        //resultPayout = 0;
    }

    private async void Start()
    {
        controls = FindObjectOfType<SlotControls>();
        if (controls == null)
        {
            Debug.LogError("[RoomController] SlotControls not found in the scene.");
        }

        // ���� ����
        await RoomSocketManager.Instance.ConnectToServer("44.202.1.36", 4000);

        // �̺�Ʈ ����
        RoomSocketManager.Instance.OnGameUserStateResponse += HandleGameUserStateUpdate;
        RoomSocketManager.Instance.OnGameStateResponsee += HandleGameStateUpdate;
        RoomSocketManager.Instance.OnBetResponse += HandleBetUpdate;
        RoomSocketManager.Instance.onAddCoinsResponse += HandleAddCoinsResponse;

        // �� ���� ��û
        await HandleJoinRoom(MPlayer.Id, roomNumber);
    }

    private void OnDestroy()
    {
        // �� ���� �� WebSocket ���� ����
        RoomSocketManager.Instance.Disconnect();
        RoomSocketManager.Instance.OnGameUserStateResponse -= HandleGameUserStateUpdate;
        RoomSocketManager.Instance.OnGameStateResponsee -= HandleGameStateUpdate;
        RoomSocketManager.Instance.OnBetResponse -= HandleBetUpdate;
    }

    private async Task HandleJoinRoom(string userId, int roomId)
    {
        Debug.Log($"Joining room {roomId}");

        // ������ �� ���� ��û
        await RoomSocketManager.Instance.WaitForConnection();
        await RoomSocketManager.Instance.SendRoomJoinRequest(userId, roomId);
    }

    //public async Task HandleBet(string userId, int betAmount)
    //{
    //    Debug.Log($"Betting: {betAmount}");

    //    // ���� ��û �񵿱� �۾� ����
    //    await  RoomSocketManager.Instance.SendBetReqeust(userId, betAmount);
    //}
    public IEnumerator HandleBet(string userId, int betAmount)
    {
        Debug.Log($"Betting: {betAmount}");

        // ���� ��û �񵿱� �۾� ����
        var sendBetTask = RoomSocketManager.Instance.SendBetReqeust(userId, betAmount);

        // Task�� �Ϸ�� ������ ���
        while (!sendBetTask.IsCompleted)
        {
            yield return null; // ���� �����ӱ��� ���
        }
    }

    private void HandleBetUpdate(BetResponse response)
    {
        MPlayer.SetCoinsCount(response.UpdatedCoins);
    }

    private void HandleGameUserStateUpdate(GameUserState userState)
    {
        Debug.Log($"[RoomController] User joined room: UserId = {MPlayer.Id}");
        SetPayout((double)userState.CurrentPayout);
    }

    private void HandleGameStateUpdate(GameState gameState)
    {
        Debug.Log($"[RoomController] Game state updated");
        Debug.Log($"Jackpot amount is {gameState.TotalJackpotAmount}");
        controls.SetJackPotCount((int)gameState.TotalJackpotAmount, JackPotType.Mega);
    }

    private void HandleAddCoinsResponse(AddCoinsResponse response)
    {
        Debug.Log($"[RoomController] User coins updated: coins = {response.AddedCoinsAmount}");
        MPlayer.SetCoinsCount(response.AddedCoinsAmount);
    }

    public void SetPayout(double payout)
    {
        resultPayout = payout * 100;
        Debug.Log($"result Payout: {resultPayout}");

        // Update the UI
        //Color lowColor = new Color(0.0f, 1.0f, 0.0f); // Green
        Color lowColor = new Color(0.0f, 0.5f, 0.0f); // ��ο� �ʷϻ�
        Color midColor = new Color(1.0f, 0.5f, 0.0f); // Orange
        Color highColor = new Color(1.0f, 0.0f, 0.0f); // Red
        float normalizedPayout = (float)(resultPayout / 100.0); // Normalize resultPayout (0 to 100 -> 0.0 to 1.0)
        Debug.Log($"Normalized Payout: {normalizedPayout}");
        PayoutText.text = resultPayout.ToString("F2") + "%";

        if (normalizedPayout < 0.25f)
        {
            // Green to Orange (0.0 to 0.25)
            PayoutText.color = Color.Lerp(lowColor, midColor, normalizedPayout / 0.25f);
        }
        else if (normalizedPayout < 0.75f)
        {
            // Orange to Red (0.25 to 0.75)
            PayoutText.color = Color.Lerp(midColor, highColor, (normalizedPayout - 0.25f) / 0.5f);
        }
        else
        {
            // Red for high values (0.75 to 1.0)
            PayoutText.color = highColor;
        }
    }

    ///
    /// �Ʒ� �ڵ���� payout�� �ʱ�ȭ�� �� ����Ǵ� ����
    ///

    /// <summary>
    /// �÷��̾��� ������ ������Ű��, ���õ� ����(sessionTotalBet)�� �ʱ�ȭ
    /// </summary>
    private void returnEvent()
    {
        // �÷��̾��� ������ �߰��ϰ�, sessionTotalBet�� resultPayout ���� �ʱ�ȭ
        //MPlayer.AddCoins((int)sessionTotalBet / 10);
        //Debug.Log("sessionTotalBet: " + sessionTotalBet + "return Value: " + (int)sessionTotalBet / 10);
        //sessionTotalBet = 1;
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