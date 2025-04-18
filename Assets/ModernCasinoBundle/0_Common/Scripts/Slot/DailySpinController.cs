using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;


#if UNITY_EDITOR
using UnityEditor;
#endif

/*
    18.05.2021
    add   internal void SetHaveSpin()
 */
namespace Mkey
{
	public class DailySpinController : MonoBehaviour
	{
        private string dailySpinApiUrl;

        [SerializeField]
        private PopUpsController screenPrefab;
        [SerializeField]
        private TextMesh timerText;
        [HideInInspector]
        public UnityEvent TimePassEvent;
        [SerializeField]
        private MkeyFW.FortuneWheelInstantiator fwInstantiator;

        #region temp vars
        // 기본값은 필요시 사용
        private int defaultHours = 24;
        private int defaultMinutes = 0;
        private GlobalTimer gTimer;
        private PopUpsController screen;
        private string timerName = "dailySpinTimer";
        private bool debug = false;

        // 인스턴스 참조 변수
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private GuiController MGui { get { return GuiController.Instance; } }
        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        #endregion temp vars

        #region properties
        public float RestDays { get; private set; }
        public float RestHours { get; private set; }
        public float RestMinutes { get; private set; }
        public float RestSeconds { get; private set; }
        public bool IsWork { get; private set; }
        public static DailySpinController Instance { get; private set; }
        public static bool HaveDailySpin { get; private set; }
        #endregion properties

        #region regular
        private void Awake()
        {
            EnvReader.Load(".env");
            dailySpinApiUrl = $"{Environment.GetEnvironmentVariable("API_DOMAIN")}/api/daily-spins";

            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (timerText) timerText.text = "";
            IsWork = false;

            // set fortune wheel event handlers
            // 스핀 결과 처리: 스핀 완료 후 코인 추가 및 타이머 재시작
            fwInstantiator.SpinResultEvent += (coins, isBigWin) =>
            {
                HaveDailySpin = false;
                StartCoroutine(RequestProcessDailySpinResultAsync(MPlayer.Id, coins));

                if (fwInstantiator.MiniGame)
                    fwInstantiator.MiniGame.SetBlocked(!HaveDailySpin, true);
            }; 

            fwInstantiator.CreateEvent += (MkeyFW.WheelController wc)=>
            {
                if (screenPrefab)
                    screen = MGui.ShowPopUp(screenPrefab);
                wc.SetBlocked(!HaveDailySpin, false);
            };

            fwInstantiator.CloseEvent += () => 
            {
                if (screen)
                    screen.CloseWindow();
                if (timerText)
                    timerText.text = "";
            };

            // 서버에서 데일리 스핀 상태 요청
            StartCoroutine(RequestDailySpinStatusAsync(MPlayer.Id));
        }

        private void Update()
        {
            if (IsWork & gTimer != null)
                gTimer.Update();
        }
        #endregion regular

        #region timerhandlers
        private void TickRestDaysHourMinSecHandler(int d, int h, int m, float s)
        {
            RestDays = d;
            RestHours = h;
            RestMinutes = m;
            RestSeconds = s;
            if(timerText && fwInstantiator.MiniGame)
                timerText.text = String.Format("{0:00}:{1:00}:{2:00}", h, m, s);
        }

        private void TimePassedHandler(double initTime, double realyTime)
        {
            IsWork = false;

            if (timerText)
                timerText.text = "";

            HaveDailySpin = true;

            Debug.Log("time passed daily spin");

            if (fwInstantiator.MiniGame)
            {
                Debug.Log("time passed daily spin - > start mini game");
                fwInstantiator.MiniGame.SetBlocked(!HaveDailySpin, false);
            }

            if (debug) Debug.Log("daily spin timer time passed, have daily spin");
            TimePassEvent?.Invoke();
        }
        #endregion timerhandlers

        #region timers
        // 남은 시간(초 또는 TimeSpan)을 파라미터로 받아서 타이머 초기화
        private void StartTimer(TimeSpan remainingTime)
        {
            if (debug)
                Debug.Log($"start daily spin timer with remaining time: {remainingTime}");
            gTimer = new GlobalTimer(timerName, remainingTime.Days, remainingTime.Hours, remainingTime.Minutes, remainingTime.Seconds);
            gTimer.TickRestDaysHourMinSecEvent += TickRestDaysHourMinSecHandler;
            gTimer.TimePassedEvent += TimePassedHandler;
            IsWork = true;
        }
        #endregion timers

