using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace Mkey
{
    public class SlotPlayer : MonoBehaviour
    {
        #region default data
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]
        [Tooltip("Default coins at start")]
        [SerializeField]
        private int defCoinsCount = 500;

        [Tooltip("Default Jams at start")]
        [SerializeField]
        private int jam = 500;

        [Tooltip("Default facebook coins")]
        [SerializeField]
        private int defFBCoinsCount = 100;

        [Tooltip("Check if you want to add level up reward")]
        [SerializeField]
        private bool useLevelUpReward = true;

        [Tooltip("Default level up reward")]
        [SerializeField]
        private int levelUpReward = 3000;

        [Tooltip("Check if you want to show big win congratulation")]
        [SerializeField]
        private bool useBigWinCongratulation = true;

        [Tooltip("Min win to show big win congratulation")]
        [SerializeField]
        private int minWin = 5000;

        [Space(9)]
        [Tooltip("Check if you want to save coins, level, progress, facebook gift flag, sound settings")]
        [SerializeField]
        private bool saveData = false;

        #endregion default data

        #region keys
        // current coins

        private string saveCoinsKey = "mk_slot_coins"; // current coins
        private string saveFbCoinsKey = "mk_slot_fbcoins"; // facebook coins
        private string saveLevelKey = "mk_slot_level"; // current level
        private string saveLevelProgressKey = "mk_slot_level_progress"; // progress to next level %
        #endregion keys

        #region events
        public Action<long> ChangeCoinsEvent;
        public Action<int> LoadCoinsEvent;
        public Action<int> ChangeJamsEvent;
        public Action<int> LoadJamsEvent;
        public Action<float> ChangeLevelProgressEvent;
        public Action<float> LoadLevelProgressEvent;
        public Action<int, long, bool> ChangeLevelEvent;
        public Action<int> LoadLevelEvent;
        public Action<int> ChangeWinCoinsEvent;
        #endregion events

        #region properties

        public string Id
        {
            get; set;
        }
        public bool SaveData
        {
            get { return saveData; }
        }

        public int MinWin
        {
            get { return minWin; }
        }

        public bool UseBigWinCongratulation
        {
            get { return useBigWinCongratulation; }
        }

        public int WinCoins
        {
            get; private set;
        }

        public long LevelUpReward => levelUpReward;

        public bool UseLevelUpReward => useLevelUpReward;
        #endregion properties

        #region saved properties
        public long Coins // 유저의 코인 수
        {
            get; set;
        }

        public int Jams
        {
            get; set;
        }

        public int Level
        {
            get; private set;
        }

        public float LevelProgress
        {
            get; private set;
        }
        public int UserNum
        {
            get; set;
        }
        #endregion saved properties

        public static SlotPlayer Instance;

        #region regular
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            //Validate();
            LoadCoins();
            LoadLevel();
            LoadLevelProgress();
        }

        private void OnValidate()
        {
            Validate();
        }

        private void Validate()
        {
            defCoinsCount = Math.Max(0, defCoinsCount);
            defFBCoinsCount = Math.Max(0, defFBCoinsCount);
            levelUpReward = Math.Max(0, levelUpReward);
        }
        #endregion regular

        #region coins
        /// <summary>
        /// 코인을 추가하고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void AddCoins(int count)
        {
            SetCoinsCount(Coins + count);
        }

        /// <summary>
        /// 코인 수를 설정하고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void SetCoinsCount(long count)
        {
            SetCoinsCount(count, true);
        }

        /// <summary>
        /// 코인 수를 설정하고 결과를 저장하고 ChangeCoinsEvent를 발생시킴
        /// </summary>
        /// <param name="count"></param>
        private void SetCoinsCount(long count, bool raiseEvent)
        {
            //count = Mathf.Max(0, count);
            bool changed = (Coins != count); // 코인이 변경될 때 changed를 true로 설정
            Coins = count; // Coins 값을 새로 전달된 count 값으로 업데이트
            if (SaveData && changed)
            {
                string key = saveCoinsKey;
                //PlayerPrefs.SetInt(key, Coins);
            }
            if (changed && raiseEvent) ChangeCoinsEvent?.Invoke(Coins);
        }

        /// <summary>
        /// Add facebook gift (only once), and save flag.
        /// </summary>
        public void AddFbCoins()
        {
            if (!PlayerPrefs.HasKey(saveFbCoinsKey) || PlayerPrefs.GetInt(saveFbCoinsKey) == 0)
            {
                PlayerPrefs.SetInt(saveFbCoinsKey, 1);
                AddCoins(defFBCoinsCount);
            }
        }
        
        /// <summary>
        /// 서버로부터 코인 데이터를 받아오는 메서드
        /// </summary>
        private IEnumerator GetCoins()
        {
            const string url = "http://localhost:3000/api/players";

            using (UnityWebRequest request = UnityWebRequest.Get($"{url}/{Instance.Id}"))
            {
                yield return request.SendWebRequest();
                Debug.Log(request.downloadHandler.text);
                if (request.result == UnityWebRequest.Result.Success)
                {
                    long coins = long.Parse(request.downloadHandler.text);
                    SetCoinsCount(coins, false); // 서버에서 받은 코인 데이터를 설정
                }
                else
                {
                    Debug.LogError("Failed to load coins from server: " + request.error);
                    SetCoinsCount(defCoinsCount, false); // 실패 시 기본값으로 설정
                }
            }
        }

        /// <summary>
        /// 직렬화된 코인 데이터를 불러오거나, 기본값으로 설정
        /// </summary>
        private void LoadCoins()
        {
            if (SaveData)
            {
                string key = saveCoinsKey;
                //SetCoinsCount(PlayerPrefs.GetInt(key, defCoinsCount), false);
                StartCoroutine(GetCoins());
            }
            else
            {
                SetCoinsCount(defCoinsCount, false);
            }

            //LoadCoinsEvent?.Invoke(Coins);
        }
        #endregion coins

        #region wincoins
        /// <summary>
        /// 승리 코인 수에 count만큼 코인을 추가하고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void AddWinCoins(int count)
        {
            SetWinCoinsCount(WinCoins + count);
        }

        /// <summary>
        /// 승리 코인의 수를 설정하고, 변경된 경우 이를 저장
        /// </summary>
        /// <param name="count"></param>
        public void SetWinCoinsCount(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = (WinCoins != count);
            WinCoins = count;
            if (changed) ChangeWinCoinsEvent?.Invoke(WinCoins);
        }

        /// <summary>
        /// 승리한 코인(WinCoins)을 플레이어의 총 코인에 추가하고, 승리 코인 수를 0으로 초기화하는 역할
        /// </summary>
        public void TakeWin()
        {
            AddCoins(WinCoins);
            SetWinCoinsCount(0);
        }
        #endregion wincoins

        #region Level
        /// <summary>
        /// 레벨을 변경시키고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void AddLevel(int count)
        {
            SetLevel(Level + count);
        }

        /// <summary>
        /// 레벨을 설정하고, 결과를 저장하고 ChangeLevelEvent를 발생시킴
        /// </summary>
        /// <param name="count"></param>
        public void SetLevel(int count)
        {
            SetLevel(count, true);
        }

        /// <summary>
        /// 레벨을 설정하고, 결과를 저장하고 ChangeLevelEvent를 발생시킴
        /// </summary>
        /// <param name="count"></param>
        private void SetLevel(int count, bool raiseEvent)
        {
            count = Mathf.Max(0, count);
            bool changed = (Level != count);
            int addLevels = count - Level;
            Level = count;
            if (SaveData && changed)
            {
                string key = saveLevelKey;
                PlayerPrefs.SetInt(key, Level);
            }
            if (changed && raiseEvent) ChangeLevelEvent?.Invoke(Level, Mathf.Max(0, addLevels * levelUpReward), useLevelUpReward);
        }

        /// <summary>
        /// Load serialized level or set 0
        /// </summary>
        private void LoadLevel()
        {
            if (SaveData)
            {
                string key = saveLevelKey;
                SetLevel (PlayerPrefs.GetInt(key, 0), false);
            }
            else
            {
                SetLevel(0, false);
            }
            LoadLevelEvent?.Invoke(Level);
        }
        #endregion Level

        #region LevelProgress
        /// <summary>
        /// Change level and save result
        /// </summary>
        /// <param name="count"></param>
        public void AddLevelProgress(float count)
        {
            SetLevelProgress(LevelProgress + count);
        }

        /// <summary>
        /// Set level, save result and raise ChangeLevelProgressEvent
        /// </summary>
        /// <param name="count"></param>
        public void SetLevelProgress(float count)
        {
            SetLevelProgress(count, true);
        }

        /// <summary>
        /// Set level, save result and raise ChangeLevelProgressEvent
        /// </summary>
        /// <param name="count"></param>
        private void SetLevelProgress(float count, bool raiseEvent)
        {
            count = Mathf.Max(0, count);
            if (count >= 100)
            {
                int addLevels = (int)count / 100;
                AddLevel(addLevels);
                count = 0;
            }

            bool changed = (LevelProgress != count);
            LevelProgress = count;
            if (SaveData && changed)
            {
                string key = saveLevelProgressKey;
                PlayerPrefs.SetFloat(key, LevelProgress);
            }
            if (changed && raiseEvent) ChangeLevelProgressEvent?.Invoke(LevelProgress);
        }

        /// <summary>
        /// Load serialized levelprogress or set 0
        /// </summary>
        private void LoadLevelProgress()
        {
            if (SaveData)
            {
                string key = saveLevelProgressKey;
                SetLevelProgress(PlayerPrefs.GetFloat(key, 0),false);
            }
            else
            {
                SetLevelProgress(0, false);
            }
            LoadLevelProgressEvent?.Invoke(LevelProgress);
        }
        #endregion LevelProgress

        public void SetDefaultData()
        {
            //SetCoinsCount(defCoinsCount);
            PlayerPrefs.SetInt(saveFbCoinsKey, 0); // reset facebook gift
           
            SetLevel(0);
            SetLevelProgress(0);
        }

        public bool HasMoneyForBet (int totalBet)
        {
             return totalBet <= Coins; 
        }
    }
}