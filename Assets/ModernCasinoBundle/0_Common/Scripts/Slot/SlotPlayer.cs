﻿using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace Mkey
{
    public class SlotPlayer : MonoBehaviour
    {
        public static SlotPlayer Instance { get; private set; }

        #region properties
        public string Id { get; set; } // 플레이어 ID
        public string Username { get; set; } // 플레이어 닉네임
        public long Coins { get; set; } // 코인 수
        public int Level { get; set; } // 레벨
        public int Experience { get; set; } // 경험치
        public int MinWin { get { return minWin; } } // 게임 내에서 빅윈의 기준을 정하기 위한 변수
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

        private int minWin = 5000;
        private bool useBigWinCongratulation = true;

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
                return;
            }
        }
        #endregion regular

        /// <summary>
        /// minWin 값을 설정하는 메서드
        /// </summary>
        /// <param name="count"></param>
        public void SetMinWinCoins(int count)
        {
            minWin = count;
        }

        #region coins
        /// <summary>
        /// 코인을 추가하고 결과를 저장
        /// </summary>
        /// <param name="count"></param>
        public void AddCoins(int count)
        {
            StartCoroutine(AddCoinsController(Id, count));
        }

        public IEnumerator AddCoinsController(string userId, int count)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("Player ID is not set.");
                yield break;
            }

            // 배팅 요청 비동기 작업 시작
            Task sendAddCoinsTask = RoomSocketManager.Instance.SendAddCoinsRequest(userId, count);

            // Task가 완료될 때까지 대기
            while (!sendAddCoinsTask.IsCompleted)
            {
                yield return null; // 다음 프레임까지 대기
            }
        }

        /// <summary>
        /// 코인 수를 설정하고 결과를 저장하고 ChangeCoinsEvent를 발생시킴
        /// 만약 데이터 변경마다 DB의 데이터를 수정하려면 이 메서드에서 로직을 구현해야 함
        /// </summary>
        /// <param name="count"></param>
        public void SetCoinsCount(long count)
        {
            Coins = Math.Max(0, count); // Coins 값을 새로 전달된 count 값으로 업데이트
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
            WinCoins = count;
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
    public class UserData
    {
        public string Id;
        public string Username;
        public long Coins;
        public int Level;
        public int Experience;
    }
}