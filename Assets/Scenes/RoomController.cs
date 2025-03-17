using System.Collections;
using Mkey;
using UnityEngine;
using UnityEngine.UI;

// 특정 룸의 지급률을 계산하고, 관련된 정보를 관리하는 클래스
public class RoomController : MonoBehaviour
{
    public static RoomController Instance;
    private RoomSocketManager RoomSocketManager { get { return RoomSocketManager.Instance; } }
    private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
    private LobbyController LobbyController { get { return LobbyController.Instance; } }
    private GuiController MGUI { get { return GuiController.Instance; } }

    private SlotControls controls;

    public double resultPayout;
    //public double sessionTotalBet;
    public int roomNumber;

    //[SerializeField]
    //private double basePayout; // 기본 지급률

    [SerializeField]
    Text PayoutText;


    double plusPayout; // 추가 지급률

    private void Awake()
    {
        // 인스턴스를 설정하고 초기 값을 설정
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

        //sessionTotalBet = 1;
        //resultPayout = 0;

        SubscribeToEvents(); // 이벤트 구독

        // 로딩 때 받아온 데이터 설정
        resultPayout = (double)LobbyController.gameData.CurrentPayout;
        SetPayout(resultPayout);
        controls.SetInitJackpotCount(LobbyController.gameData.TotalJackpotAmount);
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
        // 룸 퇴장 시 WebSocket 연결 해제
        if (RoomSocketManager != null)
        {
            UnsubscribeFromEvents(); // 이벤트 구독 해제
            RoomSocketManager.Disconnect(); // 소켓 연결 해제
        }

        StopAllCoroutines();
    }

    /// <summary>
    /// 이벤트 구독 메서드
    /// </summary>
    private void SubscribeToEvents()
    {
        if (RoomSocketManager == null)
        {
            Debug.LogError("[RoomController] RoomSocketManager.Instance is null!");
            return;
        }

        UnsubscribeFromEvents(); // 이벤트 구독 해제 후 재구독

        RoomSocketManager.OnBetResponse += HandleBetUpdate;
        RoomSocketManager.OnAddCoinsResponse += HandleAddCoins;
        RoomSocketManager.OnJackpotWinResponse += HandleJackpotWinUpdate;
        RoomSocketManager.OnGameState += HandleGameStateUpdate;
        RoomSocketManager.OnGameUserState += HandleGameUserStateUpdate;
        RoomSocketManager.OnGameSessionEnd += HandleGameSessionEnd;
    }

    /// <summary>
    /// 이벤트 구독 해제 메서드
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
        // 배팅 요청 비동기 작업 시작
        var sendBetTask = RoomSocketManager.SendBetReqeust(userId, betAmount);

        // Task가 완료될 때까지 대기
        while (!sendBetTask.IsCompleted)
        {
            yield return null; // 다음 프레임까지 대기
        }
    }

    public IEnumerator HandleJackpotWin(JackPotType jackpotType, int jackpotCoins)
    {
        // 잭팟 지급 요청 비동기 작업 시작
        var sendJackpotWinTask = RoomSocketManager.SendJackpotWinRequest(jackpotType.ToString(), jackpotCoins);

        // Task가 완료될 때까지 대기
        while (!sendJackpotWinTask.IsCompleted)
        {
            yield return null; // 다음 프레임까지 대기
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
    }

    public void HandleGameUserStateUpdate(GameUserState userState)
    {
        SetPayout((double)userState.CurrentPayout);
    }

    public void HandleGameStateUpdate(GameState gameState)
    {
        Debug.Log($"잭팟 금액!!!!!!!!! {gameState.TotalJackpotAmount}");
        controls.SetJackPotCount((int)gameState.TotalJackpotAmount, JackPotType.Mega);
    }

    public void HandleGameSessionEnd(GameSessionEnd response)
    {
        MPlayer.SetCoinsCount(response.RewardedCoinsAmount);
        MGUI.ShowMessage(null, $"Game Session has ended.\n\nThe next session will begin soon.\n\nRewarded coins: {response.RewardCoins}", 5f, null);
    }
    #endregion event handler

    public void SetPayout(double payout)
    {
        resultPayout = payout * 100;
        Debug.Log($"result Payout: {resultPayout}");

        // Update the UI
        //Color lowColor = new Color(0.0f, 1.0f, 0.0f); // Green
        Color lowColor = new Color(0.0f, 0.5f, 0.0f); // 어두운 초록색
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
    /// 아래 코드들은 payout이 초기화될 때 실행되는 동작
    ///

    /// <summary>
    /// 플레이어의 코인을 증가시키고, 관련된 상태(sessionTotalBet)를 초기화
    /// </summary>
    private void returnEvent()
    {
        // 플레이어의 코인을 추가하고, sessionTotalBet과 resultPayout 값을 초기화
        //MPlayer.AddCoins((int)sessionTotalBet / 10);
        //Debug.Log("sessionTotalBet: " + sessionTotalBet + "return Value: " + (int)sessionTotalBet / 10);
        //sessionTotalBet = 1;
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