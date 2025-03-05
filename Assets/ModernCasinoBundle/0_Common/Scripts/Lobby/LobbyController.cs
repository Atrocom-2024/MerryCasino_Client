using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey {
    public class LobbyController : MonoBehaviour {
        public static LobbyController Instance { get; private set; }

        #region temp vars
        private Button[] buttons;
        private string serverAddress = "socket.atrocom.com";
        private int serverPort = 4000;
        private PopUpsController loadingPopup;
        #endregion temp vars

        public GameData gameData = new GameData();
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private RoomSocketManager RoomSocketManager { get { return RoomSocketManager.Instance; } }

        #region regular

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            buttons = GetComponentsInChildren<Button>();

            // 이벤트 구독
            RoomSocketManager.OnGameUserStateResponse += InitGameUserState;
            RoomSocketManager.OnGameStateResponsee += InitGameState;
        }
        #endregion regular

        public void SceneLoad(int scene)
        {
            SceneLoader.Instance.LoadScene(scene);
        }

        public void InitGameUserState(GameUserState userState)
        {
            Debug.Log($"[RoomController] User joined room: UserId = {MPlayer.Id}");
            gameData.CurrentPayout = userState.CurrentPayout;
        }

        public void InitGameState(GameState gameState)
        {
            Debug.Log($"[RoomController] Game state updated");
            gameData.TotalJackpotAmount = (int)gameState.TotalJackpotAmount;
        }

        public void OnJoinRoomButtonClick(int roomId)
        {
            StartCoroutine(JoinRoomAndLoadScene(roomId - 2));
        }

        private IEnumerator JoinRoomAndLoadScene(int roomId)
        {
            ShowLoadingPopup();

            var connectSocketTask = RoomSocketManager.ConnectToServer(serverAddress, serverPort);
            yield return new WaitUntil(() => connectSocketTask.IsCompleted);

            if (!RoomSocketManager.Instance.IsConnected)
            {
                Debug.LogError("[Lobby] Failed to connect to server!");
                CloseLoadingPopup(); // 실패 시 로딩창 끄기
                yield break;
            }

            Task joinRoomTask = RoomSocketManager.SendRoomJoinRequest(MPlayer.Id, roomId);
            yield return new WaitUntil(() => joinRoomTask.IsCompleted);

            SceneLoader.Instance.LoadScene(roomId);
        }

        private void ShowLoadingPopup()
        {
            if (loadingPopup == null)
            {
                loadingPopup = SceneLoader.Instance.LoadGroupPrefab;
                GuiController guiController = FindObjectOfType<GuiController>();
                guiController.ShowPopUp(loadingPopup);
            }
        }

        private void CloseLoadingPopup()
        {
            if (loadingPopup != null)
            {
                loadingPopup.CloseWindow();
                loadingPopup = null;
            }
        }

        /// <summary>
        /// Set all buttons interactble = activity
        /// </summary>
        /// <param name="activity"></param>
        public void SetControlActivity(bool activity)
        {
            if (buttons == null) return;
            foreach (Button b in buttons)
            {
                if (b) b.interactable = activity;
            }
        }
    }

    /// <summary>
    /// 게임 데이터를 저장하는 클래스
    /// </summary>
    public class GameData
    {
        public int Coins { get; set; }
        public decimal CurrentPayout { get; set; }
        public int TotalJackpotAmount { get; set; }
    }
}