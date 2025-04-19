using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mkey
{
    public enum WinLineFlashing { All, Sequenced, None }
    public enum JackPotType { None, Mini, Maxi, Mega }

    public class SlotController : MonoBehaviour
    {
        // 슬롯 컨트롤, 메뉴 컨트롤 등 주요 참조 객체 선언.
        #region main reference
        [SerializeField]
        private SlotMenuController menuController;
        [SerializeField]
        private SlotControls controls;
        [SerializeField]
        private WinController winController;
        [SerializeField]
        private AudioClip spinSound;
        [SerializeField]
        private AudioClip looseSound;
        [SerializeField]
        private AudioClip winCoinsSound;
        [SerializeField]
        private AudioClip winFreeSpinSound;
        [SerializeField]
        public Text toalBetSumText;
        #endregion main reference

        // 슬롯 머신에서 사용되는 심볼(icon) 정의.
        #region icons
        [SerializeField, ArrayElementTitle("iconSprite"), NonReorderable]
        public SlotIcon[] slotIcons;

        [Space(8)]
        [SerializeField]
        public WinSymbolBehavior[] winSymbolBehaviors;
        #endregion icons

        #region jackpot symbol order
        [SerializeField]
        public bool useJackpotSymbOrder;
        [SerializeField]
        private List<int> jackpotSymbOrder;
        #endregion jackpot symbol order

        // 슬롯 머신의 페이라인(pay line)과 보상 데이터 정의
        #region payTable
        public List<PayLine> payTable;
        internal List<PayLine> payTableFull; // extended  if useWild
        #endregion payTable

        // 와일드 심볼(wild), 스캐터(scatter) 심볼 등 특별 기능 심볼.
        #region special major
        public int scatter_id;
        public int wild_id;
        public bool useWild;
        public bool useScatter;
        #endregion special major

        // 스캐터 심볼에 대한 페이아웃(pay-out) 정의.
        #region scatter paytable
        public List<ScatterPay> scatterPayTable;
        #endregion scatter paytable

        // 슬롯 머신에서 사용하는 게임 오브젝트(prefab) 참조.
        #region prefabs
        public GameObject tilePrefab;
        public GameObject particlesStars;
        [SerializeField]
        private WarningMessController BigWinPrefab;
        #endregion prefabs

        #region slotGroups
        [NonReorderable]
        public SlotGroupBehavior[] slotGroupsBeh;
        #endregion slotGroups

        #region tweenTargets
        public Transform bottomJumpTarget;
        public Transform topJumpTarget;
        #endregion tweenTargets

        // 슬롯 회전(스핀) 애니메이션 옵션 정의.
        #region spin options
        [SerializeField]
        private EaseAnim inRotType = EaseAnim.EaseInExpo; // in rotation part
        [SerializeField]
        [Tooltip("Time in rotation part, 0-1 sec")]
        private float inRotTime = 0.1f;
        [SerializeField]
        [Tooltip("In rotation part angle, 0-10 deg")]
        private float inRotAngle = 3f;

        [Space(16, order = 0)]
        [SerializeField]
        private EaseAnim outRotType = EaseAnim.EaseOutExpo;   // out rotation part
        [SerializeField]
        [Tooltip("Time out rotation part, 0-1 sec")]
        private float outRotTime = 0.1f;
        [SerializeField]
        [Tooltip("Out rotation part angle, 0-10 deg")]
        private float outRotAngle = 3f;

        [Space(16, order = 0)]
        [SerializeField]
        private EaseAnim mainRotateType = EaseAnim.EaseInExpo;   // main rotation part
        [SerializeField]
        [Tooltip("Time main rotation part, sec")]
        private float mainRotateTime = 0.7f; // 릴의 주요 회전 시간을 결정 -> 작을수록 빠르게 회전
        [Tooltip("min 0% - max 20%, change rotateTime")]
        [SerializeField]
        private int mainRotateTimeRandomize = 0; // 회전 시간에 랜덤 요소를 추가 -> 작을수록 일정하고 빠르게 회전
        #endregion spin options

        #region options
        public WinLineFlashing winLineFlashing = WinLineFlashing.Sequenced;
        public bool winSymbolParticles = true;
        public RNGType RandomGenerator = RNGType.Unity;
        [SerializeField]
        [Tooltip("Multiply win coins by bet multiplier")]
        public bool useLineBetMultiplier = true;
        [SerializeField]
        [Tooltip("Multiply win spins by bet multiplier")]
        public bool useLineBetFreeSpinMultiplier = false;
        [SerializeField]
        [Tooltip("Debug to console predicted symbols")]
        private bool debugPredictSymbols = false;
        #endregion options 

        // 팟 로직(미니, 맥시, 메가) 구현.
        #region jack pots
        [Space(8)]
        public int jp_symbol_id = -1;
        public bool useMiniJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int miniJackPotCount = 7;
        public bool useMaxiJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int maxiJackPotCount = 9;
        public bool useMegaJacPot = false;
        [Tooltip("Count identical symbols on screen")]
        public int megaJackPotCount = 10;
        public JackPotController jpController;
        #endregion jack pots 

        #region levelprogress
        [SerializeField]
        [Tooltip("Multiply level progress by bet multiplier")]
        public bool useLineBetProgressMultiplier = true;
        [SerializeField]
        [Tooltip("Player level progress for loose spin")]
        public float loseSpinLevelProgress = 0.5f;
        [SerializeField]
        [Tooltip("Player level progress for win spin per win line")]
        public float winSpinLevelProgress = 2.0f;
        #endregion level progress

        // 임시 변수 선언.
        #region temp vars
        private int slotTilesCount = 30;
        private WaitForSeconds wfs1_0;
        private WaitForSeconds wfs0_2;
        private WaitForSeconds wfs0_1;
        private RNG rng; // random numbers generator

        private uint spinCount = 0;
        private bool slotsRunned = false;
        private bool playFreeSpins = false;
        private bool isFreeSpin = false;

        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private RoomController RoomController { get { return RoomController.Instance; } }
        private GuiController MGUI { get { return GuiController.Instance; } }
        private List<List<Triple>> tripleCombos;
        private JackPotType jackPotType = JackPotType.None;
        private int jackPotWinCoins = 0;
        #endregion temp vars

        // 슬롯 머신의 주요 이벤트(스핀 시작, 끝 등) 정의.
        #region events
        public Action SpinPressEvent;
        public Action StartSpinEvent;
        public Action EndSpinEvent;
        public Action BeginWinCalcEvent;
        public Action EndWinCalcEvent;
        public Action StartFreeGamesEvent;
        public Action EndFreeGamesEvent;
        #endregion events

        #region dev
        public string payTableJsonString;
        #endregion dev

        public static SlotController CurrentSlot { get; private set; }

        public bool useWildInFirstPosition = false;

        #region regular
        private void OnValidate() // Unity 에디터에서 Inspector에서 값이 변경될 때 호출
        {
            Validate();
        }

        /// <summary>
        /// 클래스 변수 값의 유효성을 보장하기 위해 제한을 설정하거나 조건을 검사
        /// </summary>
        void Validate()
        {
            mainRotateTime = (float)Mathf.Clamp(mainRotateTime, 0, 1.0f); // mainRotateTime 값을 0에서 0.7f 사이로 강제 제한
            mainRotateTimeRandomize = (int)Mathf.Clamp(mainRotateTimeRandomize, 0, 20); // mainRotateTimeRandomize 값을 0에서 20 사이의 정수로 강제 제한

            inRotTime = Mathf.Clamp(inRotTime, 0, 1f); // inRotTime 값을 0에서 1f 사이로 제한합니다.
            inRotAngle = Mathf.Clamp(inRotAngle, 0, 10); // inRotAngle 값을 0에서 10 사이로 제한합니다.

            outRotTime = Mathf.Clamp(outRotTime, 0, 1f);
            outRotAngle = Mathf.Clamp(outRotAngle, 0, 10);

            miniJackPotCount = Mathf.Max(1, miniJackPotCount); // miniJackPotCount 값이 항상 1 이상이 되도록 설정
            maxiJackPotCount = Mathf.Max((useMiniJacPot) ? miniJackPotCount + 1 : 1, maxiJackPotCount); // useMiniJacPot이 true이면 miniJackPotCount + 1 또는 maxiJackPotCount 중 더 큰 값을 적용 false이면 miniJackPotCount + 1 또는 maxiJackPotCount 중 더 큰 값을 적용
            megaJackPotCount = Mathf.Max((useMaxiJacPot) ? maxiJackPotCount + 1 : 1, megaJackPotCount); // useMaxiJacPot이 true이면 maxiJackPotCount + 1 또는 megaJackPotCount 중 더 큰 값을 적용
            if (scatterPayTable != null)
            {
                foreach (var item in scatterPayTable)
                {
                    if (item != null)
                    {
                        item.payMult = Mathf.Max(0, item.payMult);
                        //item.payMult = Mathf.Max(1, item.payMult);
                    }
                }
            }
        }
      
        void Start()
        {
            wfs1_0 = new WaitForSeconds(1.0f);
            wfs0_2 = new WaitForSeconds(0.2f);
            wfs0_1 = new WaitForSeconds(0.1f);

            // 릴 생성
            int slotsGrCount = slotGroupsBeh.Length;
            ReelData[] reelsData = new ReelData[slotsGrCount];
            ReelData reelData;
            int i = 0;
            foreach (SlotGroupBehavior sGB in slotGroupsBeh)
            {
                reelData = new ReelData(sGB.symbOrder);
                reelsData[i++] = reelData;
                sGB.CreateSlotCylinder(slotIcons, slotTilesCount, tilePrefab);
            }

            // 페이 테이블 생성
            CreateFullPaytable();

            // 랜덤 객체 생성
            rng = new RNG(RNGType.Unity, reelsData);

            SetInputActivity(true);
            CurrentSlot = this;
        }
        #endregion regular

        /// <summary>
        /// 일정 확률로 잭팟을 발생시키는 메서드
        /// </summary>
        private bool CheckJackpotChange()
        {
            float jackpotChange = (float)controls.MegaJackPotProb;
            float randomValue = UnityEngine.Random.Range(0f, 1f);


            if (randomValue <= jackpotChange)
            {
                Debug.Log($"잭팟 발생, 잭팟 확률: {jackpotChange}");
                Debug.Log($"랜덤 변수 확률: {randomValue}");
                return true; // 잭팟 발생
            }

            return false;
        }

        /// <summary>
        /// 슬롯을 회전시킬 때 호출되는 메서드
        /// </summary>
        internal void SpinPress()
        {
            SpinPressEvent?.Invoke(); // 스핀 버튼을 눌렸을 때 SpinPressEvent 호출
            ValidRunSlots(); // RunSlots() 메서드로 슬롯 회전 시작
        }

        /// <summary>
        /// 슬롯 회전 유효성 검사 메서드
        /// </summary>
        private void ValidRunSlots()
        {
            // 슬롯이 이미 실행 중이라면 종료
            if (slotsRunned)
                return;

            if (!controls.AnyLineSelected)
            {
                MGUI.ShowMessage(null, "Please select a any line.", 1.5f, null);
                controls.ResetAutoSpinsMode();
                return;
            }

            // 프리 스핀 여부 확인 후 베팅 적용
            if (!controls.CheckFreeSpin()) // 프리 스핀이 아닐 때만 베팅 적용
            {
                if (!controls.CheckBet()) // 베팅 가능 여부 확인
                {
                    MGUI.ShowMessage(null, "You have no money.", 1.5f, null);
                    controls.ResetAutoSpinsMode();
                    return;
                }
                isFreeSpin = false; // 프리 스핀이 아닌 경우 false로 설정

                ++spinCount; // 프리스핀이 아닐 때만 스핀 횟수 증가
            }
            else
            {
                if (!isFreeSpin)
                    StartFreeGamesEvent?.Invoke();
                isFreeSpin = true;
            }

            StartCoroutine(RunSlotsAsync());
        }

        /// <summary>
        /// 슬롯 회전 애니메이션 실행 + 결과 확인 등 실제 동작 메서드
        /// </summary>
        /// <returns></returns>
        private IEnumerator RunSlotsAsync()
        {
            PrepareRotate(); // 회전 시작 준비

            yield return RotateSlotAsync(); // 슬롯 애니메이션 실행

            // 슬롯 애니메이션 종료 후 결과 확인
            EndSpinEvent?.Invoke(); // 스핀 종료 이벤트 호출
            BeginWinCalcEvent?.Invoke(); // 승리 여부 계산 시작 이벤트

            // 페이라인과 일치하는 심볼이 있는지 검사 후 일치하는 심볼이 있다면 win 변수에 저장
            winController.SearchWinSymbols();

            // 승리 여부 확인
            if (winController.HasAnyWinn(ref jackPotType)) // 승리했다면
            {
                yield return HandleWin(); // 승리 처리
            }
            else
            {
                HandleLose(); // 패배 처리
            }

            while (!MGUI.HasNoPopUp) yield return wfs0_1;  // 모든 팝업이 닫힐 때까지 대기

            if (controls.Auto && controls.AutoSpinsCounter >= controls.AutoSpinCount)
            {
                controls.ResetAutoSpinsMode();
            }

            if (controls.Auto || playFreeSpins)
            {
                ValidRunSlots();
            }
        }

        /// <summary>
        /// 슬롯 회전 준비 메서드
        /// </summary>
        private void PrepareRotate()
        {
            // 사용자 입력 비활성화
            SetInputActivity(false);
            winController.HideAllLines();

            // 승리 이펙트 및 상태 초기화
            winController.WinEffectsShow(false, false); // 라인 이펙트 비활성화
            winController.WinShowCancel(); // 현재 진행 중이거나 보이고 있는 Win 애니메이션 중단
            winController.ResetLineWinning(); // 이전 승리 정보 초기화
            controls.JPWinCancel(); // 잭팟 관련 승리 애니메이션 또는 결과를 중단 또는 리셋

            // 슬롯 상태를 실행 중으로 설정
            slotsRunned = true;
            if (controls.Auto && !isFreeSpin)
                controls.IncAutoSpinsCounter(); // 자동 스핀 모드일 경우 자동 스핀 횟수 증가

            // 잭팟 초기화
            jackPotWinCoins = 0;
            jackPotType = JackPotType.None;

            // 승리 코인 초기화
            MPlayer.SetWinCoinsCount(0);

            // 사운드 실행
            MSound.StopAllClip(false); // 모든 백그라운드 음악 중지
            MSound.PlayClip(0f, true, spinSound);

            // 스핀 시작 이벤트 호출
            StartSpinEvent?.Invoke();
        }

        /// <summary>
        /// 슬롯 회전 애니메이션 및 베팅 처리 메서드
        /// </summary>
        /// <returns></returns>
        private IEnumerator RotateSlotAsync()
        {
            // 슬롯 애니메이션 실행
            bool fullRotated = false;
            rng.Update(); // 슬롯을 돌렸을 때 줄별로 나올 심볼을 업데이트

            // 굿럭 멘트 설정
            if (controls.InfoText != null)
                controls.SetTextString(controls.InfoText, "Good Lock!");
            if (controls.WinAmountText != null)
                controls.SetTextString(controls.WinAmountText, "Good Lock!");

            RecurRotateSlots(() => { MSound.StopClips(spinSound); fullRotated = true; }); // 무한 슬롯 회전 시작

            // 베팅 요청을 보내고 베팅을 받은 후 SetStopReelsOrder 메서드를 호출하여 슬롯을 멈추도록 함
            if (!controls.ApplyFreeSpin()) // 프리스핀인 경우 횟수 차감
            {
                Task<BetResponse> sendBetTask = RoomSocketManager.Instance.SendBetReqeust(MPlayer.Id, controls.TotalBet);
                while (!sendBetTask.IsCompleted)
                    yield return null;
            }

            SetStopReelsOrder(); // 각 릴 심볼을 결정하며 릴 중지

            while (!fullRotated)
                yield return wfs0_2;  // 슬롯 회전 완료 대기
        }

        /// <summary>
        /// 잭팟 처리 메서드
        /// </summary>
        /// <returns></returns>
        private IEnumerator HandleJackpotAsync()
        {
            // 잭팟 금액 가져오기
            jackPotWinCoins = controls.GetJackPotCoins(jackPotType);

            // 잭팟 체크
            if (jackPotType != JackPotType.None && jackPotWinCoins > 0)
            {
                MPlayer.SetWinCoinsCount(jackPotWinCoins);
                //MPlayer.AddCoins(jackPotWinCoins);
                StartCoroutine(RoomController.HandleJackpotWin(jackPotType, jackPotWinCoins));

                spinCount = 0; // 스핀횟수 초기화

                if (controls.HasFreeSpin || controls.Auto)
                {
                    // 잭팟 UI 표시 및 처리
                    controls.JPWinShow(jackPotWinCoins, jackPotType);
                    yield return new WaitForSeconds(3.0f); // delay
                    controls.JPWinCancel();
                }
                else
                {
                    // 잭팟 UI 표시 및 처리
                    controls.JPWinShow(jackPotWinCoins, jackPotType);
                    yield return new WaitForSeconds(5.0f);// delay
                }
            }
        }

        /// <summary>
        /// 승리 처리 메서드
        /// </summary>
        /// <returns></returns>
        private IEnumerator HandleWin()
        {
            //3b ---- show particles, line flasing  -----------
            //winController.WinEffectsShow(winLineFlashing == WinLineFlashing.All, winSymbolParticles);

            while (!MGUI.HasNoPopUp)
                yield return wfs0_1;

            yield return HandleJackpotAsync(); // 잭팟 처리

            // 일반 승리 코인 계산 -> 승리 금액을 payMultiplier와 베팅 금액을 고려하여 조정
            int winCoins = winController.GetWinCoins();
            int payMultiplier = winController.GetPayMultiplier();
            winCoins *= payMultiplier;

            if (useLineBetMultiplier)
                winCoins *= controls.LineBet;
            //Debug.Log("Original wincoins: " + winCoins);

            winCoins = (int)(winCoins * (1 + (RoomController.resultPayout / 100.0))); // 승리했을 때 코인 값 계산
                                                                                      //Debug.Log("plus wincoins: " + winCoins);

            MPlayer.SetWinCoinsCount(jackPotWinCoins + winCoins);
            MPlayer.AddCoins(winCoins);
            //Debug.Log("total: " + (jackPotWinCoins + winCoins));

            // 빅윈 처리
            //Debug.Log(jackPotWinCoins + winCoins);
            if (winCoins > 0)
            {
                bool bigWin = winCoins >= MPlayer.MinWin && MPlayer.UseBigWinCongratulation;
                //bool bigWin = winCoins >= 0;
                if (!bigWin)
                    MSound.PlayClip(0, winCoinsSound);
                else
                {
                    while (!MGUI.HasNoPopUp)
                        yield return wfs0_1;  // wait for prev popup closing

                    bool bigWinClosed = false;
                    MGUI.ShowMessage(BigWinPrefab, "", winCoins.ToString(), 1f, () =>
                    {
                        bigWinClosed = true;
                    });

                    while (!bigWinClosed)
                        yield return wfs0_1;  // bigWin 팝업이 닫힐 때까지 대기
                }
            }

            // 프리 스핀 처리
            int winSpins = winController.GetWinSpins();
            int winLinesCount = winController.GetWinLinesCount();
            int freeSpinsMultiplier = winController.GetFreeSpinsMultiplier();
            winSpins *= freeSpinsMultiplier;

            if (useLineBetFreeSpinMultiplier)
                winSpins *= controls.LineBet;
            if (winSpins > 0)
                MSound.PlayClip((winCoins > 0 || jackPotWinCoins > 0) ? 1.5f : 0, winFreeSpinSound);

            // 프리 스핀 연속 여부 확인 및 종료 처리
            controls.AddFreeSpins(winSpins);
            playFreeSpins = controls.AutoPlayFreeSpins && controls.HasFreeSpin; // 자동 프리 스핀 모드가 켜져 있고, 아직 프리 스핀이 남아있다면 계속 프리 스핀 실행

            // 지금이 프리 스핀 회차 중이었고, 추가 프리 스핀도 없으면 → 프리 스핀 종료 이벤트 호출
            if (isFreeSpin && !playFreeSpins)
                EndFreeGamesEvent?.Invoke();

            // 스캐터 승리 이벤트 처리 -> 만약 이번 승리 결과가 스캐터(winController.scatterWin)에 해당하면 -> 연결된 이벤트 호출
            if (winController.scatterWin != null && winController.scatterWin.WinEvent != null)
                winController.scatterWin.WinEvent.Invoke();

            // 승리 계산 종료 이벤트 호출
            EndWinCalcEvent?.Invoke();

            // 레벨 진행도 증가 처리
            //while (!MGUI.HasNoPopUp) yield return wfs0_1; // wait for the prev popup to close
            //MPlayer.AddLevelProgress( (useLineBetProgressMultiplier)? winSpinLevelProgress * winLinesCount * controls.LineBet : winSpinLevelProgress * winLinesCount); // for each win line
            //MPlayer.AddLevelProgress(winSpinLevelProgress); 

            // 승리 라인에 따른 이펙트 시작
            winController.StartLineEvents();

            // 미니게임, 팝업 대기
            while (SlotEvents.Instance && SlotEvents.Instance.MiniGameStarted)
                yield return wfs0_1;  // wait for the mini game to close

            Debug.Log($"스핀 횟수: {spinCount}"); // 프리스핀이 아닐 때만 카운팅하고 메세지 팝업 띄우기
            StartCoroutine(ShowJackpotHintMessage(spinCount));

            while (!MGUI.HasNoPopUp)
                yield return wfs0_1;  // wait for the closin all popups

            // 슬롯 비활성화 해제 (다시 조작 가능하게)
            slotsRunned = false;
            if (!playFreeSpins)
            {
                SetInputActivity(true);
            }
            MSound.PlayCurrentMusic();

            // 페이라인의 애니메이션을 보여주고, 해당 이벤트가 끝날 때까지 대기
            bool showEnd = false;
            winController.WinSymbolShow(winLineFlashing,
                   (windata) => //linewin
                   {
                       //event can be interrupted by player
                       //if (windata!=null)  Debug.Log("lineWin : " +  windata.ToString());
                   },
                   () => //scatter win
                   {
                       //event can be interrupted by player
                   },
                   () => //jack pot 
                   {
                       //event can be interrupted by player
                   },
                   () =>
                   {
                       showEnd = true;
                   }
                   );
            while (!showEnd)
                yield return wfs0_2;  // wait for show end
        }

        /// <summary>
        /// 패배 처리 메서드
        /// </summary>
        private void HandleLose()
        {
            // 패배 로직 처리::
            //MPlayer.AddLevelProgress(loseSpinLevelProgress);

            // 멘트 처리
            // 굿럭 멘트 설정##
            if (controls.InfoText != null)
                controls.SetTextString(controls.InfoText, "One more?");
            if (controls.WinAmountText != null)
                controls.SetTextString(controls.WinAmountText, "One more?");

            playFreeSpins = (controls.AutoPlayFreeSpins && controls.HasFreeSpin);

            Debug.Log($"스핀 횟수: {spinCount}"); // 프리스핀이 아닐 때만 카운팅하고 메세지 팝업 띄우기
            StartCoroutine(ShowJackpotHintMessage(spinCount));

            // 슬롯 비활성화 해제 (다시 조작 가능하게)
            slotsRunned = false;
            SetInputActivity(true);
            MSound.PlayCurrentMusic();
        }

        /// <summary>
        /// 스핀 횟수에 따라 메세지 팝업을 띄워주는 메서드
        /// </summary>
        /// <param name="spinCount"></param>
        /// <returns></returns>
        private IEnumerator ShowJackpotHintMessage(uint spinCount)
        {
            bool hintMessageClosed = false;
            bool shouldShowMessage = true;

            switch (spinCount)
            {
                case 25:
                    MGUI.ShowMessage(null, "\nYou're off to a good start!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 50:
                    MGUI.ShowMessage(null, "\nJackpot is on the horizon!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 75:
                    MGUI.ShowMessage(null, "\nYou're getting closer to the jackpot!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 100:
                    MGUI.ShowMessage(null, "\n100 spins in — keep going,\n\nyour jackpot awaits!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 120:
                    MGUI.ShowMessage(null, "\nThe jackpot is drawing nearer...", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 140:
                    MGUI.ShowMessage(null, "\nYou're more than halfway there!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 160:
                    MGUI.ShowMessage(null, "\nFeeling lucky?\n\nYou're closing in!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 180:
                    MGUI.ShowMessage(null, "\nAlmost there\n\nthe jackpot is approaching!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                case 200:
                    MGUI.ShowMessage(null, "\n200 spins in!\n\nJackpot can't hide for long!", 1.5f, () => { hintMessageClosed = true; });
                    break;
                default:
                    shouldShowMessage = false;
                    break;
            }

            if (shouldShowMessage)
            {
                while (!hintMessageClosed)
                    yield return new WaitForSeconds(1.6f);
            }
        }

        public void SlotDelay(float seconds, Action action)
        {
            StartCoroutine(SlotDelayCoroutine(seconds, action));
        }

        private IEnumerator SlotDelayCoroutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        /// <summary>
        /// 무한 슬롯 회전 애니메이션 실행 메서드
        /// </summary>
        private void RecurRotateSlots(Action rotCallBack)
        {
            ParallelTween pT = new ParallelTween();
            int[] rands = { -1, -1, -1, -1, -1 };

            // 홀드 기능
            HoldFeature hold = controls.Hold;
            bool[] holdReels = null;
            if (controls.UseHold && hold && hold.Length == rands.Length)
            {
                holdReels = hold.GetHoldReels();
                for (int i = 0;i < rands.Length;i++)
                {
                    rands[i] = holdReels[i] ? slotGroupsBeh[i].CurrOrderPosition : rands[i]; // hold position
                }
            }

            for (int i = 0;i < slotGroupsBeh.Length;i++)
            {
                int n = i;
                int r = rands[i];

                if (holdReels == null || (holdReels != null && !holdReels[i]))
                {
                    // 잭팟이 발생했을 땐 잭팟 심볼에서 멈추도록 설정
                    pT.Add((callBack) =>
                    {
                        slotGroupsBeh[n].NextRotateCylinderEase(mainRotateType, inRotType, outRotType,
                            mainRotateTime, mainRotateTimeRandomize / 100f,
                            inRotTime, outRotTime, inRotAngle, outRotAngle,
                            r, callBack);
                    });
                }
            }

            pT.Start(rotCallBack);
        }

        private void SetStopReelsOrder()
        {
            int[] rands = rng.GetRandSymbols(); // 각 릴에 대한 다음 정지 위치(심볼 인덱스)를 결정
            bool isJackpot = (useJackpotSymbOrder && jackpotSymbOrder.Count != 0) && CheckJackpotChange(); // 일정 확률로 잭팟 발생

            // 홀드 기능
            HoldFeature hold = controls.Hold;
            bool[] holdReels = null;
            if (controls.UseHold && hold && hold.Length == rands.Length)
            {
                holdReels = hold.GetHoldReels();
            }

            for (int i = 0;i < slotGroupsBeh.Length;i++)
            {
                int n = i;
                int r = rands[i];
                int jackpotSymb = isJackpot ? jackpotSymbOrder[i] : 0;
                if (holdReels == null || (holdReels != null && !holdReels[i]))
                {
                    // 잭팟이 발생했을 땐 잭팟 심볼에서 멈추도록 설정
                    slotGroupsBeh[n].SetNextOrder(isJackpot ? jackpotSymb - 1 : r);
                }
            }
        }

        /// <summary>
        /// 슬롯 회전 애니메이션 실행 메서드
        /// </summary>
        /// <param name="rotCallBack"></param>
        private void RotateSlots(Action rotCallBack)
        {
            ParallelTween pT = new ParallelTween();
            int [] rands = rng.GetRandSymbols(); // 각 릴에 대한 다음 정지 위치(심볼 인덱스)를 결정
            bool isJackpot = (useJackpotSymbOrder && jackpotSymbOrder.Count != 0) && CheckJackpotChange(); // 일정 확률로 잭팟 발생

            // 홀드 기능
            HoldFeature hold = controls.Hold;
            bool[] holdReels = null;
            if (controls.UseHold && hold && hold.Length == rands.Length)
            {
                holdReels = hold.GetHoldReels();
                for (int i = 0; i < rands.Length; i++)
                {
                    rands[i] = holdReels[i] ? slotGroupsBeh[i].CurrOrderPosition : rands[i]; // hold position
                }
            }

            // 디버그 모드: 다음 심볼 예측
            #if UNITY_EDITOR
            #region prediction visible symbols on reels
            if (debugPredictSymbols)
                for (int i = 0;i < rands.Length;i++)
                {
                    Debug.Log("------- Reel: " + i + " ------- (down up)");
                    for (int r = 0;r < slotGroupsBeh[i].RayCasters.Length;r++)
                    {
                        int sO = (int)Mathf.Repeat(rands[i] + r, slotGroupsBeh[i].symbOrder.Count);
                        int sID = slotGroupsBeh[i].symbOrder[sO];
                        string sName = slotIcons[sID].iconSprite.name;
                        Debug.Log("NextSymb ID: " + sID + " ;name : " + sName);
                    }
                }
            #endregion prediction
            #endif

            for (int i = 0; i < slotGroupsBeh.Length; i++)
            {
                int n = i;
                int r = rands[i];
                int jackpotSymb = isJackpot ? jackpotSymbOrder[i] : 0;

                if (holdReels == null || (holdReels != null && !holdReels[i]))
                {
                    // 잭팟이 발생했을 땐 잭팟 심볼에서 멈추도록 설정
                    pT.Add((callBack) =>
                    {
                        slotGroupsBeh[n].NextRotateCylinderEase(mainRotateType, inRotType, outRotType,
                            mainRotateTime, mainRotateTimeRandomize / 100f,
                            inRotTime, outRotTime, inRotAngle, outRotAngle,
                            isJackpot ? jackpotSymb - 1 : r, callBack);
                    });
                }
            }

            pT.Start(rotCallBack);
        }

        /// <summary>
        /// Set touch activity for game and gui elements of slot scene
        /// </summary>
        public void SetInputActivity(bool activity)
        {
            if (activity)
            {
                if (controls.HasFreeSpin)
                {
                    menuController.SetControlActivity(false); // preserve bet change if free spin available
                    controls.SetControlActivity(false, true);
                }
                else
                {
                    menuController.SetControlActivity(activity);
                    controls.SetControlActivity(true, true);
                }
            }
            else
            {
                menuController.SetControlActivity(activity); 
                controls.SetControlActivity(activity, controls.Auto); 
            }
        }

        /// <summary>
        /// Calculate propabilities
        /// </summary>
        /// <returns></returns>
        public string[,] CreatePropabilityTable()
        {
            List<string> rowList = new List<string>();
            string[] iconNames = GetIconNames(false);
            int length = slotGroupsBeh.Length;
            string[,] table = new string[length + 1, iconNames.Length + 1];

            rowList.Add("reel / icon");
            rowList.AddRange(iconNames);
            SetRow(table, rowList, 0, 0);

            for (int i = 1; i <= length; i++)
            {
                table[i, 0] = "reel #" + i.ToString();
                SetRow(table, new List<float>(slotGroupsBeh[i - 1].GetReelSymbHitPropabilities(slotIcons)), 1, i);
            }
            return table;
        }

        /// <summary>
        /// Calculate propabilities
        /// </summary>
        /// <returns></returns>
        public string[,] CreatePayTable(out float sumPayOut, out float sumPayoutFreeSpins)
        {
            List<string> row = new List<string>();
            List<float[]> reelSymbHitPropabilities = new List<float[]>();
            string[] iconNames = GetIconNames(false);

            sumPayOut = 0;
            CreateFullPaytable();
            int rCount = payTableFull.Count + 1;
            int cCount = slotGroupsBeh.Length + 3;
            string[,] table = new string[rCount, cCount];
            row.Add("PayLine / reel");
            for (int i = 0; i < slotGroupsBeh.Length; i++)
            {
                row.Add("reel #" + (i + 1).ToString());
            }
            row.Add("Payout");
            row.Add("Payout, %");
            SetRow(table, row, 0, 0);

            PayLine pL;
            List<PayLine> freeSpinsPL = new List<PayLine>();  // paylines with free spins

            for (int i = 0; i < payTableFull.Count; i++) 
            {
                pL = payTableFull[i];
                table[i + 1, 0] = "Payline #" + (i + 1).ToString();
                table[i + 1, cCount - 2] = pL.pay.ToString();
                float pOut = pL.GetPayOutProb(this);
                sumPayOut += pOut;
                Debug.Log("체크");
                table[i + 1, cCount - 1] = pOut.ToString("F6");
                SetRow(table, new List<string>(pL.Names(slotIcons, slotGroupsBeh.Length)), 1, i + 1);
                if (pL.freeSpins > 0) freeSpinsPL.Add(pL);
            }

            Debug.Log("sum (without free spins) % = " + sumPayOut);

            sumPayoutFreeSpins = sumPayOut;
            foreach (var item in freeSpinsPL)
            {
                sumPayoutFreeSpins += sumPayOut * item.GetProbability(this) * item.freeSpins;
            }
            Debug.Log("sum (with free spins) % = " + sumPayoutFreeSpins);

            return table;
        }

        private void SetRow<T>(string[,] table, List<T> row, int beginColumn, int rowNumber)
        {
            if (rowNumber >= table.GetLongLength(0)) return;

            for (int i = 0; i < row.Count; i++)
            {
               // Debug.Log("sr"+i);
                if (i + beginColumn < table.GetLongLength(1)) table[rowNumber, i + beginColumn] = row[i].ToString();
            }
        }

        public string[] GetIconNames(bool addAny)
        {
            if (slotIcons == null || slotIcons.Length == 0) return null;
            int length = (addAny) ? slotIcons.Length + 1 : slotIcons.Length;
            string[] sName = new string[length];
            if (addAny) sName[0] = "any";
            int addN = (addAny) ? 1 : 0;
            for (int i = addN; i < length; i++)
            {
                if (slotIcons[i - addN] != null && slotIcons[i - addN].iconSprite != null)
                {
                    sName[i] = slotIcons[i - addN].iconSprite.name;
                }
                else
                {
                    sName[i] = (i - addN).ToString();
                }
            }
            return sName;
        }

        internal WinSymbolBehavior GetWinPrefab(string tag)
        {
            if (winSymbolBehaviors == null || winSymbolBehaviors.Length == 0) return null;
            foreach (var item in winSymbolBehaviors)
            {
                if (item.WinTag.Contains(tag))
                {
                    return item;
                }
            }
            return null;
        }

        private void CreateFullPaytable()
        {
            payTableFull = new List<PayLine>();
            for (int j = 0; j < payTable.Count; j++)
            {
                payTable[j].ClampLine(slotGroupsBeh.Length);
                payTableFull.Add(payTable[j]);
                if (useWild) payTableFull.AddRange(payTable[j].GetWildLines(this));
            }
        }

        // 페이라인, 잭팟, 확률 계산 로직.
        #region calculate
        public void CreatTripleCombos()
        {
            Measure("triples time", () => {
                List<List<int>> triplesComboNumbers;  //0 0 0 0 0; 0 0 0 0 1 .... 24 24 24 24 24
                triplesComboNumbers = new List<List<int>>();
                ComboCounterT cct = new ComboCounterT(slotGroupsBeh);
                List<int> combo = cct.combo;
                //Debug.Log(combo[0] + " : " + combo[1] + " : " + combo[2] + " : " + combo[3] + " : " + combo[4]);
                triplesComboNumbers.Add(new List<int>(combo));

                int i = 0;
                while (cct.NextCombo())
                {
                    combo = cct.combo;
                    //if(i<100)  Debug.Log(combo[0] + " : " + combo[1] + " : " + combo[2] + " : " + combo[3] + " : " + combo[4]);
                    triplesComboNumbers.Add(new List<int>(combo));
                    i++;
                }
                tripleCombos = new List<List<Triple>>();
                Debug.Log(triplesComboNumbers.Count);
                List<Triple> trList;
                Triple tr = null;
                foreach (var item in triplesComboNumbers)
                {
                    trList = new List<Triple>();
                    for (int t = 0; t < item.Count; t++)
                    {
                        tr = slotGroupsBeh[t].triples[item[t]];
                        trList.Add(tr);
                    }
                    tripleCombos.Add(trList);
                }

                Debug.Log("tripleCombos.Count " + tripleCombos.Count);
            });
            // Debug.Log(tr.ToString());
        }

        /// <summary>
        /// Calc win for triple
        /// </summary>
        /// <param name="trList"></param>
        public void TestWin()
        {
            Measure("test time", () =>
            {
                double sumPayOUt = 0;
                int sumFreeSpins = 0;
                double sumBets = 0;
                LineBehavior[] lbs = FindObjectsOfType<LineBehavior>();
                //Debug.Log("lines count: " + lbs.Length);
                int linesCount = lbs.Length;
                int i = 0;
                int wins = 0;
                double totalBet = linesCount * linebet;
                Debug.Log("totalBet: " + totalBet);
                for (int w = 0; w < 10000; w++)
                {
                    int r = UnityEngine.Random.Range(0, tripleCombos.Count);
                    var item = tripleCombos[r];
                    if (sumFreeSpins > 0) { sumFreeSpins--; }
                    else
                    {
                        sumBets += (totalBet);
                    }
                    int freeSpins = 0;
                    int pay = 0;
                    int payMult = 1;

                    int freeSpinsScat = 0;
                    int payScat = 0;
                    int payMultScat = 1;

                    CalcWin(item, lbs, ref freeSpins, ref pay, ref payMult, ref freeSpinsScat, ref payScat, ref payMultScat);
                    sumPayOUt += ((double)pay * linebet);
                    sumPayOUt += ((double)payScat * totalBet);
                    sumFreeSpins += freeSpins;
                    if (pay > 0 || payScat > 0 || freeSpins > 0) wins++;
                    i++;
                }


                //foreach (var item in tripleCombos)
                //{
                //    i++;
                //    if (sumFreeSpins > 0) {  sumFreeSpins--; }
                //   else {
                //        sumBets += (linesCount);
                //        }
                //    int freeSpins = 0;
                //    int pay = 0;
                //    int payMult = 1;
                //    CalcWin(item,lbs, ref freeSpins, ref pay, ref payMult);
                //    sumPayOUt += pay;
                //    sumFreeSpins += freeSpins;
                //    if (i > 1000000) break;
                //    if (pay > 0) wins++;
                //}
                Debug.Log("calcs: " + i + " ;payout: " + sumPayOUt + " ; sumBets: " + sumBets + "; wins: " + wins + " ;pOUt,%" + ((float)sumPayOUt / (float)sumBets * 100f));
            });
        }

        private double linebet = 0.004;
        /// <summary>
        /// Calc win for triple
        /// </summary>
        /// <param name="trList"></param>
        public void CalcWin()
        {
            Measure("calc time", () =>
            {
                LineBehavior[] lbs = FindObjectsOfType<LineBehavior>();
                winController.InitCalc();
                Debug.Log("lines count: " + lbs.Length);
                int linesCount = lbs.Length;
                int i = 0;
                int wins = 0;
                double pOut = 0;
                double comboProb = (1f / (double)tripleCombos.Count) / (double)linesCount;
                double comboProbScat = (1f / (double)tripleCombos.Count);
                int length = tripleCombos.Count;
                for (i = 0; i < length; i++)
                {
                    var item = tripleCombos[i];
                    int freeSpins = 0;
                    int pay = 0;
                    int payMult = 1;

                    int freeSpinsScat = 0;
                    int payScat = 0;
                    int payMultScat = 1;

                    CalcWin(item, lbs, ref freeSpins, ref pay, ref payMult, ref freeSpinsScat, ref payScat, ref payMultScat);
                    payMult *= payMultScat;
                    pay *= payMult;

                    pOut += ((double)pay * comboProb + (double)payScat * comboProbScat);

                    if (pay > 0 || payScat > 0 || freeSpins > 0)
                    {
                        wins++;
                        //  Debug.Log(pay + " : " + (pay * comboProb));
                    }
                }

                Debug.Log("calcs: " + i + " ; wins: " + wins + " ;payout %: " + (pOut * 100f));
            });
        }

        /// <summary>
        /// Calc win for triple
        /// </summary>
        /// <param name="trList"></param>
        public void CalcWin(List<Triple> trList, LineBehavior[] lbs, ref int freeSpins, ref int pay, ref int payMult, ref int freeSpinsScat, ref int payScat, ref int payMultScat)
        {
            SetTriples(trList);
            winController.SearchWinCalc();
            freeSpins = winController.GetLineWinSpinsCalc();
            pay = winController.GetLineWinCoinsCalc();
            payMult = winController.GetLinePayMultiplierCalc();

            freeSpinsScat = winController.GetScatterWinSpinsCalc();
            payScat = winController.GetScatterWinCoinsCalc();
            payMultScat = winController.GetScatterPayMultiplierCalc();
        }

        public void SetTriples(List<Triple> trList)
        {
            RayCaster[] rs;
            for (int i = 0; i < slotGroupsBeh.Length; i++)
            {
                rs = slotGroupsBeh[i].RayCasters;
                rs[0].ID = trList[i].ordering[2];
                rs[1].ID = trList[i].ordering[1];
                rs[2].ID = trList[i].ordering[0];
            }
        }

        public static void Measure(string message, Action measProc)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();//https://msdn.microsoft.com/ru-ru/library/system.diagnostics.stopwatch%28v=vs.110%29.aspx
            stopWatch.Start();
            if (measProc != null) { measProc(); }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            UnityEngine.Debug.Log(message + "- elapsed time: " + elapsedTime);
        }
        #endregion calculate

        #region dev
        public void RebuildLines()
        {
            foreach (var item in payTable)
            {
                int[] line = new int[slotGroupsBeh.Length];

                for (int i = 0; i < line.Length; i++)
                {
                    if (i < item.line.Length) line[i] = item.line[i];
                    else line[i] = -1;
                }
                item.line = line;
            }
        }

        public string PaytableToJsonString()
        {
            string res = "";
            ListWrapper<PayLine> lW = new ListWrapper<PayLine>(payTable);
            res = JsonUtility.ToJson(lW);
            return res;
        }

        public void SetPayTableFromJson()
        {
            Debug.Log("Json viewer - " + "http://jsonviewer.stack.hu/");
            Debug.Log("old paytable json: "  + PaytableToJsonString());

            if(string.IsNullOrEmpty(payTableJsonString))
            {
                Debug.Log("payTableJsonString : empty");
                return;
            }

            ListWrapper<PayLine> lWPB = JsonUtility.FromJson<ListWrapper<PayLine>>(payTableJsonString);
            if(lWPB!=null && lWPB.list!=null && lWPB.list.Count > 0)
            {
                payTable = lWPB.list;
            }
        }
        #endregion dev
    }

    public enum RNGType { Unity, MersenneTwister }
    public class RNG
    {
        private int[] randSymb;
        private RNGType rngType;
        private Action UpdateRNGAction;
        private ReelData[] reelsData;
        private RandomMT randomMT;

        public RNG(RNGType rngType, ReelData[] reelsData)
        {
            randSymb = new int[reelsData.Length];
            this.rngType = rngType;
            this.reelsData = reelsData;
            switch (rngType)
            {
                case RNGType.Unity:
                    UpdateRNGAction = UnityRNGUpdate;
                    break;
                case RNGType.MersenneTwister:
                    randomMT = new RandomMT();
                    UpdateRNGAction = MTRNGUpdate;
                    break;
                default:
                    UpdateRNGAction = UnityRNGUpdate;
                    break;
            }
        }

        public void Update()
        {
            UpdateRNGAction();
        }

        public int[] GetRandSymbols()
        {
            return randSymb;
        }

        int rand;
        private void UnityRNGUpdate()
        {
            for (int i = 0; i < randSymb.Length; i++)
            {
                rand = UnityEngine.Random.Range(0, reelsData[i].Length);
                randSymb[i] = rand;
            }
        }

        private void MTRNGUpdate()
        {
            for (int i = 0; i < randSymb.Length; i++)
            {
                rand = randomMT.RandomRange(0, reelsData[i].Length-1);
                randSymb[i] = rand;
            }
        }
    }

    [Serializable]
    public class ReelData
    {
        public List<int> symbOrder;
        public ReelData(List<int> symbOrder)
        {
            this.symbOrder = symbOrder;
        }
        public int Length
        {
            get { return (symbOrder == null) ? 0 : symbOrder.Count; }
        }
        public int GetSymbolAtPos(int position)
        {
            return (symbOrder == null || position >= symbOrder.Count) ? 0 : symbOrder.Count;
        }
    }

    /// <summary>
	/// Summary description for RandomMT.https://www.codeproject.com/Articles/5147/A-C-Mersenne-Twister-class
	/// </summary>
	public class RandomMT
    {
        private const int N = 624;
        private const int M = 397;
        private const uint K = 0x9908B0DFU;
        private const uint DEFAULT_SEED = 4357;

        private ulong[] state = new ulong[N + 1];
        private int next = 0;
        private ulong seedValue;


        public RandomMT()
        {
            SeedMT(DEFAULT_SEED);
        }
        public RandomMT(ulong _seed)
        {
            seedValue = _seed;
            SeedMT(seedValue);
        }

        public ulong RandomInt()
        {
            ulong y;

            if ((next + 1) > N)
                return (ReloadMT());

            y = state[next++];
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9D2C5680U;
            y ^= (y << 15) & 0xEFC60000U;
            return (y ^ (y >> 18));
        }

        private void SeedMT(ulong _seed)
        {
            ulong x = (_seed | 1U) & 0xFFFFFFFFU;
            int j = N;

            for (j = N; j >= 0; j--)
            {
                state[j] = (x *= 69069U) & 0xFFFFFFFFU;
            }
            next = 0;
        }

        public int RandomRange(int lo, int hi)
        {
            return (Math.Abs((int)RandomInt() % (hi - lo + 1)) + lo);
        }

        public int RollDice(int face, int number_of_dice)
        {
            int roll = 0;
            for (int loop = 0; loop < number_of_dice; loop++)
            {
                roll += (RandomRange(1, face));
            }
            return roll;
        }

        public int HeadsOrTails() { return ((int)(RandomInt()) % 2); }

        public int D6(int die_count) { return RollDice(6, die_count); }
        public int D8(int die_count) { return RollDice(8, die_count); }
        public int D10(int die_count) { return RollDice(10, die_count); }
        public int D12(int die_count) { return RollDice(12, die_count); }
        public int D20(int die_count) { return RollDice(20, die_count); }
        public int D25(int die_count) { return RollDice(25, die_count); }


        private ulong ReloadMT()
        {
            ulong[] p0 = state;
            int p0pos = 0;
            ulong[] p2 = state;
            int p2pos = 2;
            ulong[] pM = state;
            int pMpos = M;
            ulong s0;
            ulong s1;

            int j;

            if ((next + 1) > N)
                SeedMT(seedValue);

            for (s0 = state[0], s1 = state[1], j = N - M + 1; --j > 0; s0 = s1, s1 = p2[p2pos++])
                p0[p0pos++] = pM[pMpos++] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);


            for (pM[0] = state[0], pMpos = 0, j = M; --j > 0; s0 = s1, s1 = p2[p2pos++])
                p0[p0pos++] = pM[pMpos++] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);


            s1 = state[0];
            p0[p0pos] = pM[pMpos] ^ (mixBits(s0, s1) >> 1) ^ (loBit(s1) != 0 ? K : 0U);
            s1 ^= (s1 >> 11);
            s1 ^= (s1 << 7) & 0x9D2C5680U;
            s1 ^= (s1 << 15) & 0xEFC60000U;
            return (s1 ^ (s1 >> 18));
        }

        private ulong hiBit(ulong _u)
        {
            return ((_u) & 0x80000000U);
        }
        private ulong loBit(ulong _u)
        {
            return ((_u) & 0x00000001U);
        }
        private ulong loBits(ulong _u)
        {
            return ((_u) & 0x7FFFFFFFU);
        }
        private ulong mixBits(ulong _u, ulong _v)
        {
            return (hiBit(_u) | loBits(_v));

        }
    }

    [Serializable]
    //Helper class for creating pay table
    public class PayTable
    {
        public int reelsCount;
        public List<PayLine> payLines;
        public void Rebuild()
        {
            if (payLines != null)
            {
                foreach (var item in payLines)
                {
                    if (item != null)
                    {
                        item.RebuildLine();
                    }
                }
            }
        }
    }

    [Serializable]
    public class PayLine
    {
        private const int maxLength = 5;
        public int[] line;
        public int pay;
        public int freeSpins;
        public bool showEvent = false;
        public UnityEvent LineEvent;
        [Tooltip("Payouts multiplier, default value = 1")]
        public int payMult = 1; // payout multiplier
        [Tooltip("Free Spins multiplier, default value = 1")]
        public int freeSpinsMult = 1; // payout multiplier

        bool useWildInFirstPosition = false;

        public PayLine()
        {
            line = new int[maxLength];
            for (int i = 0; i < line.Length; i++)
            {
                line[i] = -1;
            }
        }

        public PayLine(PayLine pLine)
        {
            if (pLine.line != null)
            {
                line = pLine.line;
                RebuildLine();
                pay = pLine.pay;
                freeSpins = pLine.freeSpins;
                LineEvent = pLine.LineEvent;
                payMult = pLine.payMult;
            }
            else
            {
                RebuildLine();
            }
        }

        public PayLine(int[] newLine, int pay, int freeSpins)
        {
            if (newLine != null)
            {
                this.line = newLine;
                this.pay = pay;
                this.freeSpins = freeSpins;
            }
            RebuildLine();
        }

        public string ToString(Sprite[] sprites, int length)
        {
            string res = "";
            if (line == null) return res;
            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] >= 0)
                        res += sprites[line[i]].name;
                    else
                    {
                        res += "any";
                    }
                    if (i < line.Length - 1) res += ";";
                }
            }
            return res;
        }

        public string[] Names(SlotIcon[] sprites, int length)
        {
            if (line == null) return null;
            List<string> res = new List<string>();
            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] >= 0)
                        res.Add((sprites[line[i]] != null && sprites[line[i]].iconSprite != null) ? sprites[line[i]].iconSprite.name : "failed");
                    else
                    {
                        res.Add("any");
                    }
                }
            }
            return res.ToArray();
        }

        public float GetPayOutProb(SlotController sC)
        {
            return GetProbability(sC) * 100f * pay;
        }

        public float GetProbability(SlotController sC)
        {
            float res = 0;
            if (!sC) return res;
            if (line == null || sC.slotGroupsBeh == null || sC.slotGroupsBeh.Length > line.Length) return res;
            float[] rP = sC.slotGroupsBeh[0].GetReelSymbHitPropabilities(sC.slotIcons);

            //avoid "any" symbol error in first position
            res = (line[0] >= 0) ? rP[line[0]] : 1; //  res = rP[line[0]];

            for (int i = 1; i < sC.slotGroupsBeh.Length; i++)
            {
                if (line[i] >= 0) // any.ID = -1
                {
                    rP = sC.slotGroupsBeh[i].GetReelSymbHitPropabilities(sC.slotIcons);
                    res *= rP[line[i]];
                }
                else
                {
                    // break;
                }
            }
            return res;
        }

        /// <summary>
        /// Create and return additional lines for this line with wild symbol,  only if symbol can be substitute with wild
        /// </summary>
        /// <returns></returns>
        public List<PayLine> GetWildLines(SlotController sC)
        {
            int workLength = sC.slotGroupsBeh.Length;
            List<PayLine> res = new List<PayLine>();
            if (!sC) return res; // return empty list
            if (!sC.useWild) return res; // return empty list

            int wild_id = sC.wild_id;
            useWildInFirstPosition = sC.useWildInFirstPosition;
            List<int> wPoss = GetPositionsForWild(wild_id, sC);
            int maxWildsCount = (useWildInFirstPosition) ? wPoss.Count - 1 : wPoss.Count;
            int minWildsCount = 1;
            ComboCounter cC = new ComboCounter(wPoss);
            while (cC.NextCombo())
            {
                List<int> combo = cC.combo; // 
                int comboSum = combo.Sum(); // count of wilds in combo

                if (comboSum >= minWildsCount && comboSum <= maxWildsCount)
                {
                    PayLine p = new PayLine(this);
                    for (int i = 0; i < wPoss.Count; i++)
                    {
                        int pos = wPoss[i];
                        if (combo[i] == 1)
                        {
                            p.line[pos] = wild_id;
                        }
                    }
                    if (!p.IsEqual(this, workLength) && !ContainEqualLine(res, p, workLength)) res.Add(p);
                }
            }

            return res;
        }

        private bool IsEqual(PayLine pLine, int workLength)
        {
            if (pLine == null) return false;
            if (pLine.line == null) return false;
            if (line.Length != pLine.line.Length) return false;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != pLine.line[i]) return false;
            }
            return true;
        }

        private bool ContainEqualLine(List<PayLine> pList, PayLine pLine, int workLength)
        {
            if (pList == null) return false;
            if (pLine == null) return false;
            if (pLine.line == null) return false;

            foreach (var item in pList)
            {
                if (item.IsEqual(pLine, workLength)) return true;
            }
            return false;
        }

        /// <summary>
        /// return list position on line for wild symbols (0 - line.length -1)  
        /// </summary>
        /// <param name="wild_id"></param>
        /// <param name="sC"></param>
        /// <returns></returns>
        private List<int> GetPositionsForWild(int wild_id, SlotController sC)
        {
            List<int> wPoss = new List<int>();
            int counter = 0;
            int length = sC.slotGroupsBeh.Length;

            for (int i = 0; i < line.Length; i++)
            {
                if (i < length)
                {
                    if (line[i] != -1 && line[i] != wild_id)
                    {
                        if (!useWildInFirstPosition && counter == 0) // don't use first
                        {
                            counter++;
                        }
                        else
                        {
                            if (sC.slotIcons[line[i]].useWildSubstitute) wPoss.Add(i);
                            counter++;
                        }
                    }
                }
            }
            return wPoss;
        }

        public void RebuildLine()
        {
          // if (line.Length == maxLength) return;
            int[] lineT = new int[maxLength];
            for (int i = 0; i < maxLength; i++)
            {
                if (line != null && i < line.Length) lineT[i] = line[i];
                else lineT[i] = -1;
            }
            line = lineT;
        }

        public void ClampLine(int workLength)
        {
            RebuildLine();
            for (int i = 0; i < maxLength; i++)
            {
                if (i >= workLength) line[i] = -1;
            }
        }

        
    }

    [Serializable]
    public class ScatterPay
    {
        public int scattersCount;
        public int pay;
        public int freeSpins;
        public int payMult = 1;
        public int freeSpinsMult = 1;
        public UnityEvent WinEvent;

        public ScatterPay()
        {
            payMult = 1;
            freeSpinsMult = 1;
            scattersCount = 3;
            pay = 0;
            freeSpins = 0;
        }
    }

    static class ClassExt
    {
        public enum FieldAllign { Left, Right, Center}

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this float fNumber, string format, int field)
        {
            string form = "{0," + field.ToString() +":"+ format + "}";
            string res = String.Format(form, fNumber);
            return res;
        }

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this string s, int field)
        {
            string form = "{0," + field.ToString() +"}";
            string res = String.Format(form, s);
            return res;
        }

        /// <summary>
        /// Return formatted string; (F2, N5, e, r, p, X, D12, C)
        /// </summary>
        /// <param name="fNumber"></param>
        /// <param name="format"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string ToString(this string s, int field, FieldAllign fAllign)
        {
            int length = s.Length;
            if (length >= field)
            {
                string form = "{0," + field.ToString() + "}";
                return String.Format(form, s);
            }
            else
            {
                if (fAllign == FieldAllign.Center)
                {
                    int lCount = (field - length) / 2;
                    int rCount = field - length - lCount;
                    string lSp = new string('*', lCount);
                    string rSp = new string('*', rCount);
                    return (lSp + s + rSp);
                }
                else if (fAllign == FieldAllign.Left)
                {
                    int lCount = (field - length);
                    string lSp = new string('*', lCount);
                    return (s+lSp);
                }
                else
                {
                    string form = "{0," + field.ToString() + "}";
                    return  String.Format(form, s);
                }
            }
        }

        private static string ToStrings<T>(T[] a)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString();
                res += " ";
            }
            return res;
        }

        private static string ToStrings(float[] a, string format, int field)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString(format, field);
                res += " ";
            }
            return res;
        }

        private static string ToStrings(string[] a, int field, ClassExt.FieldAllign allign)
        {
            string res = "";
            for (int i = 0; i < a.Length; i++)
            {
                res += a[i].ToString(field, allign);
                res += " ";
            }
            return res;
        }

        private static float[] Mul(float[] a, float[] b)
        {
            if (a.Length != b.Length) return null;
            float[] res = new float[a.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = a[i] * b[i];
            }
            return res;
        }

    }

    /// <summary>
    /// Helper class to make combinations from symbols with wild
    /// </summary>
    public class ComboCounter
    {
        public List<int> combo;
        public List<int> positions;

        List<byte> counterSizes;

        public ComboCounter(List<int> positions)
        {
            this.positions = positions;
            counterSizes = GetComboCountsForSymbols();
            combo = new List<int>(counterSizes.Count);

            for (int i = 0; i < counterSizes.Count; i++) // create in counter first combination
            {
                combo.Add(0);
            }
        }

        /// <summary>
        /// get list with counts of combinations for each position
        /// </summary>
        /// <returns></returns>
        private List<byte> GetComboCountsForSymbols()
        {
            List<byte> res = new List<byte>();
            foreach (var item in positions)
            {
                res.Add((byte)(1)); // wild or symbol (0 or 1)
            }
            return res;
        }

        private bool Next()
        {
            for (int i = counterSizes.Count - 1; i >= 0; i--)
            {
                if (combo[i] < counterSizes[i])
                {
                    combo[i]++;
                    if (i != counterSizes.Count - 1) // reset low "bytes"
                    {
                        for (int j = i + 1; j < counterSizes.Count; j++)
                        {
                            combo[j] = 0;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool NextCombo()
        {
            if (Next())
            {
                return true;
            }
            return false;
        }
    }

    public class ComboCounterT
    {
        public List<int> combo; // combination with 5 numbers
        public List<int> positions;

        List<byte> counterSizes;

        public ComboCounterT(SlotGroupBehavior[] sgb)
        {
            counterSizes = new List<byte>();
            for (int i = 0; i < sgb.Length; i++)
            {
                counterSizes.Add((byte)sgb[i].triples.Count);
            }

            combo = new List<int>(counterSizes.Count);

            for (int i = 0; i < counterSizes.Count; i++) // create in counter first combination
            {
                combo.Add(0);
            }
        }

        private bool Next()
        {
            for (int i = counterSizes.Count - 1; i >= 0; i--)
            {
                if (combo[i] < counterSizes[i] - 1)
                {
                    combo[i]++;
                    if (i != counterSizes.Count - 1) // reset low "bytes"
                    {
                        for (int j = i + 1; j < counterSizes.Count; j++)
                        {
                            combo[j] = 0;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool NextCombo()
        {
            if (Next())
            {
                return true;
            }
            return false;
        }
    }
}

