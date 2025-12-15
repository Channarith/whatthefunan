using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Achievements
{
    /// <summary>
    /// Manages achievements and their progression.
    /// Syncs with platform services (Game Center, Google Play Games).
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        #region Singleton
        private static AchievementSystem _instance;
        public static AchievementSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Achievement> OnAchievementUnlocked;
        public static event Action<Achievement, float> OnAchievementProgress;
        public static event Action<int> OnPointsEarned; // Total achievement points
        #endregion

        #region Achievement Data
        [Header("Achievements")]
        [SerializeField] private List<Achievement> _achievements = new List<Achievement>();
        
        private Dictionary<string, Achievement> _achievementLookup = new Dictionary<string, Achievement>();
        private Dictionary<string, float> _progress = new Dictionary<string, float>();
        private HashSet<string> _unlockedIds = new HashSet<string>();
        
        public IReadOnlyList<Achievement> AllAchievements => _achievements;
        public int UnlockedCount => _unlockedIds.Count;
        public int TotalPoints => _achievements.Where(a => _unlockedIds.Contains(a.achievementId)).Sum(a => a.points);
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
            
            InitializeAchievements();
            LoadProgress();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeAchievements()
        {
            _achievementLookup.Clear();
            foreach (var achievement in _achievements)
            {
                _achievementLookup[achievement.achievementId] = achievement;
            }
        }
        #endregion

        #region Progress Tracking
        /// <summary>
        /// Add progress to an achievement.
        /// </summary>
        public void AddProgress(string achievementId, float amount = 1f)
        {
            if (!_achievementLookup.TryGetValue(achievementId, out Achievement achievement))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement not found: {achievementId}");
                return;
            }
            
            if (_unlockedIds.Contains(achievementId))
            {
                return; // Already unlocked
            }
            
            float currentProgress = GetProgress(achievementId);
            float newProgress = currentProgress + amount;
            _progress[achievementId] = newProgress;
            
            OnAchievementProgress?.Invoke(achievement, newProgress / achievement.targetValue);
            
            if (newProgress >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }
            else
            {
                SaveProgress();
            }
        }

        /// <summary>
        /// Set absolute progress for an achievement.
        /// </summary>
        public void SetProgress(string achievementId, float value)
        {
            if (!_achievementLookup.TryGetValue(achievementId, out Achievement achievement))
            {
                return;
            }
            
            if (_unlockedIds.Contains(achievementId))
            {
                return;
            }
            
            _progress[achievementId] = value;
            
            OnAchievementProgress?.Invoke(achievement, value / achievement.targetValue);
            
            if (value >= achievement.targetValue)
            {
                UnlockAchievement(achievementId);
            }
            else
            {
                SaveProgress();
            }
        }

        /// <summary>
        /// Get current progress for an achievement.
        /// </summary>
        public float GetProgress(string achievementId)
        {
            return _progress.GetValueOrDefault(achievementId, 0);
        }

        /// <summary>
        /// Get progress as percentage (0-1).
        /// </summary>
        public float GetProgressPercent(string achievementId)
        {
            if (!_achievementLookup.TryGetValue(achievementId, out Achievement achievement))
            {
                return 0;
            }
            
            if (_unlockedIds.Contains(achievementId))
            {
                return 1f;
            }
            
            return GetProgress(achievementId) / achievement.targetValue;
        }
        #endregion

        #region Unlock
        /// <summary>
        /// Unlock an achievement immediately.
        /// </summary>
        public void UnlockAchievement(string achievementId)
        {
            if (_unlockedIds.Contains(achievementId))
            {
                return;
            }
            
            if (!_achievementLookup.TryGetValue(achievementId, out Achievement achievement))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement not found: {achievementId}");
                return;
            }
            
            _unlockedIds.Add(achievementId);
            _progress[achievementId] = achievement.targetValue;
            
            SaveProgress();
            
            OnAchievementUnlocked?.Invoke(achievement);
            OnPointsEarned?.Invoke(TotalPoints);
            
            // Grant rewards
            GrantRewards(achievement);
            
            // Report to platform
            ReportToPlatform(achievement);
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[AchievementSystem] Achievement unlocked: {achievement.achievementName}");
        }

        /// <summary>
        /// Check if an achievement is unlocked.
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return _unlockedIds.Contains(achievementId);
        }
        #endregion

        #region Rewards
        private void GrantRewards(Achievement achievement)
        {
            if (achievement.rewardCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(achievement.rewardCoins);
            }
            
            if (achievement.rewardGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(achievement.rewardGems);
            }
            
            if (!string.IsNullOrEmpty(achievement.rewardUnlockId))
            {
                PlayerPrefs.SetInt($"Achievement_Unlock_{achievement.rewardUnlockId}", 1);
                PlayerPrefs.Save();
            }
        }
        #endregion

        #region Platform Integration
        private void ReportToPlatform(Achievement achievement)
        {
            // TODO: Implement platform-specific reporting
            
            #if UNITY_IOS
            // Game Center
            // Social.ReportProgress(achievement.platformId, 100.0, success => { });
            #endif
            
            #if UNITY_ANDROID
            // Google Play Games
            // PlayGamesPlatform.Instance.ReportProgress(achievement.platformId, 100.0, success => { });
            #endif
            
            Debug.Log($"[AchievementSystem] Reported to platform: {achievement.achievementId}");
        }

        /// <summary>
        /// Sync achievements with platform services.
        /// </summary>
        public void SyncWithPlatform()
        {
            // TODO: Implement platform sync
            Debug.Log("[AchievementSystem] Syncing with platform...");
        }
        #endregion

        #region Query
        /// <summary>
        /// Get achievements by category.
        /// </summary>
        public List<Achievement> GetByCategory(AchievementCategory category)
        {
            return _achievements.Where(a => a.category == category).ToList();
        }

        /// <summary>
        /// Get locked achievements.
        /// </summary>
        public List<Achievement> GetLocked()
        {
            return _achievements.Where(a => !_unlockedIds.Contains(a.achievementId)).ToList();
        }

        /// <summary>
        /// Get unlocked achievements.
        /// </summary>
        public List<Achievement> GetUnlocked()
        {
            return _achievements.Where(a => _unlockedIds.Contains(a.achievementId)).ToList();
        }

        /// <summary>
        /// Get nearly complete achievements.
        /// </summary>
        public List<Achievement> GetNearlyComplete(float thresholdPercent = 0.8f)
        {
            return _achievements
                .Where(a => !_unlockedIds.Contains(a.achievementId))
                .Where(a => GetProgressPercent(a.achievementId) >= thresholdPercent)
                .ToList();
        }
        #endregion

        #region Save/Load
        private void SaveProgress()
        {
            // Save progress
            foreach (var kvp in _progress)
            {
                PlayerPrefs.SetFloat($"Achievement_Progress_{kvp.Key}", kvp.Value);
            }
            
            // Save unlocked
            string unlocked = string.Join(",", _unlockedIds);
            PlayerPrefs.SetString("Achievements_Unlocked", unlocked);
            
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            // Load unlocked
            string unlocked = PlayerPrefs.GetString("Achievements_Unlocked", "");
            if (!string.IsNullOrEmpty(unlocked))
            {
                _unlockedIds = new HashSet<string>(unlocked.Split(','));
            }
            
            // Load progress
            foreach (var achievement in _achievements)
            {
                float progress = PlayerPrefs.GetFloat($"Achievement_Progress_{achievement.achievementId}", 0);
                if (progress > 0)
                {
                    _progress[achievement.achievementId] = progress;
                }
            }
        }
        #endregion

        #region Event Hooks (Call from other systems)
        public void OnEnemyDefeated(string enemyType)
        {
            AddProgress("defeat_enemies_10", 1);
            AddProgress("defeat_enemies_100", 1);
            AddProgress($"defeat_{enemyType}_10", 1);
        }

        public void OnQuestCompleted()
        {
            AddProgress("complete_quests_10", 1);
            AddProgress("complete_quests_50", 1);
        }

        public void OnBossDefeated(string bossId)
        {
            UnlockAchievement($"defeat_boss_{bossId}");
            AddProgress("defeat_bosses_all", 1);
        }

        public void OnFishCaught(string fishRarity)
        {
            AddProgress("catch_fish_50", 1);
            if (fishRarity == "legendary")
            {
                UnlockAchievement("catch_legendary_fish");
            }
        }

        public void OnRecipeCooked(int starRating)
        {
            AddProgress("cook_dishes_20", 1);
            if (starRating == 3)
            {
                AddProgress("cook_perfect_10", 1);
            }
        }

        public void OnCodexEntryUnlocked()
        {
            AddProgress("discover_codex_50", 1);
        }

        public void OnDailyStreak(int days)
        {
            SetProgress("login_streak_7", days);
            SetProgress("login_streak_30", days);
        }
        #endregion
    }

    #region Achievement Data Classes
    public enum AchievementCategory
    {
        Combat,
        Exploration,
        Collection,
        Social,
        Progression,
        MiniGames,
        Secret
    }

    public enum AchievementRarity
    {
        Common,     // Easy to get
        Uncommon,   // Moderate effort
        Rare,       // Significant effort
        Epic,       // Major accomplishment
        Legendary   // Exceptional
    }

    [Serializable]
    public class Achievement
    {
        [Header("Identity")]
        public string achievementId;
        public string achievementName;
        [TextArea] public string description;
        public Sprite icon;
        public Sprite lockedIcon;
        
        [Header("Classification")]
        public AchievementCategory category;
        public AchievementRarity rarity;
        public int points = 10;
        
        [Header("Progress")]
        public float targetValue = 1f;
        public bool isHidden = false;
        
        [Header("Rewards")]
        public int rewardCoins;
        public int rewardGems;
        public string rewardUnlockId;
        
        [Header("Platform")]
        public string platformId; // Game Center / Google Play Games ID
    }
    #endregion
}

