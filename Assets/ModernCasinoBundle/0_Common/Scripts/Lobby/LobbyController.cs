using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey {
    public class LobbyController : MonoBehaviour {
        public static LobbyController Instance { get; private set; }
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private RoomSocketManager RoomSocketManager { get { return RoomSocketManager.Instance; } }
        private GuiController MGUI { get { return GuiController.Instance; } }

        #region const vars
        //private const string SERVER_ADDRESS = "127.0.0.1";
        private const string SERVER_ADDRESS = "socket.atrocom.com";
        //private const string SERVER_ADDRESS = "taewoomac.iptime.org";
        private const int SERVER_PORT = 4000;
        #endregion

        #region temp vars
        public GameData gameData = new GameData();
        private Button[] buttons;
        #endregion temp vars

        #region regular

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            buttons = GetComponentsInChildren<Button>();

            // 기존 이벤트 제거
            RoomSocketManager.OnGameUserState -= InitGameUserState;
            RoomSocketManager.OnGameState -= InitGameState;

            // 이벤트 구독
            RoomSocketManager.OnGameUserState += InitGameUserState;
            RoomSocketManager.OnGameState += InitGameState;
        }

        private void OnDestroy()
        {
            // 기존 이벤트 제거
            RoomSocketManager.OnGameUserState -= InitGameUserState;
            RoomSocketManager.OnGameState -= InitGameState;
        }
        #endregion regular

        public void SceneLoad(int scene)
        {
            SceneLoader.Instance.LoadScene(scene);
        }

        public void InitGameUserState(GameUserState userState)
        {
            //Debug.Log($"[RoomController] User joined room: UserId = {MPlayer.Id}");
            gameData.CurrentPayout = userState.CurrentPayout;
            gameData.JackpotProb = userState.JackpotProb;
        }

        public void InitGameState(GameState gameState)
        {
            //Debug.Log($"[RoomController] Game state updated");
            gameData.TotalJackpotAmount = (int)gameState.TotalJackpotAmount;
        }

        public void OnJoinRoomButtonClick(int roomId)
        {
            StartCoroutine(JoinRoomAndLoadScene(roomId));
        }

        private IEnumerator JoinRoomAndLoadScene(int roomId)
        {
            SceneLoader.Instance.ShowLoadingPopup();

            if (RoomSocketManager == null)
            {
                Debug.LogError("[Lobby] RoomSocketManager is null!");
                SceneLoader.Instance.CloseLoadingPopup(); // 실패 시 로딩창 끄기
                MGUI.ShowMessageWithCloseButton("Connection Error", "\nAn unexpected error occurred.\nPlease try again.", () => { });
                yield break;
            }

            var connectSocketTask = RoomSocketManager.ConnectToServer(SERVER_ADDRESS, SERVER_PORT);
            yield return new WaitUntil(() => connectSocketTask.IsCompleted);

            if (!RoomSocketManager.IsConnected)
            {
                Debug.LogError("[Lobby] Failed to connect to server!");
                SceneLoader.Instance.CloseLoadingPopup(); // 실패 시 로딩창 끄기
                MGUI.ShowMessageWithCloseButton("Connection Error", "\nFailed to connect to server.\nPlease check your network status.", () => { });
                yield break;
            }

            Task joinRoomTask = RoomSocketManager.SendRoomJoinRequest(MPlayer.Id, roomId - 2);
            yield return new WaitUntil(() => joinRoomTask.IsCompleted);

            SceneLoad(roomId);
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
        public decimal JackpotProb { get; set; }
    }
}