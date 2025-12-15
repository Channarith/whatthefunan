using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace WhatTheFunan.Core
{
    /// <summary>
    /// Handles saving and loading game data to local storage and cloud.
    /// Supports multiple save slots and auto-save functionality.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        #region Singleton
        private static SaveSystem _instance;
        public static SaveSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveSystem>();
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action OnSaveStarted;
        public static event Action OnSaveCompleted;
        public static event Action OnLoadStarted;
        public static event Action OnLoadCompleted;
        public static event Action<string> OnSaveError;
        public static event Action<string> OnLoadError;
        #endregion

        #region Constants
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string SAVE_FILE_EXTENSION = ".json";
        private const string QUICK_SAVE_KEY = "QuickSave";
        private const int MAX_SAVE_SLOTS = 3;
        #endregion

        #region Settings
        [Header("Save Settings")]
        [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private bool _enableCloudSave = false; // Enable when Firebase is set up
        #endregion

        #region State
        public bool IsSaving { get; private set; }
        public bool IsLoading { get; private set; }
        public int CurrentSlot { get; private set; } = 0;
        public SaveData CurrentSaveData { get; private set; }
        
        private float _lastAutoSaveTime;
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
        }

        private void Update()
        {
            // Auto-save check
            if (_enableAutoSave && 
                GameManager.Instance != null && 
                GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            {
                if (Time.time - _lastAutoSaveTime >= _autoSaveInterval)
                {
                    AutoSave();
                    _lastAutoSaveTime = Time.time;
                }
            }
        }
        #endregion

        #region Save Data Class
        [Serializable]
        public class SaveData
        {
            // Meta
            public string version = "1.0.0";
            public DateTime saveDate = DateTime.Now;
            public float playTime = 0f;
            public int saveSlot = 0;
            
            // Player Progress
            public string currentScene = "";
            public Vector3Serializable playerPosition = new Vector3Serializable();
            public string currentChapter = "Chapter1";
            public int chapterProgress = 0;
            
            // Character Stats
            public string selectedCharacter = "Domrey";
            public int level = 1;
            public int experience = 0;
            public int currentHealth = 100;
            public int maxHealth = 100;
            
            // Economy
            public int funanCoins = 0;
            public int dragonGems = 0;
            
            // Inventory
            public List<string> inventoryItems = new List<string>();
            public List<string> equippedItems = new List<string>();
            
            // Quests
            public List<string> activeQuests = new List<string>();
            public List<string> completedQuests = new List<string>();
            
            // Story/Choices
            public List<StoryChoice> storyChoices = new List<StoryChoice>();
            public List<string> unlockedEndings = new List<string>();
            
            // Collections
            public List<string> collectedCards = new List<string>();
            public List<string> unlockedRecipes = new List<string>();
            public List<string> caughtFish = new List<string>();
            public List<string> unlockedDances = new List<string>();
            
            // Mounts
            public List<string> unlockedMounts = new List<string>();
            public string currentMount = "";
            
            // Codex
            public List<string> unlockedCodexEntries = new List<string>();
            
            // Settings preserved
            public string combatMode = "FreeFlow"; // FreeFlow, PairedAnimation, Automated
            public float musicVolume = 1f;
            public float sfxVolume = 1f;
            public string language = "en";
            
            // Daily/Social
            public DateTime lastLoginDate = DateTime.MinValue;
            public int loginStreak = 0;
            public int dailyRewardDay = 0;
        }

        [Serializable]
        public class StoryChoice
        {
            public string choiceId;
            public string selectedOption;
            public DateTime timestamp;
        }

        [Serializable]
        public class Vector3Serializable
        {
            public float x, y, z;
            
            public Vector3Serializable() { }
            
            public Vector3Serializable(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }
            
            public Vector3 ToVector3() => new Vector3(x, y, z);
        }
        #endregion

        #region Save Methods
        /// <summary>
        /// Save game to the current slot.
        /// </summary>
        public void Save()
        {
            Save(CurrentSlot);
        }

        /// <summary>
        /// Save game to a specific slot.
        /// </summary>
        public void Save(int slot)
        {
            if (IsSaving)
            {
                Debug.LogWarning("[SaveSystem] Already saving. Please wait.");
                return;
            }

            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[SaveSystem] Invalid save slot: {slot}");
                return;
            }

            IsSaving = true;
            OnSaveStarted?.Invoke();

            try
            {
                CurrentSlot = slot;
                
                // Update save data from current game state
                UpdateSaveDataFromGameState();
                
                // Serialize to JSON
                string json = JsonUtility.ToJson(CurrentSaveData, true);
                
                // Save to file
                string path = GetSavePath(slot);
                File.WriteAllText(path, json);
                
                Debug.Log($"[SaveSystem] Game saved to slot {slot} at {path}");
                
                // Cloud save if enabled
                if (_enableCloudSave)
                {
                    SaveToCloud(json);
                }
                
                OnSaveCompleted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Quick save (used for auto-save and app pause).
        /// </summary>
        public void QuickSave()
        {
            if (CurrentSaveData == null) return;
            
            try
            {
                UpdateSaveDataFromGameState();
                string json = JsonUtility.ToJson(CurrentSaveData);
                PlayerPrefs.SetString(QUICK_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveSystem] Quick save completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Quick save failed: {e.Message}");
            }
        }

        /// <summary>
        /// Auto-save triggered by timer.
        /// </summary>
        private void AutoSave()
        {
            Debug.Log("[SaveSystem] Auto-saving...");
            Save(CurrentSlot);
        }

        private void UpdateSaveDataFromGameState()
        {
            if (CurrentSaveData == null)
            {
                CurrentSaveData = new SaveData();
            }

            CurrentSaveData.saveDate = DateTime.Now;
            CurrentSaveData.version = GameManager.Instance?.GameVersion ?? "1.0.0";
            CurrentSaveData.playTime = GameManager.Instance?.PlayTime ?? 0f;
            CurrentSaveData.saveSlot = CurrentSlot;
            CurrentSaveData.currentScene = SceneController.Instance?.CurrentSceneName ?? "";
            
            // TODO: Update from actual game systems when implemented
            // CurrentSaveData.playerPosition = new Vector3Serializable(PlayerController.Instance.transform.position);
            // CurrentSaveData.level = CharacterStats.Instance.Level;
            // etc.
        }
        #endregion

        #region Load Methods
        /// <summary>
        /// Load game from a specific slot.
        /// </summary>
        public bool Load(int slot)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SaveSystem] Already loading. Please wait.");
                return false;
            }

            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
            {
                Debug.LogError($"[SaveSystem] Invalid load slot: {slot}");
                return false;
            }

            IsLoading = true;
            OnLoadStarted?.Invoke();

            try
            {
                string path = GetSavePath(slot);
                
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[SaveSystem] No save file found at slot {slot}");
                    IsLoading = false;
                    return false;
                }

                string json = File.ReadAllText(path);
                CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
                CurrentSlot = slot;
                
                ApplySaveDataToGameState();
                
                Debug.Log($"[SaveSystem] Game loaded from slot {slot}");
                OnLoadCompleted?.Invoke();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                OnLoadError?.Invoke(e.Message);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Load quick save (from app resume).
        /// </summary>
        public bool LoadQuickSave()
        {
            try
            {
                if (!PlayerPrefs.HasKey(QUICK_SAVE_KEY))
                {
                    return false;
                }

                string json = PlayerPrefs.GetString(QUICK_SAVE_KEY);
                CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
                ApplySaveDataToGameState();
                
                Debug.Log("[SaveSystem] Quick save loaded");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Quick save load failed: {e.Message}");
                return false;
            }
        }

        private void ApplySaveDataToGameState()
        {
            if (CurrentSaveData == null) return;
            
            // TODO: Apply save data to actual game systems when implemented
            // PlayerController.Instance.transform.position = CurrentSaveData.playerPosition.ToVector3();
            // CharacterStats.Instance.SetLevel(CurrentSaveData.level);
            // etc.
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Create a new save in a specific slot.
        /// </summary>
        public void CreateNewSave(int slot)
        {
            CurrentSlot = slot;
            CurrentSaveData = new SaveData
            {
                saveSlot = slot,
                saveDate = DateTime.Now,
                lastLoginDate = DateTime.Now,
                loginStreak = 1
            };
            
            Save(slot);
        }

        /// <summary>
        /// Delete a save from a specific slot.
        /// </summary>
        public void DeleteSave(int slot)
        {
            string path = GetSavePath(slot);
            
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Save deleted from slot {slot}");
            }
        }

        /// <summary>
        /// Check if a save exists in a specific slot.
        /// </summary>
        public bool SaveExists(int slot)
        {
            return File.Exists(GetSavePath(slot));
        }

        /// <summary>
        /// Get save info for display (without loading full save).
        /// </summary>
        public SaveData GetSaveInfo(int slot)
        {
            string path = GetSavePath(slot);
            
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get list of all save slots with their info.
        /// </summary>
        public List<SaveData> GetAllSaveSlots()
        {
            List<SaveData> saves = new List<SaveData>();
            
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                saves.Add(GetSaveInfo(i));
            }
            
            return saves;
        }

        private string GetSavePath(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"{SAVE_FILE_PREFIX}{slot}{SAVE_FILE_EXTENSION}");
        }
        #endregion

        #region Cloud Save
        private void SaveToCloud(string json)
        {
            // TODO: Implement Firebase Cloud Save
            Debug.Log("[SaveSystem] Cloud save not yet implemented");
        }

        private void LoadFromCloud()
        {
            // TODO: Implement Firebase Cloud Load
            Debug.Log("[SaveSystem] Cloud load not yet implemented");
        }
        #endregion
    }
}

