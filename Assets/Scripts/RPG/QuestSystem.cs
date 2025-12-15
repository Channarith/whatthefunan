using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using WhatTheFunan.Economy;

namespace WhatTheFunan.RPG
{
    /// <summary>
    /// Manages all quests, objectives, and quest progression.
    /// Supports main story quests, side quests, daily quests, and event quests.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        #region Singleton
        private static QuestSystem _instance;
        public static QuestSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Quest> OnQuestStarted;
        public static event Action<Quest> OnQuestCompleted;
        public static event Action<Quest> OnQuestFailed;
        public static event Action<Quest, QuestObjective> OnObjectiveUpdated;
        public static event Action<Quest, QuestObjective> OnObjectiveCompleted;
        public static event Action<QuestReward> OnRewardClaimed;
        #endregion

        #region Quest Data
        [Header("Quest Database")]
        [SerializeField] private List<Quest> _allQuests = new List<Quest>();
        
        private Dictionary<string, Quest> _questLookup = new Dictionary<string, Quest>();
        private List<Quest> _activeQuests = new List<Quest>();
        private List<string> _completedQuestIds = new List<string>();
        
        public IReadOnlyList<Quest> ActiveQuests => _activeQuests;
        public IReadOnlyList<string> CompletedQuestIds => _completedQuestIds;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private int _maxActiveQuests = 10;
        [SerializeField] private int _maxDailyQuests = 3;
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
            
            InitializeQuests();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeQuests()
        {
            _questLookup.Clear();
            foreach (var quest in _allQuests)
            {
                if (!string.IsNullOrEmpty(quest.questId))
                {
                    _questLookup[quest.questId] = quest;
                }
            }
        }
        #endregion

        #region Quest Management
        /// <summary>
        /// Start a quest by ID.
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (!_questLookup.TryGetValue(questId, out Quest quest))
            {
                Debug.LogWarning($"[QuestSystem] Quest not found: {questId}");
                return false;
            }
            
