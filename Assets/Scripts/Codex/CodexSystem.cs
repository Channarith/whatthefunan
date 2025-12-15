using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Codex
{
    /// <summary>
    /// Educational Codex system containing real Funan Kingdom history.
    /// Unlockable entries about history, culture, architecture, and more.
    /// </summary>
    public class CodexSystem : MonoBehaviour
    {
        #region Singleton
        private static CodexSystem _instance;
        public static CodexSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<CodexEntry> OnEntryUnlocked;
        public static event Action<CodexCategory> OnCategoryProgress;
        public static event Action OnCodexComplete;
        #endregion

        #region Codex Data
        [Header("Codex Database")]
        [SerializeField] private List<CodexEntry> _entries = new List<CodexEntry>();
        
        private Dictionary<string, CodexEntry> _entryLookup = new Dictionary<string, CodexEntry>();
        private HashSet<string> _unlockedEntryIds = new HashSet<string>();
        
        public IReadOnlyList<CodexEntry> AllEntries => _entries;
        public int TotalEntries => _entries.Count;
        public int UnlockedCount => _unlockedEntryIds.Count;
        public float CompletionPercent => TotalEntries > 0 ? (float)UnlockedCount / TotalEntries : 0;
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
            
            InitializeCodex();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeCodex()
        {
            _entryLookup.Clear();
            foreach (var entry in _entries)
            {
                _entryLookup[entry.entryId] = entry;
            }
        }
        #endregion

        #region Entry Management
        /// <summary>
        /// Unlock a codex entry.
        /// </summary>
        public bool UnlockEntry(string entryId)
        {
            if (_unlockedEntryIds.Contains(entryId))
            {
                return false; // Already unlocked
            }
            
            if (!_entryLookup.TryGetValue(entryId, out CodexEntry entry))
            {
                Debug.LogWarning($"[CodexSystem] Entry not found: {entryId}");
                return false;
            }
            
            _unlockedEntryIds.Add(entryId);
            OnEntryUnlocked?.Invoke(entry);
            
            // Check category progress
            var category = entry.category;
            OnCategoryProgress?.Invoke(category);
            
            // Check for full completion
            if (_unlockedEntryIds.Count >= _entries.Count)
            {
                OnCodexComplete?.Invoke();
            }
            
            // Grant XP for discovery
            // CharacterStats.Instance?.AddExperience(entry.xpReward);
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            Debug.Log($"[CodexSystem] Unlocked: {entry.title}");
            
            // Update quest objectives
            RPG.QuestSystem.Instance?.UpdateObjective(RPG.ObjectiveType.Discover, entryId);
            
            return true;
        }

        /// <summary>
        /// Check if an entry is unlocked.
        /// </summary>
        public bool IsUnlocked(string entryId)
        {
            return _unlockedEntryIds.Contains(entryId);
        }

        /// <summary>
        /// Get an entry by ID.
        /// </summary>
        public CodexEntry GetEntry(string entryId)
        {
            return _entryLookup.GetValueOrDefault(entryId, null);
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get all entries in a category.
        /// </summary>
        public List<CodexEntry> GetEntriesByCategory(CodexCategory category)
        {
            return _entries.Where(e => e.category == category).ToList();
        }

        /// <summary>
        /// Get unlocked entries in a category.
        /// </summary>
        public List<CodexEntry> GetUnlockedEntriesByCategory(CodexCategory category)
        {
            return _entries
                .Where(e => e.category == category && _unlockedEntryIds.Contains(e.entryId))
                .ToList();
        }

        /// <summary>
        /// Get category completion progress.
        /// </summary>
        public float GetCategoryProgress(CodexCategory category)
        {
            var categoryEntries = GetEntriesByCategory(category);
            if (categoryEntries.Count == 0) return 0;
            
            int unlocked = categoryEntries.Count(e => _unlockedEntryIds.Contains(e.entryId));
            return (float)unlocked / categoryEntries.Count;
        }

        /// <summary>
        /// Get all unlocked entries.
        /// </summary>
        public List<CodexEntry> GetAllUnlockedEntries()
        {
            return _entries.Where(e => _unlockedEntryIds.Contains(e.entryId)).ToList();
        }

        /// <summary>
        /// Search entries by keyword.
        /// </summary>
        public List<CodexEntry> SearchEntries(string keyword)
        {
            keyword = keyword.ToLower();
            return _entries
                .Where(e => _unlockedEntryIds.Contains(e.entryId))
                .Where(e => 
                    e.title.ToLower().Contains(keyword) || 
                    e.content.ToLower().Contains(keyword) ||
                    e.tags.Any(t => t.ToLower().Contains(keyword)))
                .ToList();
        }
        #endregion

        #region Automatic Unlocks
        /// <summary>
        /// Called when player visits a location.
        /// </summary>
        public void OnLocationVisited(string locationId)
        {
            // Find entries associated with this location
            foreach (var entry in _entries)
            {
                if (entry.unlockLocationId == locationId)
                {
                    UnlockEntry(entry.entryId);
                }
            }
        }

        /// <summary>
        /// Called when player defeats a creature.
        /// </summary>
        public void OnCreatureDefeated(string creatureId)
        {
            foreach (var entry in _entries)
            {
                if (entry.unlockCreatureId == creatureId)
                {
                    UnlockEntry(entry.entryId);
                }
            }
        }

        /// <summary>
        /// Called when player finds an artifact.
        /// </summary>
        public void OnArtifactFound(string artifactId)
        {
            foreach (var entry in _entries)
            {
                if (entry.unlockArtifactId == artifactId)
                {
                    UnlockEntry(entry.entryId);
                }
            }
        }
        #endregion

        #region Save/Load
        public CodexSaveData GetSaveData()
        {
            return new CodexSaveData
            {
                unlockedEntryIds = _unlockedEntryIds.ToList()
            };
        }

        public void LoadSaveData(CodexSaveData data)
        {
            _unlockedEntryIds = new HashSet<string>(data.unlockedEntryIds);
        }

        [Serializable]
        public class CodexSaveData
        {
            public List<string> unlockedEntryIds;
        }
        #endregion
    }

    #region Codex Data Classes
    public enum CodexCategory
    {
        History,        // Real Funan Kingdom facts, timeline, rulers
        Culture,        // Traditions, festivals, daily life
        Architecture,   // Temples, water systems, city planning
        Religion,       // Hindu-Buddhist blend, deities, practices
        Trade,          // Maritime trade routes, goods, neighbors
        Language,       // Basic Khmer/Sanskrit words, script
        Creatures,      // Real and mythical animals, their significance
        Art,            // Sculpture, dance, music history
        Geography,      // Rivers, regions, climate
        People          // Important historical figures (fictional names)
    }

    [Serializable]
    public class CodexEntry
    {
        [Header("Identity")]
        public string entryId;
        public string title;
        public CodexCategory category;
        public Sprite icon;
        public Sprite image;
        
        [Header("Content")]
        [TextArea(5, 20)] public string content;
        [TextArea(2, 5)] public string funFact;
        public List<string> tags = new List<string>();
        
        [Header("Unlock Conditions")]
        public string unlockLocationId;
        public string unlockCreatureId;
        public string unlockArtifactId;
        public string unlockQuestId;
        
        [Header("Rewards")]
        public int xpReward = 10;
        
        [Header("Real World Connection")]
        public string wikiLink;           // Wikipedia link (parent-gated)
        public string realWorldLocation;  // "Visit today: ..."
        
        [Header("Related Entries")]
        public List<string> relatedEntryIds = new List<string>();
    }
    #endregion

    #region Sample Entries (Would be created in Unity Inspector)
    /*
    SAMPLE ENTRIES:
    
    HISTORY:
    - "funan_origin" - "The Kingdom of Funan" - Origin story of Funan (1st century CE)
    - "funan_trade" - "Masters of the Sea" - Maritime trade empire
    - "funan_decline" - "Rise of Chenla" - Transition to Chenla kingdom
    
    CULTURE:
    - "water_festival" - "Festival of the Waters" - Bon Om Touk traditions
    - "apsara_dance" - "Dance of the Celestials" - Apsara dance origins
    
    ARCHITECTURE:
    - "temple_design" - "Houses of the Gods" - Temple architecture
    - "water_systems" - "Barays and Reservoirs" - Water management
    
    RELIGION:
    - "hinduism_funan" - "Shiva and Vishnu" - Hindu influence
    - "buddhism_arrival" - "The Middle Way" - Buddhist adoption
    
    CREATURES:
    - "naga_lore" - "Serpent Guardians" - Naga mythology
    - "elephant_symbol" - "Noble Giants" - Elephants in culture
    
    NOTE: All entries use fictional names for rulers to comply with
    legal requirements regarding real historical figures.
    */
    #endregion
}

