using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Collection
{
    /// <summary>
    /// Manages collectible albums - creatures, fish, recipes, artwork, etc.
    /// Provides completion tracking and rewards.
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        #region Singleton
        private static CollectionManager _instance;
        public static CollectionManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<CollectionEntry> OnEntryUnlocked;
        public static event Action<CollectionCategory> OnCategoryCompleted;
        public static event Action OnCollectionComplete;
        public static event Action<int> OnCompletionMilestone;
        #endregion

        #region Collection Data
        [Header("Collection Categories")]
        [SerializeField] private List<CollectionCategory> _categories = new List<CollectionCategory>();
        
        private Dictionary<string, CollectionEntry> _entryLookup = new Dictionary<string, CollectionEntry>();
        private HashSet<string> _unlockedEntries = new HashSet<string>();
        
        public IReadOnlyList<CollectionCategory> Categories => _categories;
        #endregion

        #region Statistics
        public int TotalEntries => _categories.Sum(c => c.entries.Count);
        public int UnlockedCount => _unlockedEntries.Count;
        public float CompletionPercent => TotalEntries > 0 ? (float)UnlockedCount / TotalEntries * 100 : 0;
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
            
            InitializeCollections();
            LoadProgress();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeCollections()
        {
            _entryLookup.Clear();
            foreach (var category in _categories)
            {
                foreach (var entry in category.entries)
                {
                    _entryLookup[entry.entryId] = entry;
                }
            }
        }
        #endregion

        #region Unlock Entries
        /// <summary>
        /// Unlock a collection entry.
        /// </summary>
        public bool UnlockEntry(string entryId)
        {
            if (_unlockedEntries.Contains(entryId))
            {
                return false; // Already unlocked
            }
            
            if (!_entryLookup.TryGetValue(entryId, out CollectionEntry entry))
            {
                Debug.LogWarning($"[CollectionManager] Entry not found: {entryId}");
                return false;
            }
            
            _unlockedEntries.Add(entryId);
            entry.unlockedDate = DateTime.Now;
            
            SaveProgress();
            
            OnEntryUnlocked?.Invoke(entry);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            // Check milestones
            CheckMilestones();
            
            // Check category completion
            var category = GetCategoryForEntry(entryId);
            if (category != null && IsCategoryComplete(category))
            {
                OnCategoryCompleted?.Invoke(category);
                GrantCategoryReward(category);
            }
            
            // Check full completion
            if (_unlockedEntries.Count >= TotalEntries)
            {
                OnCollectionComplete?.Invoke();
            }
            
            Debug.Log($"[CollectionManager] Unlocked: {entry.entryName}");
            return true;
        }

        /// <summary>
        /// Unlock multiple entries at once.
        /// </summary>
        public int UnlockEntries(List<string> entryIds)
        {
            int count = 0;
            foreach (var id in entryIds)
            {
                if (UnlockEntry(id))
                {
                    count++;
                }
            }
            return count;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Check if an entry is unlocked.
        /// </summary>
        public bool IsUnlocked(string entryId)
        {
            return _unlockedEntries.Contains(entryId);
        }

        /// <summary>
        /// Get an entry by ID.
        /// </summary>
        public CollectionEntry GetEntry(string entryId)
        {
            return _entryLookup.GetValueOrDefault(entryId, null);
        }

        /// <summary>
        /// Get category for an entry.
        /// </summary>
        public CollectionCategory GetCategoryForEntry(string entryId)
        {
            foreach (var category in _categories)
            {
                if (category.entries.Any(e => e.entryId == entryId))
                {
                    return category;
                }
            }
            return null;
        }

        /// <summary>
        /// Get category progress.
        /// </summary>
        public float GetCategoryProgress(string categoryId)
        {
            var category = _categories.FirstOrDefault(c => c.categoryId == categoryId);
            if (category == null || category.entries.Count == 0) return 0;
            
            int unlocked = category.entries.Count(e => _unlockedEntries.Contains(e.entryId));
            return (float)unlocked / category.entries.Count;
        }

        /// <summary>
        /// Check if a category is complete.
        /// </summary>
        public bool IsCategoryComplete(CollectionCategory category)
        {
            return category.entries.All(e => _unlockedEntries.Contains(e.entryId));
        }

        /// <summary>
        /// Get unlocked entries in a category.
        /// </summary>
        public List<CollectionEntry> GetUnlockedInCategory(string categoryId)
        {
            var category = _categories.FirstOrDefault(c => c.categoryId == categoryId);
            if (category == null) return new List<CollectionEntry>();
            
            return category.entries.Where(e => _unlockedEntries.Contains(e.entryId)).ToList();
        }

        /// <summary>
        /// Get locked entries in a category.
        /// </summary>
        public List<CollectionEntry> GetLockedInCategory(string categoryId)
        {
            var category = _categories.FirstOrDefault(c => c.categoryId == categoryId);
            if (category == null) return new List<CollectionEntry>();
            
            return category.entries.Where(e => !_unlockedEntries.Contains(e.entryId)).ToList();
        }
        #endregion

        #region Milestones & Rewards
        private void CheckMilestones()
        {
            int[] milestones = { 10, 25, 50, 75, 100, 150, 200 };
            
            foreach (int milestone in milestones)
            {
                if (_unlockedEntries.Count == milestone)
                {
                    OnCompletionMilestone?.Invoke(milestone);
                    GrantMilestoneReward(milestone);
                    break;
                }
            }
        }

        private void GrantMilestoneReward(int milestone)
        {
            int gems = milestone / 5;
            Economy.CurrencyManager.Instance?.AddGems(gems);
            
            Debug.Log($"[CollectionManager] Milestone {milestone}! Rewarded {gems} gems");
        }

        private void GrantCategoryReward(CollectionCategory category)
        {
            if (category.completionRewardGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(category.completionRewardGems);
            }
            
            if (!string.IsNullOrEmpty(category.completionUnlockId))
            {
                // Unlock special content
                PlayerPrefs.SetInt($"Collection_Unlock_{category.completionUnlockId}", 1);
                PlayerPrefs.Save();
            }
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[CollectionManager] Category complete: {category.categoryName}");
        }
        #endregion

        #region Save/Load
        private void SaveProgress()
        {
            string data = string.Join(",", _unlockedEntries);
            PlayerPrefs.SetString("Collection_Unlocked", data);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            string data = PlayerPrefs.GetString("Collection_Unlocked", "");
            if (!string.IsNullOrEmpty(data))
            {
                _unlockedEntries = new HashSet<string>(data.Split(','));
            }
        }
        #endregion
    }

    #region Collection Data Classes
    [Serializable]
    public class CollectionCategory
    {
        public string categoryId;
        public string categoryName;
        public Sprite icon;
        public List<CollectionEntry> entries = new List<CollectionEntry>();
        
        [Header("Completion Reward")]
        public int completionRewardGems;
        public string completionUnlockId;
    }

    [Serializable]
    public class CollectionEntry
    {
        public string entryId;
        public string entryName;
        [TextArea] public string description;
        public Sprite icon;
        public Sprite fullImage;
        
        [Header("Rarity")]
        public EntryRarity rarity;
        
        [Header("Discovery")]
        public string hintText; // Hint for locked entries
        public string locationHint;
        
        [HideInInspector] public DateTime unlockedDate;
    }

    public enum EntryRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    #endregion
}

