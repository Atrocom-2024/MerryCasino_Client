using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;

namespace Mkey
{
    public class SlotPlayer : MonoBehaviour
    {
        #region properties
        public string Id { get; set; } // 플레이어 ID
        public string Username { get; set; } // 플레이어 닉네임
        public long Coins { get; set; } // 코인 수
        public int Level { get; set; } // 레벨
        public int Experience { get; set; } // 경험치
        public int MinWin { get { return minWin; } }
        public bool UseBigWinCongratulation { get { return useBigWinCongratulation; } }
        #endregion

        #region events
        public Action<long> ChangeCoinsEvent;
        public Action<long> LoadCoinsEvent;
        public Action<int> ChangeJamsEvent;
        public Action<int> LoadJamsEvent;
        public Action<float> ChangeLevelProgressEvent;
        public Action<float> LoadLevelProgressEvent;
        public Action<int, long, bool> ChangeLevelEvent;
        public Action<int> LoadLevelEvent;
        public Action<int> ChangeWinCoinsEvent;
        #endregion events

        #region properties
        public int WinCoins
        {
            get; private set;
        }

        //public long LevelUpReward => levelUpReward;

        //public bool UseLevelUpReward => useLevelUpReward;
        //#endregion properties

        //#region saved properties
        //public long Coins // 유저의 코인 수
        //{
        //    get; set;
        //}

        public int Jams
        {
            get; set;
        }

        //public int Level
        //{
        //    get; private set;
        //}

        //public float LevelProgress
        //{
        //    get; private set;
        //}
        public int UserNum
        {
            get; set;
        }
        #endregion saved properties

        public static SlotPlayer Instance;
        private int minWin = 5000;
        private bool useBigWinCongratulation = true;

        #region regular
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(GetPlayerInfoController());
        }
        #endregion regular

        /// <summary>
        /// 서버에서 받아온 데이터를 플레이어 속성에 저장
        /// </summary>
        /// <param name="data"></param>
        private void SetPlayerData(PlayerData data)
        {
            Id = data.Id;
            Username = data.Username;
            Coins = data.Coins;
            Level = data.Level;
            Experience = data.Experience;
        }

        /// <summary>
        /// 서버에서 플레이어 데이터를 로드
        /// </summary>
        private IEnumerator GetPlayerInfoController()
        {
            Debug.Log("Loading player data from server...");
            yield return RoomAPIManager.Instance.GetPlayerInfo(Id,
                onSuccess: data =>
                {
                    SetPlayerData(data);
                    Debug.Log("Player data successfully loaded.");
                },
                onError: error => Debug.LogError($"Failed to load player data: {error}")
            );
        }

        #region coins
        /// <summary>
        /// 코인을 추가하고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void AddCoins(int count)
        {
            //SetCoinsCount(Coins + count);
            StartCoroutine(AddCoinsController(count));
        }

