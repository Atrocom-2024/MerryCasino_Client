using System.Threading.Tasks;
using Mkey;
using UnityEngine;
using UnityEngine.UI;

// Ư�� ���� ���޷��� ����ϰ�, ���õ� ������ �����ϴ� Ŭ����
public class RoomController : MonoBehaviour
{
    public static RoomController Instance;
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
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
        // ���� ����
        await RoomSocketManager.Instance.ConnectToServer("127.0.0.1", 4000);

        // �̺�Ʈ ����
        RoomSocketManager.Instance.OnRoomJoinResponse += HandleRoomJoinResponse;
        RoomSocketManager.Instance.OnGameStateUpdate += HandleGameStateUpdate;

        // �� ���� ��û
        await JoinRoom(MPlayer.Id, roomNumber);
    }

    private void OnDestroy()
    {
        // �� ���� �� WebSocket ���� ����
        RoomSocketManager.Instance.Disconnect();
        RoomSocketManager.Instance.OnRoomJoinResponse -= HandleRoomJoinResponse;
        RoomSocketManager.Instance.OnGameStateUpdate -= HandleGameStateUpdate;
    }

    private async Task JoinRoom(string userId, int roomId)
    {
        Debug.Log($"Joining room {roomId}");

        // ������ �� ���� ��û
        await RoomSocketManager.Instance.WaitForConnection();
        await RoomSocketManager.Instance.SendRoomJoinRequest(userId, roomId);
    }

    private void HandleRoomJoinResponse(GameUserState userState)
    {
        Debug.Log($"[RoomController] User joined room: UserId = {userState.GameUserId}");
        SetPayout(userState.CurrentPayout);
    }

    private void HandleGameStateUpdate(GameSession gameState)
    {
        Debug.Log($"[RoomControoler] Game state updated");
    }

    public void SetPayout(double payout)
    {
        resultPayout = payout;
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