        private IEnumerator RequestDailySpinStatusAsync(string userId)
        {
            string requestUrl = $"{dailySpinApiUrl}/{userId}"; // 서버 URL을 설정하세요.
            yield return StartCoroutine(APIManager.Instance.GetRequest(
                requestUrl,
                onSuccess: (jsonData) =>
                {
                    try
                    {
                        var responseData = JsonConvert.DeserializeObject<DailySpinResponse>(jsonData);
                        if (responseData.IsAvailable)
                        {
                            // 이미 사용 가능하므로 타이머 없이 즉시 스핀 실행 UI 활성화
                            HaveDailySpin = true;
                            if (timerText)
                                timerText.text = "";
                            if (fwInstantiator.MiniGame)
                                fwInstantiator.MiniGame.SetBlocked(false, false);
                        }
                        else
                        {
                            // 남은 초를 TimeSpan으로 변환해서 타이머 시작
                            TimeSpan ts = TimeSpan.FromSeconds(responseData.RemainingSeconds);
                            StartTimer(ts);
                            HaveDailySpin = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error parsing daaily spin status: " + ex.Message);

                        // 파싱 실패 시 기본 타이머 로직을 진행
                        StartTimer(new TimeSpan(defaultHours, defaultMinutes, 0));
                    }
                },
                onError: (error) =>
                {
                    Debug.LogError("Error fetching daily spin status: " + error);

                    // 에러 시 기본 타이머 로직(예를 들어 24시간)으로 설정하거나 재시도 로직 구현 가능
                    StartTimer(new TimeSpan(defaultHours, defaultMinutes, 0));
                }
            ));
        }

        private IEnumerator RequestProcessDailySpinResultAsync(string userId, int rewardCoins)
        {
            var boryData = new
            {
                UserId = userId,
                SpinRewardCoins = rewardCoins
            };
            var bodyJsonData = JsonConvert.SerializeObject(boryData);

            yield return StartCoroutine(APIManager.Instance.PostRequest(
                dailySpinApiUrl,
                bodyJsonData,
                onSuccess: (jsonData) =>
                {
                    try
                    {
                        var responseData = JsonConvert.DeserializeObject<ProcessDailySpinResultResponse>(jsonData);
                        MPlayer.SetCoinsCount(responseData.ProcessedCoins);
                        MGui.ShowMessageWithCloseButton("Reward Received!", "\nGreat job!\nYour daily spin reward\nhas been successfully added.", () => { });

                        // Option 1: Re-request the server status to get the updated timer
                        //StartCoroutine(RequestDailySpinStatusAsync(MPlayer.Id));

                        // Option 2: Start a new timer locally if the cooldown is fixed (24 hours)
                         StartTimer(TimeSpan.FromHours(24));
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error parsing daily spin result: " + ex.Message);
                        MGui.ShowMessageWithYesNoCloseButton("Error", "Failed to process daily spin result. Please try again.", () => { }, null, null);
                    }
                },
                onError: (error) =>
                {
                    Debug.LogError("Error processing daily spin result: " + error);
                    MGui.ShowMessageWithYesNoCloseButton("Error", "Failed to process daily spin result. Please try again.", () => { }, null, null);
                }
            ));
        }


        public void OpenSpinGame()
        {
            fwInstantiator.Create(false);
            if (fwInstantiator.MiniGame)
            {
                fwInstantiator.MiniGame.BackGroundButton.clickEvent.AddListener(()=> { fwInstantiator.ForceClose(); });
            }
        }

        public void CloseSpinGame()
        {
            fwInstantiator.Close();
        }

        public void ResetData()
        {
            GlobalTimer.RemoveTimerPrefs(timerName);
        }

        internal void SetHaveSpin()
        {
            if (HaveDailySpin) return;

            HaveDailySpin = true;

            if (gTimer != null)
            {
                gTimer.RemoveTimerPrefs();
                IsWork = false;
            }

            if (timerText)
                timerText.text = "";

            if (fwInstantiator.MiniGame)
            {
                Debug.Log("time passed daily spin - > start mini game");
                fwInstantiator.MiniGame.SetBlocked(false, false);
            }
        }

        public class DailySpinResponse
        {
            public bool IsAvailable { get; set; }
            public int RemainingSeconds { get; set; }
        }

        public class ProcessDailySpinResultResponse
        {
            public string Message { get; set; } = string.Empty;
            public long ProcessedCoins { get; set; }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DailySpinController))]
    public class DailySpinControllerEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (!EditorApplication.isPlaying)
            {
                if (test = EditorGUILayout.Foldout(test, "Test"))
                {
                    EditorGUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("Reset Data"))
                    {
                        DailySpinController t = (DailySpinController)target;
                        t.ResetData();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Set daily spin"))
                {
                    DailySpinController t = (DailySpinController)target;
                    t.SetHaveSpin();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
#endif
}