        public IEnumerator AddCoinsController(int count)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogError("Player ID is not set.");
                yield break;
            }

            //yield return RoomSocketManager.Instance.UpdatePlayerCoins(Id, count,
            //    onSuccess: updatedCoins =>
            //    {
            //        Debug.Log($"Server updated coins successfully. New coin count: {updatedCoins}");
            //        // 로컬 상태를 서버 응답으로 동기화 (필요 시)
            //        SetCoinsCount(updatedCoins);
            //    },
            //    onError: error =>
            //    {
            //        Debug.LogError($"Failed to update coins on server: {error}");
            //    });
        }

        public IEnumerator UpdateBetController(int count)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogError("Player ID is not set.");
                yield break;
            }
            
            //yield return RoomSocketManager.Instance.SendBetReqeust(Id, count,
            //    onSuccess: (updatedCoins, updatedPayout) =>
            //    {
            //        Debug.Log($"Server updated coins successfully. New coin count: {updatedCoins}");
            //        // 로컬 상태를 서버 응답으로 동기화 (필요 시)
            //        SetCoinsCount(updatedCoins);
            //        RoomController.Instance.SetPayout(updatedPayout);
            //    },
            //    onError: error =>
            //    {
            //        Debug.LogError($"Failed to update coins on server: {error}");
            //    });
        }

        /// <summary>
        /// 코인 수를 설정하고 결과를 저장하고 ChangeCoinsEvent를 발생시킴
        /// 만약 데이터 변경마다 DB의 데이터를 수정하려면 이 메서드에서 로직을 구현해야 함
        /// </summary>
        /// <param name="count"></param>
        public void SetCoinsCount(long count)
        {
            Coins = count; // Coins 값을 새로 전달된 count 값으로 업데이트
            ChangeCoinsEvent?.Invoke(Coins);
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
            if (changed)
                ChangeWinCoinsEvent?.Invoke(WinCoins);
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
        //public void AddLevel(int count)
        //{
        //    SetLevel(Level + count);
        //}

        /// <summary>
        /// 레벨을 설정하고, 결과를 저장하고 ChangeLevelEvent를 발생시킴
        /// </summary>
        /// <param name="count"></param>
        //public void SetLevel(int count)
        //{
        //    SetLevel(count, true);
        //}

        /// <summary>
        /// 레벨을 설정하고, 결과를 저장하고 ChangeLevelEvent를 발생시킴
        /// </summary>
        /// <param name="count"></param>
        //private void SetLevel(int count, bool raiseEvent)
        //{
        //    count = Mathf.Max(0, count);
        //    bool changed = (Level != count);
        //    int addLevels = count - Level;
        //    Level = count;
        //    if (SaveData && changed)
        //    {
        //        string key = saveLevelKey;
        //        PlayerPrefs.SetInt(key, Level);
        //    }
        //    if (changed && raiseEvent) ChangeLevelEvent?.Invoke(Level, Mathf.Max(0, addLevels * levelUpReward), useLevelUpReward);
        //}

        /// <summary>
        /// Load serialized level or set 0
        /// </summary>
        //private void LoadLevel()
        //{
        //    if (SaveData)
        //    {
        //        string key = saveLevelKey;
        //        SetLevel (PlayerPrefs.GetInt(key, 0), false);
        //    }
        //    else
        //    {
        //        SetLevel(0, false);
        //    }
        //    LoadLevelEvent?.Invoke(Level);
        //}
        #endregion Level

        #region LevelProgress
        /// <summary>
        /// Change level and save result
        /// </summary>
        /// <param name="count"></param>
        //public void AddLevelProgress(float count)
        //{
        //    SetLevelProgress(LevelProgress + count);
        //}

        /// <summary>
        /// Set level, save result and raise ChangeLevelProgressEvent
        /// </summary>
        /// <param name="count"></param>
        //public void SetLevelProgress(float count)
        //{
        //    SetLevelProgress(count, true);
        //}

        /// <summary>
        /// Set level, save result and raise ChangeLevelProgressEvent
        /// </summary>
        /// <param name="count"></param>
        //private void SetLevelProgress(float count, bool raiseEvent)
        //{
        //    count = Mathf.Max(0, count);
        //    if (count >= 100)
        //    {
        //        int addLevels = (int)count / 100;
        //        AddLevel(addLevels);
        //        count = 0;
        //    }

        //    bool changed = (LevelProgress != count);
        //    LevelProgress = count;
        //    if (SaveData && changed)
        //    {
        //        string key = saveLevelProgressKey;
        //        PlayerPrefs.SetFloat(key, LevelProgress);
        //    }
        //    if (changed && raiseEvent) ChangeLevelProgressEvent?.Invoke(LevelProgress);
        //}

        /// <summary>
        /// Load serialized levelprogress or set 0
        /// </summary>
        //private void LoadLevelProgress()
        //{
        //    if (SaveData)
        //    {
        //        string key = saveLevelProgressKey;
        //        SetLevelProgress(PlayerPrefs.GetFloat(key, 0),false);
        //    }
        //    else
        //    {
        //        SetLevelProgress(0, false);
        //    }
        //    LoadLevelProgressEvent?.Invoke(LevelProgress);
        //}
        #endregion LevelProgress

        //public void SetDefaultData()
        //{
        //    SetCoinsCount(defCoinsCount);

        //    SetLevel(0);
        //    SetLevelProgress(0);
        //}

        //public bool HasMoneyForBet (int totalBet)
        //{
        //     return totalBet <= Coins; 
        //}
    }

    [Serializable]
    public class PlayerData
    {
        public string Id;
        public string Username;
        public long Coins;
        public int Level;
        public int Experience;
    }
}