            return StartQuest(quest);
        }

        /// <summary>
        /// Start a quest.
        /// </summary>
        public bool StartQuest(Quest quest)
        {
            if (quest == null) return false;
            
            // Check if already active or completed
            if (_activeQuests.Contains(quest))
            {
                Debug.Log($"[QuestSystem] Quest already active: {quest.questName}");
                return false;
            }
            
            if (_completedQuestIds.Contains(quest.questId) && !quest.isRepeatable)
            {
                Debug.Log($"[QuestSystem] Quest already completed: {quest.questName}");
                return false;
            }
            
            // Check prerequisites
            if (!CheckPrerequisites(quest))
            {
                Debug.Log($"[QuestSystem] Prerequisites not met for: {quest.questName}");
                return false;
            }
            
            // Check max active quests
            if (_activeQuests.Count >= _maxActiveQuests)
            {
                Debug.LogWarning("[QuestSystem] Max active quests reached");
                return false;
            }
            
            // Start quest
            quest.state = QuestState.Active;
            quest.startTime = DateTime.Now;
            
            // Reset objectives
            foreach (var objective in quest.objectives)
            {
                objective.currentProgress = 0;
                objective.isCompleted = false;
            }
            
            _activeQuests.Add(quest);
            OnQuestStarted?.Invoke(quest);
            
            Debug.Log($"[QuestSystem] Quest started: {quest.questName}");
            return true;
        }

        /// <summary>
        /// Abandon an active quest.
        /// </summary>
        public void AbandonQuest(string questId)
        {
            var quest = _activeQuests.FirstOrDefault(q => q.questId == questId);
            if (quest != null)
            {
                quest.state = QuestState.Failed;
                _activeQuests.Remove(quest);
                OnQuestFailed?.Invoke(quest);
                Debug.Log($"[QuestSystem] Quest abandoned: {quest.questName}");
            }
        }

        /// <summary>
        /// Complete a quest and claim rewards.
        /// </summary>
        public void CompleteQuest(Quest quest)
        {
            if (quest == null || quest.state != QuestState.Active) return;
            
            // Check if all objectives are complete
            if (!quest.objectives.All(o => o.isCompleted))
            {
                Debug.LogWarning($"[QuestSystem] Not all objectives complete for: {quest.questName}");
                return;
            }
            
            quest.state = QuestState.Completed;
            quest.completionTime = DateTime.Now;
            
            _activeQuests.Remove(quest);
            _completedQuestIds.Add(quest.questId);
            
            // Grant rewards
            GrantRewards(quest.rewards);
            
            OnQuestCompleted?.Invoke(quest);
            Core.HapticManager.Instance?.OnQuestComplete();
            
            Debug.Log($"[QuestSystem] Quest completed: {quest.questName}");
            
            // Auto-start next quest in chain
            if (!string.IsNullOrEmpty(quest.nextQuestId))
            {
                StartQuest(quest.nextQuestId);
            }
        }

        private bool CheckPrerequisites(Quest quest)
        {
            // Check required quests
            foreach (var reqId in quest.requiredQuestIds)
            {
                if (!_completedQuestIds.Contains(reqId))
                {
                    return false;
                }
            }
            
            // Check required level
            // TODO: Check CharacterStats.Instance.Level >= quest.requiredLevel
            
            return true;
        }

        private void GrantRewards(QuestReward rewards)
        {
            if (rewards == null) return;
            
            // Currency
            if (rewards.coins > 0)
            {
                CurrencyManager.Instance?.AddCoins(rewards.coins);
            }
            if (rewards.gems > 0)
            {
                CurrencyManager.Instance?.AddGems(rewards.gems);
            }
            
            // Experience
            if (rewards.experience > 0)
            {
                // TODO: CharacterStats.Instance.AddExperience(rewards.experience);
            }
            
            // Items
            foreach (var itemId in rewards.itemIds)
            {
                // TODO: InventorySystem.Instance.AddItem(itemId);
            }
            
            OnRewardClaimed?.Invoke(rewards);
        }
        #endregion

        #region Objective Updates
        /// <summary>
        /// Update objective progress by type.
        /// </summary>
        public void UpdateObjective(ObjectiveType type, string targetId = null, int amount = 1)
        {
            foreach (var quest in _activeQuests.ToList())
            {
                foreach (var objective in quest.objectives)
                {
                    if (objective.isCompleted) continue;
                    if (objective.type != type) continue;
                    if (!string.IsNullOrEmpty(objective.targetId) && objective.targetId != targetId) continue;
                    
                    objective.currentProgress += amount;
                    objective.currentProgress = Mathf.Min(objective.currentProgress, objective.requiredProgress);
                    
                    OnObjectiveUpdated?.Invoke(quest, objective);
                    
                    if (objective.currentProgress >= objective.requiredProgress)
                    {
                        objective.isCompleted = true;
                        OnObjectiveCompleted?.Invoke(quest, objective);
                    }
                }
                
                // Check if all objectives complete
                if (quest.objectives.All(o => o.isCompleted))
                {
                    if (quest.autoComplete)
                    {
                        CompleteQuest(quest);
                    }
                }
            }
        }

        /// <summary>
        /// Record enemy kill for quest objectives.
        /// </summary>
        public void OnEnemyKilled(string enemyId)
        {
            UpdateObjective(ObjectiveType.Kill, enemyId);
            UpdateObjective(ObjectiveType.KillAny);
        }

        /// <summary>
        /// Record item collection for quest objectives.
        /// </summary>
        public void OnItemCollected(string itemId)
        {
            UpdateObjective(ObjectiveType.Collect, itemId);
        }

        /// <summary>
        /// Record location visit for quest objectives.
        /// </summary>
        public void OnLocationVisited(string locationId)
        {
            UpdateObjective(ObjectiveType.Visit, locationId);
        }

        /// <summary>
        /// Record NPC interaction for quest objectives.
        /// </summary>
        public void OnNPCTalkedTo(string npcId)
        {
            UpdateObjective(ObjectiveType.TalkTo, npcId);
        }

        /// <summary>
        /// Record item delivery for quest objectives.
        /// </summary>
        public void OnItemDelivered(string itemId, string npcId)
        {
            UpdateObjective(ObjectiveType.Deliver, itemId);
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get a quest by ID.
        /// </summary>
        public Quest GetQuest(string questId)
        {
            _questLookup.TryGetValue(questId, out Quest quest);
            return quest;
        }

        /// <summary>
        /// Check if a quest is active.
        /// </summary>
        public bool IsQuestActive(string questId)
        {
            return _activeQuests.Any(q => q.questId == questId);
        }

        /// <summary>
        /// Check if a quest is completed.
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return _completedQuestIds.Contains(questId);
        }

        /// <summary>
        /// Get all available quests (can be started).
        /// </summary>
        public List<Quest> GetAvailableQuests()
        {
            return _allQuests.Where(q => 
                !IsQuestActive(q.questId) && 
                !IsQuestCompleted(q.questId) && 
                CheckPrerequisites(q)
            ).ToList();
        }

        /// <summary>
        /// Get quests by type.
        /// </summary>
        public List<Quest> GetQuestsByType(QuestType type)
        {
            return _allQuests.Where(q => q.type == type).ToList();
        }
        #endregion

        #region Save/Load
        public QuestSaveData GetSaveData()
        {
            return new QuestSaveData
            {
                activeQuestIds = _activeQuests.Select(q => q.questId).ToList(),
                completedQuestIds = new List<string>(_completedQuestIds),
                objectiveProgress = _activeQuests.ToDictionary(
                    q => q.questId,
                    q => q.objectives.Select(o => o.currentProgress).ToList()
                )
            };
        }

        public void LoadSaveData(QuestSaveData data)
        {
            _completedQuestIds = new List<string>(data.completedQuestIds);
            _activeQuests.Clear();
            
            foreach (var questId in data.activeQuestIds)
            {
                if (_questLookup.TryGetValue(questId, out Quest quest))
                {
                    quest.state = QuestState.Active;
                    _activeQuests.Add(quest);
                    
                    // Restore objective progress
                    if (data.objectiveProgress.TryGetValue(questId, out List<int> progress))
                    {
                        for (int i = 0; i < quest.objectives.Count && i < progress.Count; i++)
                        {
                            quest.objectives[i].currentProgress = progress[i];
                            quest.objectives[i].isCompleted = 
                                quest.objectives[i].currentProgress >= quest.objectives[i].requiredProgress;
                        }
                    }
                }
            }
        }

        [Serializable]
        public class QuestSaveData
        {
            public List<string> activeQuestIds;
            public List<string> completedQuestIds;
            public Dictionary<string, List<int>> objectiveProgress;
        }
        #endregion
    }

    #region Quest Data Classes
    public enum QuestType
    {
        Main,       // Main story quest
        Side,       // Optional side quest
        Daily,      // Daily repeatable quest
        Event,      // Time-limited event quest
        Tutorial    // Tutorial quest
    }

    public enum QuestState
    {
        Inactive,
        Available,
        Active,
        Completed,
        Failed
    }

    public enum ObjectiveType
    {
        Kill,           // Kill specific enemy type
        KillAny,        // Kill any enemies
        Collect,        // Collect items
        Visit,          // Visit location
        TalkTo,         // Talk to NPC
        Deliver,        // Deliver item to NPC
        Escort,         // Escort NPC
        Survive,        // Survive for time
        Complete,       // Complete activity (mini-game, etc.)
        Discover        // Discover codex entry
    }

    [Serializable]
    public class Quest
    {
        [Header("Identity")]
        public string questId;
        public string questName;
        [TextArea] public string description;
        public QuestType type;
        public Sprite icon;
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public List<string> requiredQuestIds = new List<string>();
        
        [Header("Objectives")]
        public List<QuestObjective> objectives = new List<QuestObjective>();
        
        [Header("Rewards")]
        public QuestReward rewards;
        
        [Header("Settings")]
        public bool isRepeatable = false;
        public bool autoComplete = true;
        public float timeLimit = 0f; // 0 = no limit
        
        [Header("Chain")]
        public string nextQuestId;
        
        // Runtime state
        [HideInInspector] public QuestState state = QuestState.Inactive;
        [HideInInspector] public DateTime startTime;
        [HideInInspector] public DateTime completionTime;
    }

    [Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public string targetId;         // Enemy ID, item ID, location ID, etc.
        public int requiredProgress;
        public bool isOptional = false;
        
        // Runtime state
        [HideInInspector] public int currentProgress = 0;
        [HideInInspector] public bool isCompleted = false;
        
        public float ProgressPercent => (float)currentProgress / requiredProgress;
    }

    [Serializable]
    public class QuestReward
    {
        public int coins;
        public int gems;
        public int experience;
        public List<string> itemIds = new List<string>();
        public string unlockId; // Unlock character, mount, etc.
    }
    #endregion
}

