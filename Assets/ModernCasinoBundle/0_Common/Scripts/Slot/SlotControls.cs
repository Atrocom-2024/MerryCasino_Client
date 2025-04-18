﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mkey
{
    public class SlotControls : MonoBehaviour
    {
        public static SlotControls Instance { get; private set; }
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private GuiController MGUI { get { return GuiController.Instance; } }

        #region main references
        [SerializeField]
        private WarningMessController megaJackPotWinPuPrefab;
        [SerializeField]
        private WarningMessController miniJackPotWinPuPrefab;
        [SerializeField]
        private WarningMessController maxiJackPotWinPuPrefab;

        [SerializeField]
        private GameObject megaJackPotWinPrefab;
        [SerializeField]
        private GameObject maxiJackPotWinPrefab;
        [SerializeField]
        private GameObject miniJackPotWinPrefab;
        [SerializeField]
        private JackPotController jackPotController;
        #endregion main references

        #region default
        [Space(8)]
        [Header("Jackpot coins")]
        [Tooltip("Mini jackpot sum start value")]
        //[SerializeField]
        private int miniStart = 10;

        [Tooltip("Maxi jackpot sum start value")]
        //[SerializeField]
        private int maxiStart = 20;

        [Tooltip("Mega jackpot sum start value")]
        //[SerializeField]
        //private int megaStart = 1000;
        private decimal megaProbStart = 0.00M;

        [Space(8)]
        [Tooltip("Check if you want to save coins, level, progress, facebook gift flag, sound settings")]
        [SerializeField]
        private bool saveData = false;

        [Tooltip("Default max line bet, min =1")]
        [SerializeField]
        private int maxLineBet = 20;

        [Tooltip("Default line bet at start, min = 1")]
        [SerializeField]
        private int defLineBet = 1;

        [Tooltip("Default max bet line num, min =1")]
        [SerializeField]
        public int betLineNum = 1;

        [Tooltip("Check if you want to play auto all free spins")]
        [SerializeField]
        private bool autoPlayFreeSpins = true;

        [Tooltip("Default auto spins count, min = 1")]
        [SerializeField]
        private int defAutoSpins = 1;

        [Tooltip("Max value of auto spins, min = 1")]
        [SerializeField]
        private int maxAutoSpins = int.MaxValue;
        #endregion default

        #region output
        [Space(16, order = 0)]
        [SerializeField]
        private Text LineBetSumText;
        [SerializeField]
        private Text TotalBetSumText;
        [SerializeField]
        private Text LinesCountText;
        [SerializeField]
        private Text FreeSpinText;
        [SerializeField]
        private Text FreeSpinCountText;
        [SerializeField]
        private Text AutoSpinsCountText;
        [SerializeField]
        public Text InfoText;
        [SerializeField]
        public Text WinAmountText;
        [Space(8)]
        [SerializeField]
        private Text MegaJackpotAmountText;
        [SerializeField]
        private Text MiniJackpotAmountText;
        [SerializeField]
        private Text MaxiJackpotAmountText;
        [SerializeField]
        private TextMesh MegaJackpotAmountTextMesh;
        [SerializeField]
        private TextMesh MiniJackpotAmountTextMesh;
        [SerializeField]
        private TextMesh MaxiJackpotAmountTextMesh;
        [SerializeField]
        private Text MegaJackpotProbText;
        #endregion output

        [SerializeField]
        private Button spinButton;
        [SerializeField]
        private Button autoSpinButton;

        #region features
        [SerializeField]
        private HoldFeature hold;
        #endregion features

        #region keys
        private static string Prefix { get { return SceneLoader.GetCurrentSceneName() + SceneLoader.GetCurrentSceneBuildIndex(); } }
        private static string SaveMiniJackPotKey { get { return Prefix + "_mk_slot_minijackpot"; } }// current  mini jackpot
        private static string SaveMaxiJackPotKey { get { return Prefix + "_mk_slot_maxijackpot"; } } // current  maxi jackpot
        private static string SaveMegaJackPotKey { get { return Prefix + "_mk_slot_megajackpot"; } } // current  mega jackpot
        private static string SaveAutoSpinsKey { get { return Prefix + "_mk_slot_autospins"; } } // current auto spins
        #endregion keys

        #region temp vars
        private float levelxp;
        private float oldLevelxp;
        private int levelTweenId;
        private SceneButton[] buttons;
        private TweenIntValue balanceTween;
        private TweenIntValue winCoinsTween;
        private TweenIntValue infoCoinsTween;
        private WarningMessController megaJackPotWinPu;
        private WarningMessController miniJackPotWinPu;
        private WarningMessController maxiJackPotWinPu;
        private GameObject megaJackPotWinGO;
        private GameObject maxiJackPotWinGO;
        private GameObject miniJackPotWinGO;
        private string coinsFormat =  "0,0";
        private SpinButtonBehavior spinButtonBehavior;
        #endregion temp vars

        #region references
        [SerializeField]
        private SlotController slot;
        [SerializeField]
        private LinesController linesController;
        #endregion references

        #region events
        public Action<int> ChangeMiniJackPotEvent;
        public Action<int> ChangeMaxiJackPotEvent;
        public Action<decimal> ChangeMegaJackpotProbEvent;
        //public Action<int> ChangeMegaJackPotEvent;
        //public Action<int> LoadMiniJackPotEvent;
        //public Action<int> LoadMaxiJackPotEvent;
        //public Action<int> LoadMegaJackPotEvent;

        public Action<int> ChangeTotalBetEvent;
        public Action<int> ChangeLineBetEvent;
        public Action<int, bool> ChangeSelectedLinesEvent;

        public Action<int> ChangeFreeSpinsEvent;
        public Action<int> ChangeAutoSpinsEvent;
        public Action<int, int> ChangeAutoSpinsCounterEvent; // 
        public Action<bool> ChangeAutoStateEvent;
        #endregion events

        #region properties
        //public bool SaveData
        //{
        //    get { return saveData; }
        //}

        public int MiniJackPotStart
        {
            get { return miniStart; }
        }

        public int MaxiJackPotStart
        {
            get { return maxiStart; }
        }

        public decimal MegaJackPotProbStart
        {
            get { return megaProbStart; }
        }

        //public long MegaJackPotStart
        //{
        //    get { return megaStart; }
        //}

        public int LineBet
        {
            get; private set;
        }

        public int TotalBet
        {
            get { return LineBet * SelectedLinesCount * HoldMultipler; }
        }

        public int HoldMultipler
        {
            get { return (hold && hold.enabled && hold.gameObject.activeSelf) ? hold.GetMultiplier() : 1; }
        }

        public int SelectedLinesCount
        {
            get; private set;
        }

        public bool AnyLineSelected
        {
            get { return SelectedLinesCount > 0; }
        }

        public int FreeSpins
        {
            get; private set;
        }

        public bool HasFreeSpin
        {
            get { return FreeSpins > 0; }
        }

        public bool AutoPlayFreeSpins
        {
            get { return autoPlayFreeSpins; }
        }

        public bool Auto { get; private set; }

        public int AutoSpinsCounter { get; private set; }

        public HoldFeature Hold { get { return hold; } }

        public bool UseHold
        {
            get { return (hold && hold.enabled && hold.gameObject.activeSelf); }
        }
        #endregion properties

        #region saved properties
        public int MiniJackPot
        {
            get; private set;
        }

        public int MaxiJackPot
        {
            get; private set;
        }

        public int MegaJackPot
        {
            get; private set;
        }

        public decimal MegaJackPotProb
        {
            get; private set;
        }

        public int AutoSpinCount
        {
            get; private set;
        }
        #endregion saved properties

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
        #region regular
        private IEnumerator Start()
        {
            while (!MPlayer)
            {
                yield return new WaitForEndOfFrame();
            }

            if (spinButton) spinButtonBehavior = spinButton.GetComponent<SpinButtonBehavior>();
            if (spinButtonBehavior)
            {
                spinButtonBehavior.TrySetAutoEvent += ((auto) =>
                  {
                      if (Auto) { ResetAutoSpinsMode(); return; }

                      if (auto)
                      {
                          SetAutoSpinsCounter(0);
                          Auto = true;
                          slot.SpinPress();
                          ChangeAutoStateEvent?.Invoke(true);
                      }
                  });

                spinButtonBehavior.ClickEvent += () => { Spin_Click(); };
            }

            buttons = GetComponentsInChildren<SceneButton>();

            // set player event handlers
            ChangeFreeSpinsEvent += ChangeFreeSpinsHandler;
            ChangeAutoSpinsEvent += ChangeAutoSpinsHandler;
            ChangeTotalBetEvent += ChangeTotalBetHandler;
            ChangeLineBetEvent += ChangeLineBetHandler;
            ChangeSelectedLinesEvent += ChangeSelectedLinesHandler;

            MPlayer.ChangeWinCoinsEvent += ChangeWinCoinsHandler;

            ChangeMiniJackPotEvent += ChangeMiniJackPotHandler;
            ChangeMaxiJackPotEvent += ChangeMaxiJackPotHandler;
            ChangeMegaJackpotProbEvent += ChangeMegaJackPotProbHandler;


            LoadLineBet();
            if (hold) hold.ChangeBetMultiplierEvent += (hm) => { RefreshBetLines(); };
            if (WinAmountText) winCoinsTween = new TweenIntValue(WinAmountText.gameObject, 0, 0.5f, 2, true, (w) => { if (this && WinAmountText) WinAmountText.text = (w > 0) ? w.ToString(coinsFormat) : "0"; });
            if (InfoText) infoCoinsTween = new TweenIntValue(InfoText.gameObject, 0, 0.5f, 2, true, (w) => { if (this) SetTextString(InfoText, (w > 0) ? w.ToString(coinsFormat) : "0"); });

            AutoSpinsCounter = 0;
            ChangeAutoSpinsCounterEvent += (r, i) => { if (this && AutoSpinsCountText) AutoSpinsCountText.text = i.ToString(); };
            LoadFreeSpins();
            LoadAutoSpins();
            //SetTextString(InfoText, (SelectedLinesCount > 0) ? "Click to SPIN to start!" : "Select any slot line!");
            //ChangeSelectedLinesEvent += (l, b) => { SetTextString(InfoText, (l > 0) ? "Click to SPIN to start!" : "Select any slot line!"); };
            SetTextString(InfoText, (SelectedLinesCount > 0) ? "Feeling lucky?" : "Select any slot line!");
            ChangeSelectedLinesEvent += (l, b) => { SetTextString(InfoText, (l > 0) ? "Feeling lucky?" : "Select any slot line!"); };
            Refresh();

            //LoadMiniJackPot();
            //LoadMaxiJackPot();
            LoadMegaJackPotProb();
            Debug.Log($"MegaStart: {megaProbStart}");
        }

        void OnDestroy()
        {
            ChangeTotalBetEvent -= ChangeTotalBetHandler;
            ChangeLineBetEvent -= ChangeLineBetHandler;
            ChangeSelectedLinesEvent -= ChangeSelectedLinesHandler;

            // remove player event handlers
            if (MPlayer)
            {
                MPlayer.ChangeWinCoinsEvent -= ChangeWinCoinsHandler;
            }
        }

        private void OnValidate()
        {
            miniStart = Math.Max(0, miniStart);
            maxiStart = Math.Max(0, maxiStart);
            megaProbStart = Math.Max(0, megaProbStart);

            maxLineBet = Math.Max(1, maxLineBet);
            defLineBet = Math.Max(1, defLineBet);
            defLineBet = Mathf.Min(defLineBet, maxLineBet);

            maxAutoSpins = Math.Max(1, maxAutoSpins);
            defAutoSpins = Math.Max(1, defAutoSpins);
            defAutoSpins = Math.Min(defAutoSpins, maxAutoSpins);
        }
        #endregion regular

        /// <summary>
        /// Set all buttons interactble = activity, but startButton = startButtonAcivity
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="startButtonAcivity"></param>
        public void SetControlActivity(bool activity, bool startButtonAcivity)
        {
            if (buttons != null)
            {
                foreach (SceneButton b in buttons)
                {
                    if (b) b.interactable = activity;
                }
            }
            if (spinButton) spinButton.interactable = startButtonAcivity;
            if (linesController) { linesController.SetControlActivity(activity); }

        }

        #region refresh
        /// <summary>
        /// Refresh gui data : Balance,  BetCount, freeSpin
        /// </summary>
        private void Refresh()
        {
            RefreshJackPots();
            RefreshBetLines();
            RefreshSpins();
            if (WinAmountText) WinAmountText.text = 0.ToString();
        }

        /// <summary>
        /// Refresh gui lines, bet
        /// </summary>
        private void RefreshBetLines()
        {
            if (MPlayer)
            {
                if (LineBetSumText) LineBetSumText.text = LineBet.ToString();
                if (TotalBetSumText) TotalBetSumText.text = TotalBet >= 10 ? TotalBet.ToString(coinsFormat) : TotalBet.ToString();
                if (LinesCountText) LinesCountText.text = SelectedLinesCount.ToString();
            }
        }

        /// <summary>
        /// Refresh gui spins
        /// </summary>
        private void RefreshSpins()
        {
            if (AutoSpinsCountText) AutoSpinsCountText.text = AutoSpinCount.ToString();
            if (FreeSpinText) FreeSpinText.text = (FreeSpins > 0) ? "Free" : "";
            if (FreeSpinCountText) FreeSpinCountText.text = (FreeSpins > 0) ? FreeSpins.ToString() : "";
        }

        private void RefreshJackPots()
        {
            if (this && MiniJackpotAmountText) MiniJackpotAmountText.text = MiniJackPot.ToString(coinsFormat);
            if (this && MaxiJackpotAmountText) MaxiJackpotAmountText.text = MaxiJackPot.ToString(coinsFormat);
            if (this && MegaJackpotAmountText) MegaJackpotAmountText.text = MegaJackPot.ToString(coinsFormat);

            // if use text mesh output
            if (this && MiniJackpotAmountTextMesh) MiniJackpotAmountTextMesh.text = MiniJackPot.ToString(coinsFormat);
            if (this && MaxiJackpotAmountTextMesh) MaxiJackpotAmountTextMesh.text = MaxiJackPot.ToString(coinsFormat);
            if (this && MegaJackpotAmountTextMesh) MegaJackpotAmountTextMesh.text = MegaJackPot.ToString(coinsFormat);
        }
        #endregion refresh

        #region control buttons
        public void LinesPlus_Click()
        {
            AddSelectedLinesCount(1, true);
        }

        public void LinesMinus_Click()
        {
            AddSelectedLinesCount(-1, false);
        }

        public void LineBetPlus_Click()
        {
            if (LineBet == 1 && betLineNum != 1)
            {
                SetLineBet(betLineNum); // 처음 증가 시 5로 설정
            }
            else
            {
                AddLineBet(1 * betLineNum); // 이후부터는 5씩 증가
            }
            //AddLineBet(1 * betLineNum);
        }

        public void LineBetMinus_Click()
        {
            if (LineBet == betLineNum)
            {
                SetLineBet(1); // 5에서 감소 시 1로 설정
            }
            else
            {
                AddLineBet(-1 * betLineNum); // 이후부터는 5씩 감소
            }
            //AddLineBet(-1 * betLineNum);
        }

        public void AutoSpinPlus_Click()
        {
            AddAutoSpins(5);
        }

        public void AutoSpinMinus_Click()
        {
            AddAutoSpins(-5);
        }

        public void MaxBet_Click()
        {
            linesController.SelectAllLines(true);
            SetMaxLineBet();
        }

        /// <summary>
        /// 사용자가 Spin 버튼을 눌렀을 때 실행되는 메서드
        /// </summary>
        public void Spin_Click()
        {
            if (Auto) { ResetAutoSpinsMode(); return; }
            slot.SpinPress();
        }

        public void AutoSpin_Click()
        {
            if (Auto) { ResetAutoSpinsMode(); return; }
            SetAutoSpinsCounter(0);
            Auto = true;
            slot.SpinPress();
            if (autoSpinButton) autoSpinButton.SetPressed();
            ChangeAutoStateEvent?.Invoke(true);
        }
        #endregion control buttons

        #region event handlers
        private void ChangeFreeSpinsHandler(int newFreeSpinsCount)
        {
            if (this)
            {
                if (FreeSpinText) FreeSpinText.text = (FreeSpins > 0) ? "Free" : "";
                if (FreeSpinCountText) FreeSpinCountText.text = (newFreeSpinsCount > 0) ? newFreeSpinsCount.ToString() : "";
            }
        }

        private void ChangeAutoSpinsHandler(int newAutoSpinsCount)
        {
            if (this && AutoSpinsCountText) AutoSpinsCountText.text = newAutoSpinsCount.ToString();
        }

        private void ChangeTotalBetHandler(int newTotalBet)
        {
            if (this && TotalBetSumText)
                TotalBetSumText.text = TotalBet >= 10 ? TotalBet.ToString(coinsFormat) : TotalBet.ToString();
            if (this && MegaJackpotAmountText)
                MegaJackpotAmountText.text = (TotalBet * 100).ToString(coinsFormat);
            if (this && MegaJackpotAmountTextMesh)
                MegaJackpotAmountTextMesh.text = (TotalBet * 100).ToString(coinsFormat);
        }

        private void ChangeLineBetHandler(int newLineBet)
        {
            if (this && LineBetSumText) LineBetSumText.text = newLineBet.ToString();

        }

        private void ChangeSelectedLinesHandler(int newCount, bool burn)
        {
            if (this && LinesCountText) LinesCountText.text = newCount.ToString();
       }

        private void ChangeMiniJackPotHandler(int newCount)
        {
            if (this && MiniJackpotAmountText) MiniJackpotAmountText.text = newCount.ToString(coinsFormat);
            if (this && MiniJackpotAmountTextMesh) MiniJackpotAmountTextMesh.text = newCount.ToString(coinsFormat);
        }

        private void ChangeMaxiJackPotHandler(int newCount)
        {
            if (this && MaxiJackpotAmountText) MaxiJackpotAmountText.text = newCount.ToString(coinsFormat);
            if (this && MaxiJackpotAmountTextMesh) MaxiJackpotAmountTextMesh.text = newCount.ToString(coinsFormat);
        }

        //private void ChangeMegaJackPotHandler(int newCount)
        //{
        //    if (this && MegaJackpotAmountText) MegaJackpotAmountText.text = newCount.ToString(coinsFormat);
        //    if (this && MegaJackpotAmountTextMesh) MegaJackpotAmountTextMesh.text = newCount.ToString(coinsFormat);
        //}

        private void ChangeMegaJackPotProbHandler(decimal newJackpotProb)
        {
            var percent = newJackpotProb * 100;
            if (this && MegaJackpotProbText)
                MegaJackpotProbText.text = percent.ToString("F3") + "%";
        }

        private void ChangeMegaJackPotAmountHandler(int newCount)
        {
            if (this && MegaJackpotAmountText)
                MegaJackpotAmountText.text = newCount.ToString(coinsFormat);
            if (this && MegaJackpotAmountTextMesh)
                MegaJackpotAmountTextMesh.text = newCount.ToString(coinsFormat);
        }

        private void ChangeWinCoinsHandler(int newCount)
        {
            if (winCoinsTween != null) winCoinsTween.Tween(newCount, 100);
            if (infoCoinsTween != null) infoCoinsTween.Tween(newCount, 100);
        }
        #endregion event handlers

        #region mini jackpot
        /// <summary>
        /// Add mini jack pot and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddMiniJackPot(int count)
        {
            SetMiniJackPotCount(MiniJackPot + count);
        }

        /// <summary>
        /// Set mini jackpot and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetMiniJackPotCount(int count)
        {
            count = Mathf.Max(miniStart, count);
            MiniJackPot = count;
            ChangeMiniJackPotEvent?.Invoke(MiniJackPot);
        }

        /// <summary>
        /// Load serialized mini jackpot or set defaults
        /// </summary>
        private void LoadMiniJackPot()
        {
            SetMiniJackPotCount(miniStart);
        }
        #endregion mini jackpot

        #region maxi jackpot
        /// <summary>
        /// Add maxi jack pot and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddMaxiJackPot(int count)
        {
            SetMaxiJackPotCount(MaxiJackPot + count);
        }

        /// <summary>
        /// Set maxi jackpot and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetMaxiJackPotCount(int count)
        {
            count = Mathf.Max(maxiStart, count);
            MaxiJackPot = count;
            ChangeMaxiJackPotEvent?.Invoke(MaxiJackPot);
        }

        /// <summary>
        /// Load serialized maxi jackpot or set defaults
        /// </summary>
        private void LoadMaxiJackPot()
        {
            SetMaxiJackPotCount(maxiStart);
        }
        #endregion maxi jackpot

        #region mega jackpot
        /// <summary>
        /// Add mega jack pot and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddMegaJackPot(int count)
        {
            SetMegaJackPotCount(MegaJackPot + count);
        }

        /// <summary>
        /// Set mega jackpot and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetMegaJackPotCount(int count)
        {
            count = Mathf.Max(0, count);
            //count = Mathf.Max(megaStart, count);
            MegaJackPot = count;
            ChangeMegaJackpotProbEvent?.Invoke(MegaJackPot);
        }

        /// <summary>
        /// Load serialized mega jackpot or set defaults
        /// </summary>
        private void LoadMegaJackPotProb()
        {
            //SetMegaJackPotCount(megaStart);
            SetMegaJackPotProb(megaProbStart);
        }

        public void SetMegaJackpotProbStart(decimal jackpotProb)
        {
            megaProbStart = jackpotProb;
        }
        #endregion mega jackpot

        #region common jackpot
        //public void SetJackPotCount(int count, JackPotType jackPotType)
        //{
        //    switch (jackPotType)
        //    {
        //        case JackPotType.Mini:
        //            SetMiniJackPotCount(count);
        //            break;
        //        case JackPotType.Maxi:
        //            SetMaxiJackPotCount(count);
        //            break;
        //        case JackPotType.Mega:
        //            SetMegaJackPotCount(count);
        //            break;
        //    }
        //}
        public void SetJackPotProb(decimal jackpotProb, JackPotType jackPotType)
        {
            switch (jackPotType)
            {
                case JackPotType.Mini:
                    SetMiniJackPotProb(jackpotProb);
                    break;
                case JackPotType.Maxi:
                    SetMaxiJackPotProb(jackpotProb);
                    break;
                case JackPotType.Mega:
                    SetMegaJackPotProb(jackpotProb);
                    break;
            }
        }

        public void SetMiniJackPotProb(decimal count)
        {
            ChangeMegaJackpotProbEvent?.Invoke(count);
        }
        
        public void SetMaxiJackPotProb(decimal count)
        {
            ChangeMegaJackpotProbEvent?.Invoke(count);
        }
        
        public void SetMegaJackPotProb(decimal jackpotProb)
        {
            MegaJackPotProb = jackpotProb;
            ChangeMegaJackpotProbEvent?.Invoke(jackpotProb);
        }

        //public int GetJackPotCoins(JackPotType jackPotType)
        //{
        //    int jackPotCoins = 0;
        //    switch (jackPotType)
        //    {
        //        case JackPotType.Mini:
        //            jackPotCoins = MiniJackPot;
        //            break;
        //        case JackPotType.Maxi:
        //            jackPotCoins = MaxiJackPot;
        //            break;
        //        case JackPotType.Mega:
        //            jackPotCoins = MegaJackPot;
        //            break;
        //    }
        //    return jackPotCoins;
        //}
        
        public int GetJackPotCoins(JackPotType jackPotType)
        {
            int jackPotCoins = 0;
            switch (jackPotType)
            {
                case JackPotType.Mini:
                    jackPotCoins = MiniJackPot;
                    break;
                case JackPotType.Maxi:
                    jackPotCoins = MaxiJackPot;
                    break;
                case JackPotType.Mega:
                    jackPotCoins = TotalBet * 100;
                    break;
            }
            return jackPotCoins;
        }

        internal void JPWinCancel()
        {
            if (megaJackPotWinPu) megaJackPotWinPu.CloseWindow();
            if (maxiJackPotWinPu) maxiJackPotWinPu.CloseWindow();
            if (miniJackPotWinPu) miniJackPotWinPu.CloseWindow();

            if (megaJackPotWinGO) Destroy(megaJackPotWinGO);
            if (maxiJackPotWinGO) Destroy(maxiJackPotWinGO);
            if (miniJackPotWinGO) Destroy(miniJackPotWinGO);
        }

        /// <summary>
        /// 잭팟 UI 처리
        /// </summary>
        /// <param name="jackPotCoins"></param>
        /// <param name="jackPotType"></param>
        internal void JPWinShow(int jackPotCoins, JackPotType jackPotType)
        {
            switch (jackPotType)
            {
                case JackPotType.None:
                    break;
                case JackPotType.Mini:
                    if (miniJackPotWinPrefab && jackPotController) miniJackPotWinGO = Instantiate(miniJackPotWinPrefab, jackPotController.transform);
                    if (miniJackPotWinPuPrefab) miniJackPotWinPu = MGUI.ShowMessage(miniJackPotWinPuPrefab, "", jackPotCoins.ToString(), 5f, null);
                    break;
                case JackPotType.Maxi:
                    if (maxiJackPotWinPrefab && jackPotController) maxiJackPotWinGO = Instantiate(maxiJackPotWinPrefab, jackPotController.transform);
                    if (maxiJackPotWinPuPrefab) maxiJackPotWinPu = MGUI.ShowMessage(maxiJackPotWinPuPrefab, "", jackPotCoins.ToString(), 5f, null);
                    break;
                case JackPotType.Mega:
                    if (megaJackPotWinPrefab && jackPotController) megaJackPotWinGO = Instantiate(megaJackPotWinPrefab, jackPotController.transform);
                    if (megaJackPotWinPuPrefab) megaJackPotWinPu = MGUI.ShowMessage(megaJackPotWinPuPrefab, "", jackPotCoins.ToString(), 5f, null);
                    break;
                default:
                    break;
            }
        }
        #endregion common jackpot

        #region LineBet
        /// <summary>
        /// Change line bet and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddLineBet(int count)
        {
            SetLineBet(LineBet + count);
        }

        /// <summary>
        /// Set line bet and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetLineBet(int count)
        {
            count = Mathf.Max(defLineBet, count);
            count = Mathf.Min(count, maxLineBet);
            bool changed = (LineBet != count);
            LineBet = count;
            if (changed)
            {
                ChangeLineBetEvent?.Invoke(LineBet);
                ChangeTotalBetEvent?.Invoke(TotalBet);
            }
        }

        /// <summary>
        /// Load default line bet
        /// </summary>
        private void LoadLineBet()
        {
            SetLineBet(defLineBet);
        }

        internal void SetMaxLineBet()
        {
            SetLineBet(maxLineBet);
        }

        /// <summary>
        /// 유저가 갖고있는 돈보다 배팅 금액이 적을 때만 배팅 진행
        /// </summary>
        /// <returns></returns>
        internal bool ApplyBet()
        {
            if (MPlayer.Coins >= TotalBet)
            {
                Debug.Log($"돈 있음: {MPlayer.Coins}");
                StartCoroutine(RoomController.Instance.HandleBetting(MPlayer.Id, TotalBet));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool CheckBet()
        {
            if (MPlayer.Coins >= TotalBet)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 유저가 갖고있는 돈보다 배팅 금액이 적을 때만 배팅 진행
        /// </summary>
        /// <returns></returns>
        //internal async Task<BetResponse> ApplyBetAsync()
        //{
        //    if (MPlayer.Coins >= TotalBet)
        //    {
        //        Debug.Log($"돈 있음: {MPlayer.Coins}");
        //        return await RoomController.Instance.HandleBettingAsync(MPlayer.Id, TotalBet);
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}


        #endregion LineBet

        #region lines
        internal void AddSelectedLinesCount(int count, bool burn)
        {
            SetSelectedLinesCount(SelectedLinesCount + count, burn);
        }

        internal void SetSelectedLinesCount(int count, bool burn)
        {
            count = Mathf.Max(1, count);
            count = Mathf.Min(linesController.LinesCount, count);

            bool changed = (SelectedLinesCount != count);
            SelectedLinesCount = count;
            if (changed)
            {
                ChangeSelectedLinesEvent?.Invoke(count, burn);
                ChangeTotalBetEvent?.Invoke(TotalBet);
            }
        }
        #endregion lines

        #region FreeSpins
        /// <summary>
        /// 프리스핀의 개수를 더하는 메서드
        /// </summary>
        /// <param name="count"></param>
        public void AddFreeSpins(int count)
        {
            SetFreeSpinsCount(FreeSpins + count);
        }

        /// <summary>
        /// 프리스핀의 수를 설정하고 저장하는 메서드
        /// </summary>
        /// <param name="count"></param>
        public void SetFreeSpinsCount(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = (FreeSpins != count);
            FreeSpins = count;
            if (changed) ChangeFreeSpinsEvent?.Invoke(FreeSpins);
        }

        /// <summary>
        /// Load default free spins count
        /// </summary>
        private void LoadFreeSpins()
        {
            SetFreeSpinsCount(0);
        }

        /// <summary>
        /// If has free spins, dec free spin and return true.
        /// </summary>
        /// <returns></returns>
        internal bool ApplyFreeSpin()
        {
            if (HasFreeSpin)
            {
                AddFreeSpins(-1);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 프리스핀 체크 메서드
        /// </summary>
        /// <returns></returns>
        internal bool CheckFreeSpin()
        {
            if (HasFreeSpin)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion FreeSpins

        #region AutoSpins
        /// <summary>
        /// Change auto spins cout and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddAutoSpins(int count)
        {
            SetAutoSpinsCount(AutoSpinCount + count);
        }

        /// <summary>
        /// Set level and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetAutoSpinsCount(int count)
        {
            count = Mathf.Max(5, count);
            count = Mathf.Min(count, maxAutoSpins);
            AutoSpinCount = count;
            ChangeAutoSpinsEvent?.Invoke(AutoSpinCount);
        }

        /// <summary>
        /// Load serialized auto spins count or set default auto spins count
        /// </summary>
        private void LoadAutoSpins()
        {
            SetAutoSpinsCount(defAutoSpins);
        }

        public void IncAutoSpinsCounter()
        {
            SetAutoSpinsCounter(AutoSpinsCounter + 1);
        }

        public void SetAutoSpinsCounter(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = (count != AutoSpinsCounter);
            AutoSpinsCounter = count;
            if (changed) ChangeAutoSpinsCounterEvent?.Invoke(AutoSpinsCounter, AutoSpinCount);
        }

        public void ResetAutoSpinsMode()
        {
            Auto = false;
            if (autoSpinButton) autoSpinButton.Release();
            ChangeAutoStateEvent?.Invoke(false);
        }
        #endregion AutoSpins

        public void SetDefaultData()
        {
            SetMiniJackPotCount(miniStart);
            SetMaxiJackPotCount(maxiStart);
            //SetMegaJackPotCount(megaStart);
            SetMegaJackPotProb(megaProbStart);
            SetLineBet(defLineBet);
            SetAutoSpinsCount(defAutoSpins);
        }

        #region utils
        public void SetTextString(Text text, string textString)
        {
            if (text)
            {
                text.text = textString;
            }
        }

        public void SetImageSprite(Image image, Sprite sprite)
        {
            if (image)
            {
                image.sprite = sprite;
            }
        }

        private string GetMoneyName(int count)
        {
            if (count > 1) return "coins";
            else return "coin";
        }
        #endregion utils
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SlotControls))]
    public class SlotControlsEditor : Editor
    {
        private bool test = false;
        Color lineBgColor;
        Sprite normal;
        Sprite pressed;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            SlotControls t = (SlotControls)target;

            if (!EditorApplication.isPlaying)
            {
                if (test = EditorGUILayout.Foldout(test, "Development"))
                {
                    lineBgColor = EditorGUILayout.ColorField("New Color", lineBgColor);

                    EditorGUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("set_color bg"))
                    {
                        LineBehavior [] lbs = t.GetComponentsInChildren<LineBehavior>(true);
                        foreach (var item in lbs)
                        {
                            item.lineInfoBGColor = lineBgColor;
                        }

                    }

                    if (GUILayout.Button("rebuild handles"))
                    {
                        LineCreator[] lbs = t.GetComponentsInChildren<LineCreator>(true);
                        foreach (var item in lbs)
                        {
                            item.SetInitial();
                            EditorUtility.SetDirty(item);
                        }

                    }

                    if (GUILayout.Button("clean raycasters"))
                    {
                        LineBehavior[] lbs = t.GetComponentsInChildren<LineBehavior>(true);
                        foreach (var item in lbs)
                        {
                            List<RayCaster> rcsL = new List<RayCaster>(item.rayCasters);
                            rcsL.RemoveAll((rc) => { return !rc; });
                            item.rayCasters = rcsL.ToArray();
                            EditorUtility.SetDirty(item);
                        }

                    }
                    EditorGUILayout.EndHorizontal();
               
                    EditorGUILayout.BeginVertical();
                    //normal = (Sprite)EditorGUILayout.ObjectField("normal sprite button", (UnityEngine.Object)normal, typeof(Sprite));
                    //pressed = (Sprite)EditorGUILayout.ObjectField("pressed sprite button", (UnityEngine.Object)pressed, typeof(Sprite));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginHorizontal("box");
                    if (GUILayout.Button("Set new button sprites"))
                    {
                        LineButtonBehavior[] lbs = t.GetComponentsInChildren<LineButtonBehavior>(true);
                        foreach (var item in lbs)
                        {
                            item.SetSprites(normal, pressed);
                        }

                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
#endif
}
#endregion