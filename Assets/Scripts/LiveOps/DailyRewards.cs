using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.LiveOps
{
    /// <summary>
    /// Manages daily login rewards and streak bonuses.
    /// Rewards reset at midnight local time.
    /// </summary>
    public class DailyRewards : MonoBehaviour
    {
        #region Singleton
        private static DailyRewards _instance;
        public static DailyRewards Instance => _instance;
        #endregion

        #region Events
        public static event Action<DailyReward, int> OnRewardClaimed; // reward, streak day
        public static event Action<int> OnStreakUpdated;
        public static event Action OnStreakBroken;
        public static event Action OnNewDayStarted;
        #endregion

        #region Reward Configuration
        [Header("Daily Rewards (7-day cycle)")]
        [SerializeField] private List<DailyReward> _rewards = new List<DailyReward>();
        
        [Header("Streak Bonus Rewards")]
        [SerializeField] private List<StreakReward> _streakRewards = new List<StreakReward>();
        #endregion

        #region State
        private DateTime _lastClaimDate;
        private int _currentStreak;
        private int _totalDaysClaimed;
        private bool _claimedToday;
        
        public int CurrentStreak => _currentStreak;
        public int TotalDaysClaimed => _totalDaysClaimed;
        public bool CanClaimToday => !_claimedToday;
        public int CurrentCycleDay => (_totalDaysClaimed % _rewards.Count);
        public DailyReward TodaysReward => _rewards.Count > 0 ? _rewards[CurrentCycleDay] : null;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadState();
            CheckNewDay();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // Returning from background
                CheckNewDay();
            }
        }
        #endregion

        #region Daily Check
        private void CheckNewDay()
        {
            DateTime today = DateTime.Today;
            DateTime lastClaimDay = _lastClaimDate.Date;
            
            if (today > lastClaimDay)
            {
                // New day!
                OnNewDayStarted?.Invoke();
                
                // Check streak
                TimeSpan daysDiff = today - lastClaimDay;
                
                if (daysDiff.Days == 1)
                {
                    // Consecutive day - streak continues
                    _claimedToday = false;
                }
                else if (daysDiff.Days > 1)
                {
                    // Missed a day - streak broken
                    if (_currentStreak > 0)
                    {
                        _currentStreak = 0;
                        OnStreakBroken?.Invoke();
                        Debug.Log("[DailyRewards] Streak broken - missed a day");
                    }
                    _claimedToday = false;
                }
                
                SaveState();
            }
        }
        #endregion

        #region Claim Rewards
        /// <summary>
        /// Claim today's daily reward.
        /// </summary>
        public bool ClaimDailyReward()
        {
            if (_claimedToday)
            {
                Debug.Log("[DailyRewards] Already claimed today");
                return false;
            }
            
            var reward = TodaysReward;
            if (reward == null)
            {
                Debug.LogError("[DailyRewards] No reward configured");
                return false;
            }
            
            // Update state
            _lastClaimDate = DateTime.Now;
            _claimedToday = true;
            _currentStreak++;
            _totalDaysClaimed++;
            
            // Grant reward
            GrantReward(reward);
            
            // Check for streak bonus
            CheckStreakBonus();
            
            OnRewardClaimed?.Invoke(reward, _currentStreak);
            OnStreakUpdated?.Invoke(_currentStreak);
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            SaveState();
            
            Debug.Log($"[DailyRewards] Claimed day {CurrentCycleDay + 1} reward. Streak: {_currentStreak}");
            return true;
        }

        private void GrantReward(DailyReward reward)
        {
            if (reward.coins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(reward.coins);
            }
            
            if (reward.gems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(reward.gems);
            }
            
            if (reward.experience > 0)
            {
                // CharacterStats.Instance?.AddExperience(reward.experience);
            }
            
            if (!string.IsNullOrEmpty(reward.itemId))
            {
                // InventorySystem.Instance?.AddItem(reward.itemId);
            }
        }

        private void CheckStreakBonus()
        {
            foreach (var streakReward in _streakRewards)
            {
                if (_currentStreak == streakReward.streakDay)
                {
                    GrantStreakReward(streakReward);
                    Debug.Log($"[DailyRewards] Streak bonus! {streakReward.streakDay} day streak!");
                }
            }
        }

        private void GrantStreakReward(StreakReward reward)
        {
            if (reward.bonusCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(reward.bonusCoins);
            }
            
            if (reward.bonusGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(reward.bonusGems);
            }
            
            if (!string.IsNullOrEmpty(reward.specialUnlockId))
            {
                // Unlock special content
                PlayerPrefs.SetInt($"Unlocked_{reward.specialUnlockId}", 1);
                PlayerPrefs.Save();
            }
        }
        #endregion

        #region Preview
        /// <summary>
        /// Get upcoming rewards for display.
        /// </summary>
        public List<DailyRewardPreview> GetUpcomingRewards(int count = 7)
        {
            var previews = new List<DailyRewardPreview>();
            
            for (int i = 0; i < count && i < _rewards.Count; i++)
            {
                int day = (CurrentCycleDay + i) % _rewards.Count;
                
                previews.Add(new DailyRewardPreview
                {
                    dayNumber = i + 1,
                    reward = _rewards[day],
                    isClaimed = i == 0 && _claimedToday,
                    isToday = i == 0,
                    isStreakBonus = _streakRewards.Exists(s => s.streakDay == _currentStreak + i + 1)
                });
            }
            
            return previews;
        }

        /// <summary>
        /// Get time until next reward resets.
        /// </summary>
        public TimeSpan GetTimeUntilReset()
        {
            DateTime tomorrow = DateTime.Today.AddDays(1);
            return tomorrow - DateTime.Now;
        }
        #endregion

        #region Save/Load
        private void SaveState()
        {
            PlayerPrefs.SetString("DailyRewards_LastClaim", _lastClaimDate.ToString("o"));
            PlayerPrefs.SetInt("DailyRewards_Streak", _currentStreak);
            PlayerPrefs.SetInt("DailyRewards_TotalDays", _totalDaysClaimed);
            PlayerPrefs.SetInt("DailyRewards_ClaimedToday", _claimedToday ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            if (PlayerPrefs.HasKey("DailyRewards_LastClaim"))
            {
                string dateStr = PlayerPrefs.GetString("DailyRewards_LastClaim");
                DateTime.TryParse(dateStr, out _lastClaimDate);
            }
            else
            {
                _lastClaimDate = DateTime.MinValue;
            }
            
            _currentStreak = PlayerPrefs.GetInt("DailyRewards_Streak", 0);
            _totalDaysClaimed = PlayerPrefs.GetInt("DailyRewards_TotalDays", 0);
            _claimedToday = PlayerPrefs.GetInt("DailyRewards_ClaimedToday", 0) == 1;
            
            // Check if the "claimedToday" is from a previous day
            if (_lastClaimDate.Date < DateTime.Today)
            {
                _claimedToday = false;
            }
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class DailyReward
    {
        public string rewardId;
        public string displayName;
        public Sprite icon;
        
        [Header("Rewards")]
        public int coins;
        public int gems;
        public int experience;
        public string itemId;
        
        [Header("Display")]
        public bool isSpecial; // Highlight day 7, etc.
    }

    [Serializable]
    public class StreakReward
    {
        public int streakDay; // e.g., 7, 14, 30
        public int bonusCoins;
        public int bonusGems;
        public string specialUnlockId;
        public string description;
    }

    public class DailyRewardPreview
    {
        public int dayNumber;
        public DailyReward reward;
        public bool isClaimed;
        public bool isToday;
        public bool isStreakBonus;
    }
    #endregion
}

