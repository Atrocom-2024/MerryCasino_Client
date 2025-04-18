using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Mkey;

// Ư�� ���� ���޷��� ����ϰ�, ���õ� ������ �����ϴ� Ŭ����
public class RoomController : MonoBehaviour
{
    public static RoomController Instance;
    private RoomSocketManager RoomSocketManager { get { return RoomSocketManager.Instance; } }
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
    private LobbyController LobbyController { get { return LobbyController.Instance; } }
    private GuiController MGUI { get { return GuiController.Instance; } }

    private SlotControls controls;

    public double resultPayout;
    public int roomNumber;

    [SerializeField]
    Text PayoutText;

    private void Awake()
    {
        // �ν��Ͻ��� �����ϰ� �ʱ� ���� ����
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        controls = FindObjectOfType<SlotControls>();

        if (controls == null)
        {
            Debug.LogError("[RoomController] SlotControls not found in the scene.");
        }

        SubscribeToEvents(); // �̺�Ʈ ����

        // �ε� �� �޾ƿ� ������ ����
        resultPayout = (double)LobbyController.gameData.CurrentPayout;
        SetPayout(resultPayout);
        controls.SetMegaJackpotProbStart(LobbyController.gameData.JackpotProb);
    }

    private void Start()
    {
        if (controls == null)
        {
            controls = FindObjectOfType<SlotControls>();
        }

        if (controls == null)
        {
            Debug.LogError("[RoomController] SlotControls still not found in Start.");
        }
    }

    private void OnDestroy()
    {
        // �� ���� �� TCP ���� ����
        if (RoomSocketManager != null)
        {
            UnsubscribeFromEvents(); // �̺�Ʈ ���� ����
            RoomSocketManager.Disconnect(); // ���� ���� ����
        }
    }

    /// <summary>
    /// �̺�Ʈ ���� �޼���
    /// </summary>
    private void SubscribeToEvents()
    {
        if (RoomSocketManager == null)
        {
            Debug.LogError("[RoomController] RoomSocketManager.Instance is null!");
            return;
        }

        //UnsubscribeFromEvents(); // �̺�Ʈ ���� ���� �� �籸��

        RoomSocketManager.OnBetResponse += HandleBetUpdate;
        RoomSocketManager.OnAddCoinsResponse += HandleAddCoins;
        RoomSocketManager.OnJackpotWinResponse += HandleJackpotWinUpdate;
        RoomSocketManager.OnGameState += HandleGameStateUpdate;
        RoomSocketManager.OnGameUserState += HandleGameUserStateUpdate;
        RoomSocketManager.OnGameSessionEnd += HandleGameSessionEnd;
    }

    /// <summary>
    /// �̺�Ʈ ���� ���� �޼���
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (RoomSocketManager == null)
        {
            Debug.LogError("[RoomController] RoomSocketManager.Instance is null!");
            return;
        }

        RoomSocketManager.OnGameUserState -= LobbyController.InitGameUserState;
        RoomSocketManager.OnGameState -= LobbyController.InitGameState;

        RoomSocketManager.OnBetResponse -= HandleBetUpdate;
        RoomSocketManager.OnAddCoinsResponse -= HandleAddCoins;
        RoomSocketManager.OnJackpotWinResponse -= HandleJackpotWinUpdate;
        RoomSocketManager.OnGameState -= HandleGameStateUpdate;
        RoomSocketManager.OnGameUserState -= HandleGameUserStateUpdate;
        RoomSocketManager.OnGameSessionEnd -= HandleGameSessionEnd;
    }

    public IEnumerator HandleBetting(string userId, int betAmount)
    {
        // ���� ��û �񵿱� �۾� ����
        var sendBetTask = RoomSocketManager.SendBetReqeust(userId, betAmount);

        // Task�� �Ϸ�� ������ ���
        while (!sendBetTask.IsCompleted)
        {
            yield return null; // ���� �����ӱ��� ���
        }
    }

    public IEnumerator HandleJackpotWin(JackPotType jackpotType, int jackpotCoins)
    {
        // ���� ���� ��û �񵿱� �۾� ����
        var sendJackpotWinTask = RoomSocketManager.SendJackpotWinRequest(jackpotType.ToString(), jackpotCoins);

        // Task�� �Ϸ�� ������ ���
        while (!sendJackpotWinTask.IsCompleted)
        {
            yield return null; // ���� �����ӱ��� ���
        }
    }

    #region event handler
    public void HandleBetUpdate(BetResponse response)
    {
        MPlayer.SetCoinsCount(response.UpdatedCoins);
    }

    public void HandleAddCoins(AddCoinsResponse response)
    {
        MPlayer.SetCoinsCount(response.AddedCoinsAmount);
    }

    public void HandleJackpotWinUpdate(JackpotWinResponse response)
    {
        MPlayer.SetCoinsCount(response.AddedCoinsAmount);
        MGUI.ShowMessage(null, $"\nJACKPOT! Unbelievable luck!\n\nYou won {controls.TotalBet * 100} coins!", 4f, null);
    }

    public void HandleGameUserStateUpdate(GameUserState userState)
    {
        SetPayout((double)userState.CurrentPayout);
        controls.SetJackPotProb(userState.JackpotProb, JackPotType.Mega);
    }

    public void HandleGameStateUpdate(GameState gameState)
    {
        //controls.SetJackPotProb((int)gameState.TotalJackpotAmount, JackPotType.Mega);
        //controls.SetJackPotCount((int)gameState.TotalJackpotAmount, JackPotType.Mega);
    }

    public void HandleGameSessionEnd(GameSessionEnd response)
    {
        MPlayer.SetCoinsCount(response.RewardedCoinsAmount);
        MGUI.ShowMessage(null, $"Game Session has ended.\n\nThe next session will begin soon.\n\nRewarded coins: {response.RewardCoins}", 3f, null);
    }
    #endregion event handler

    public void SetPayout(double payout)
    {
        resultPayout = payout * 100;

        // Update the UI
        //Color lowColor = new Color(0.0f, 1.0f, 0.0f); // Green
        Color lowColor = new Color(0.0f, 0.5f, 0.0f); // ��ο� �ʷϻ�
        Color midColor = new Color(1.0f, 0.5f, 0.0f); // Orange
        Color highColor = new Color(1.0f, 0.0f, 0.0f); // Red
        float normalizedPayout = (float)(resultPayout / 100.0); // Normalize resultPayout (0 to 100 -> 0.0 to 1.0)

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
}