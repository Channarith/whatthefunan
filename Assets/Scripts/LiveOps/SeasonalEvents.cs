using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.LiveOps
{
    /// <summary>
    /// Manages seasonal and limited-time events.
    /// Includes Water Festival, Harvest Festival, and special events.
    /// </summary>
    public class SeasonalEvents : MonoBehaviour
    {
        #region Singleton
        private static SeasonalEvents _instance;
        public static SeasonalEvents Instance => _instance;
        #endregion

        #region Events
        public static event Action<SeasonalEvent> OnEventStarted;
        public static event Action<SeasonalEvent> OnEventEnded;
        public static event Action<SeasonalEvent, EventChallenge> OnChallengeCompleted;
        public static event Action<SeasonalEvent> OnEventRewardsClaimed;
        #endregion

        #region Event Data
        [Header("Events")]
        [SerializeField] private List<SeasonalEvent> _events = new List<SeasonalEvent>();
        
        private Dictionary<string, SeasonalEvent> _eventLookup = new Dictionary<string, SeasonalEvent>();
        private Dictionary<string, EventProgress> _eventProgress = new Dictionary<string, EventProgress>();
        
        public IReadOnlyList<SeasonalEvent> AllEvents => _events;
        #endregion

        #region State
        private SeasonalEvent _currentEvent;
        public SeasonalEvent CurrentEvent => _currentEvent;
        public bool HasActiveEvent => _currentEvent != null && IsEventActive(_currentEvent);
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
            
            InitializeEvents();
            LoadProgress();
        }

        private void Start()
        {
            CheckForActiveEvents();
        }

        private void Update()
        {
            // Check for event state changes periodically
            // In production, this would be less frequent
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeEvents()
        {
            _eventLookup.Clear();
            foreach (var evt in _events)
            {
                _eventLookup[evt.eventId] = evt;
            }
        }
        #endregion

        #region Event State
        /// <summary>
        /// Check for currently active events.
        /// </summary>
        public void CheckForActiveEvents()
        {
            // Check scheduled events
            foreach (var evt in _events)
            {
                if (IsEventActive(evt))
                {
                    if (_currentEvent == null || _currentEvent.eventId != evt.eventId)
                    {
                        StartEvent(evt);
                    }
                    return;
                }
            }
            
            // No active events
            if (_currentEvent != null)
            {
                EndEvent(_currentEvent);
            }
        }

        /// <summary>
        /// Check if an event is currently active.
        /// </summary>
        public bool IsEventActive(SeasonalEvent evt)
        {
            if (evt == null) return false;
            
            DateTime now = DateTime.Now;
            return now >= evt.startDate && now <= evt.endDate;
        }

        /// <summary>
        /// Get time remaining in current event.
        /// </summary>
        public TimeSpan GetTimeRemaining()
        {
            if (_currentEvent == null) return TimeSpan.Zero;
            
            return _currentEvent.endDate - DateTime.Now;
        }

        private void StartEvent(SeasonalEvent evt)
        {
            _currentEvent = evt;
            
            // Initialize progress if needed
            if (!_eventProgress.ContainsKey(evt.eventId))
            {
                _eventProgress[evt.eventId] = new EventProgress
                {
                    eventId = evt.eventId,
                    challengeProgress = new Dictionary<string, int>(),
                    rewardsClaimed = new HashSet<int>()
                };
            }
            
            // Apply event modifiers
            ApplyEventModifiers(evt);
            
            // Schedule notification for event end
            Notifications.NotificationManager.Instance?.ScheduleEventNotification(
                evt.eventName, evt.endDate.AddHours(-1));
            
            OnEventStarted?.Invoke(evt);
            Debug.Log($"[SeasonalEvents] Event started: {evt.eventName}");
        }

        private void EndEvent(SeasonalEvent evt)
        {
            // Remove event modifiers
            RemoveEventModifiers(evt);
            
            OnEventEnded?.Invoke(evt);
            
            _currentEvent = null;
            Debug.Log($"[SeasonalEvents] Event ended: {evt.eventName}");
        }
        #endregion

        #region Challenges
        /// <summary>
        /// Add progress to an event challenge.
        /// </summary>
        public void AddChallengeProgress(string challengeId, int amount = 1)
        {
            if (_currentEvent == null) return;
            
            var challenge = _currentEvent.challenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challenge == null) return;
            
            var progress = _eventProgress[_currentEvent.eventId];
            
            if (!progress.challengeProgress.ContainsKey(challengeId))
            {
                progress.challengeProgress[challengeId] = 0;
            }
            
            int oldProgress = progress.challengeProgress[challengeId];
            progress.challengeProgress[challengeId] += amount;
            int newProgress = progress.challengeProgress[challengeId];
            
            // Check for completion
            if (oldProgress < challenge.targetAmount && newProgress >= challenge.targetAmount)
            {
                OnChallengeCompleted?.Invoke(_currentEvent, challenge);
                GrantChallengeReward(challenge);
                Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            }
            
            SaveProgress();
        }

        /// <summary>
        /// Get progress for a challenge.
        /// </summary>
        public int GetChallengeProgress(string challengeId)
        {
            if (_currentEvent == null) return 0;
            
            var progress = _eventProgress.GetValueOrDefault(_currentEvent.eventId, null);
            if (progress == null) return 0;
            
            return progress.challengeProgress.GetValueOrDefault(challengeId, 0);
        }

        /// <summary>
        /// Check if a challenge is complete.
        /// </summary>
        public bool IsChallengeComplete(string challengeId)
        {
            if (_currentEvent == null) return false;
            
            var challenge = _currentEvent.challenges.FirstOrDefault(c => c.challengeId == challengeId);
            if (challenge == null) return false;
            
            return GetChallengeProgress(challengeId) >= challenge.targetAmount;
        }

        private void GrantChallengeReward(EventChallenge challenge)
        {
            if (challenge.rewardCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(challenge.rewardCoins);
            }
            
            if (challenge.rewardGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(challenge.rewardGems);
            }
            
            if (!string.IsNullOrEmpty(challenge.rewardItemId))
            {
                // InventorySystem.Instance?.AddItem(challenge.rewardItemId);
            }
        }
        #endregion

        #region Event Points & Rewards
        /// <summary>
        /// Get total event points earned.
        /// </summary>
        public int GetTotalEventPoints()
        {
            if (_currentEvent == null) return 0;
            
            int total = 0;
            foreach (var challenge in _currentEvent.challenges)
            {
                if (IsChallengeComplete(challenge.challengeId))
                {
                    total += challenge.pointsReward;
                }
            }
            
            return total;
        }

        /// <summary>
        /// Claim event milestone reward.
        /// </summary>
        public bool ClaimMilestoneReward(int milestoneIndex)
        {
            if (_currentEvent == null) return false;
            if (milestoneIndex < 0 || milestoneIndex >= _currentEvent.milestoneRewards.Count) return false;
            
            var milestone = _currentEvent.milestoneRewards[milestoneIndex];
            var progress = _eventProgress[_currentEvent.eventId];
            
            if (progress.rewardsClaimed.Contains(milestoneIndex))
            {
                Debug.Log("[SeasonalEvents] Reward already claimed");
                return false;
            }
            
            if (GetTotalEventPoints() < milestone.pointsRequired)
            {
                Debug.Log("[SeasonalEvents] Not enough points");
                return false;
            }
            
            // Grant reward
            if (milestone.rewardCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(milestone.rewardCoins);
            }
            if (milestone.rewardGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(milestone.rewardGems);
            }
            if (!string.IsNullOrEmpty(milestone.rewardUnlockId))
            {
                PlayerPrefs.SetInt($"Event_Unlock_{milestone.rewardUnlockId}", 1);
            }
            
            progress.rewardsClaimed.Add(milestoneIndex);
            SaveProgress();
            
            OnEventRewardsClaimed?.Invoke(_currentEvent);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            return true;
        }
        #endregion

        #region Event Modifiers
        private void ApplyEventModifiers(SeasonalEvent evt)
        {
            // Apply global modifiers during event
            foreach (var modifier in evt.modifiers)
            {
                switch (modifier.type)
                {
                    case EventModifierType.XPBoost:
                        // CharacterStats.Instance?.SetXPMultiplier(modifier.value);
                        break;
                    case EventModifierType.CoinBoost:
                        // CurrencyManager.Instance?.SetCoinMultiplier(modifier.value);
                        break;
                }
            }
        }

        private void RemoveEventModifiers(SeasonalEvent evt)
        {
            // Reset modifiers
            // CharacterStats.Instance?.SetXPMultiplier(1f);
            // CurrencyManager.Instance?.SetCoinMultiplier(1f);
        }
        #endregion

        #region Save/Load
        private void SaveProgress()
        {
            // Serialize and save event progress
            foreach (var kvp in _eventProgress)
            {
                string json = JsonUtility.ToJson(new EventProgressData(kvp.Value));
                PlayerPrefs.SetString($"EventProgress_{kvp.Key}", json);
            }
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            foreach (var evt in _events)
            {
                string json = PlayerPrefs.GetString($"EventProgress_{evt.eventId}", "");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<EventProgressData>(json);
                    _eventProgress[evt.eventId] = data.ToEventProgress();
                }
            }
        }

        [Serializable]
        private class EventProgressData
        {
            public string eventId;
            public List<string> challengeKeys = new List<string>();
            public List<int> challengeValues = new List<int>();
            public List<int> rewardsClaimed = new List<int>();
            
            public EventProgressData(EventProgress progress)
            {
                eventId = progress.eventId;
                foreach (var kvp in progress.challengeProgress)
                {
                    challengeKeys.Add(kvp.Key);
                    challengeValues.Add(kvp.Value);
                }
                rewardsClaimed = progress.rewardsClaimed.ToList();
            }
            
            public EventProgress ToEventProgress()
            {
                var progress = new EventProgress
                {
                    eventId = eventId,
                    challengeProgress = new Dictionary<string, int>(),
                    rewardsClaimed = new HashSet<int>(rewardsClaimed)
                };
                
                for (int i = 0; i < challengeKeys.Count; i++)
                {
                    progress.challengeProgress[challengeKeys[i]] = challengeValues[i];
                }
                
                return progress;
            }
        }
        #endregion
    }

    #region Event Data Classes
    [Serializable]
    public class SeasonalEvent
    {
        [Header("Identity")]
        public string eventId;
        public string eventName;
        [TextArea] public string description;
        public Sprite banner;
        public Sprite icon;
        public EventTheme theme;
        
        [Header("Schedule")]
        public DateTime startDate;
        public DateTime endDate;
        
        [Header("Challenges")]
        public List<EventChallenge> challenges = new List<EventChallenge>();
        
        [Header("Milestone Rewards")]
        public List<MilestoneReward> milestoneRewards = new List<MilestoneReward>();
        
        [Header("Modifiers")]
        public List<EventModifier> modifiers = new List<EventModifier>();
        
        [Header("Special Content")]
        public string specialSceneId;
        public string specialQuestId;
        public List<string> eventShopItems = new List<string>();
    }

    public enum EventTheme
    {
        WaterFestival,      // Bon Om Touk inspired
        HarvestFestival,    // Pchum Ben inspired
        NewYear,            // Khmer New Year inspired
        TempleBlessing,     // Temple ceremony
        DragonBoat,         // Boat racing
        LanternFestival,    // Light festival
        MonsoonSeason,      // Rainy season event
        Anniversary         // Game anniversary
    }

    [Serializable]
    public class EventChallenge
    {
        public string challengeId;
        public string challengeName;
        [TextArea] public string description;
        public ChallengeType type;
        public int targetAmount;
        public int pointsReward;
        
        [Header("Rewards")]
        public int rewardCoins;
        public int rewardGems;
        public string rewardItemId;
    }

    public enum ChallengeType
    {
        DefeatEnemies,
        CompleteQuests,
        CollectItems,
        PlayMiniGames,
        Login,
        SpendCurrency,
        Custom
    }

    [Serializable]
    public class MilestoneReward
    {
        public int pointsRequired;
        public string rewardName;
        public Sprite icon;
        public int rewardCoins;
        public int rewardGems;
        public string rewardUnlockId;
    }

    [Serializable]
    public class EventModifier
    {
        public EventModifierType type;
        public float value;
    }

    public enum EventModifierType
    {
        XPBoost,
        CoinBoost,
        DropRateBoost,
        EnergyReduction
    }

    public class EventProgress
    {
        public string eventId;
        public Dictionary<string, int> challengeProgress;
        public HashSet<int> rewardsClaimed;
    }
    #endregion
}

