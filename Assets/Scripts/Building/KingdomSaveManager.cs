using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Handles saving and loading of kingdom builds.
    /// Supports multiple save slots and cloud sync.
    /// </summary>
    public class KingdomSaveManager : MonoBehaviour
    {
        #region Singleton
        private static KingdomSaveManager _instance;
        public static KingdomSaveManager Instance => _instance;
        #endregion

        #region Events
        public static event Action OnSaveStarted;
        public static event Action OnSaveCompleted;
        public static event Action OnLoadStarted;
        public static event Action OnLoadCompleted;
        public static event Action<string> OnSaveError;
        #endregion

        #region Save Data Structure
        [Serializable]
        public class KingdomSaveData
        {
            public string saveId;
            public string saveName;
            public DateTime saveDate;
            public string gameVersion;
            
            // Kingdom data
            public KingdomManager.KingdomData kingdomData;
            
            // Placed objects
            public List<PlacedObjectData> placedObjects;
            
            // Resources
            public List<ResourceEntry> resources;
            
            // Metadata
            public int objectCount;
            public float playtimeHours;
            public string thumbnailBase64;
        }

        [Serializable]
        public class PlacedObjectData
        {
            public string objectId;
            public float[] position; // x, y, z
            public float[] rotation; // x, y, z
            public int paintIndex;
            public string customData;
        }

        [Serializable]
        public class ResourceEntry
        {
            public string resourceId;
            public int amount;
        }
        #endregion

        #region Settings
        [Header("Save Settings")]
        [SerializeField] private int _maxSaveSlots = 5;
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
        
        [Header("Cloud")]
        [SerializeField] private bool _cloudSyncEnabled = true;
        #endregion

        #region State
        private string _saveFolderPath;
        private float _lastAutoSaveTime;
        private bool _isSaving;
        private bool _isLoading;
        
        public bool IsSaving => _isSaving;
        public bool IsLoading => _isLoading;
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
            
            _saveFolderPath = Path.Combine(Application.persistentDataPath, "KingdomSaves");
            EnsureSaveFolderExists();
        }

        private void Update()
        {
            // Auto-save
            if (_autoSaveEnabled && Time.time - _lastAutoSaveTime > _autoSaveInterval)
            {
                AutoSave();
                _lastAutoSaveTime = Time.time;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _autoSaveEnabled)
            {
                AutoSave();
            }
        }

        private void OnApplicationQuit()
        {
            if (_autoSaveEnabled)
            {
                AutoSave();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Save
        /// <summary>
        /// Save kingdom to a slot.
        /// </summary>
        public bool SaveKingdom(int slotIndex, string saveName = "")
        {
            if (_isSaving || _isLoading)
            {
                Debug.LogWarning("[KingdomSaveManager] Already saving/loading");
                return false;
            }
            
            if (slotIndex < 0 || slotIndex >= _maxSaveSlots)
            {
                Debug.LogError("[KingdomSaveManager] Invalid slot index");
                return false;
            }
            
            _isSaving = true;
            OnSaveStarted?.Invoke();
            
            try
            {
                var saveData = CreateSaveData(slotIndex, saveName);
                string json = JsonUtility.ToJson(saveData, true);
                string filePath = GetSaveFilePath(slotIndex);
                
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[KingdomSaveManager] Saved to slot {slotIndex}: {filePath}");
                
                // Cloud sync
                if (_cloudSyncEnabled)
                {
                    SyncToCloud(slotIndex, json);
                }
                
                OnSaveCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[KingdomSaveManager] Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Auto-save to slot 0.
        /// </summary>
        public void AutoSave()
        {
            SaveKingdom(0, "Auto Save");
        }

        private KingdomSaveData CreateSaveData(int slotIndex, string saveName)
        {
            var saveData = new KingdomSaveData
            {
                saveId = Guid.NewGuid().ToString(),
                saveName = string.IsNullOrEmpty(saveName) ? $"Save {slotIndex + 1}" : saveName,
                saveDate = DateTime.Now,
                gameVersion = Application.version
            };
            
            // Kingdom data
            saveData.kingdomData = KingdomManager.Instance?.Data;
            
            // Placed objects
            saveData.placedObjects = new List<PlacedObjectData>();
            var placed = BuildingSystem.Instance?.GetPlacedObjects();
            if (placed != null)
            {
                foreach (var obj in placed)
                {
                    saveData.placedObjects.Add(new PlacedObjectData
                    {
                        objectId = obj.objectId,
                        position = new float[] { obj.position.x, obj.position.y, obj.position.z },
                        rotation = new float[] { obj.rotation.x, obj.rotation.y, obj.rotation.z },
                        customData = obj.customData
                    });
                }
            }
            saveData.objectCount = saveData.placedObjects.Count;
            
            // Resources
            saveData.resources = new List<ResourceEntry>();
            var resources = ResourceManager.Instance?.GetAllResources();
            if (resources != null)
            {
                foreach (var kvp in resources)
                {
                    saveData.resources.Add(new ResourceEntry
                    {
                        resourceId = kvp.Key,
                        amount = kvp.Value
                    });
                }
            }
            
            // Metadata
            saveData.playtimeHours = KingdomManager.Instance?.Data?.totalPlayTime / 3600f ?? 0;
            
            // Thumbnail (would capture screenshot)
            // saveData.thumbnailBase64 = CaptureThumbnail();
            
            return saveData;
        }
        #endregion

        #region Load
        /// <summary>
        /// Load kingdom from a slot.
        /// </summary>
        public bool LoadKingdom(int slotIndex)
        {
            if (_isSaving || _isLoading)
            {
                Debug.LogWarning("[KingdomSaveManager] Already saving/loading");
                return false;
            }
            
            string filePath = GetSaveFilePath(slotIndex);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[KingdomSaveManager] No save file at slot {slotIndex}");
                return false;
            }
            
            _isLoading = true;
            OnLoadStarted?.Invoke();
            
            try
            {
                string json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<KingdomSaveData>(json);
                
                ApplySaveData(saveData);
                
                Debug.Log($"[KingdomSaveManager] Loaded from slot {slotIndex}");
                
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[KingdomSaveManager] Load failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
                return false;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void ApplySaveData(KingdomSaveData saveData)
        {
            // Clear current kingdom
            BuildingSystem.Instance?.ClearAllObjects();
            
            // Apply kingdom data
            // KingdomManager would need a LoadData method
            
            // Place objects
            var placedList = new List<PlacedObject>();
            foreach (var objData in saveData.placedObjects)
            {
                placedList.Add(new PlacedObject
                {
                    objectId = objData.objectId,
                    position = new Vector3(objData.position[0], objData.position[1], objData.position[2]),
                    rotation = new Vector3(objData.rotation[0], objData.rotation[1], objData.rotation[2]),
                    customData = objData.customData
                });
            }
            BuildingSystem.Instance?.LoadPlacedObjects(placedList);
            
            // Apply resources
            foreach (var res in saveData.resources)
            {
                ResourceManager.Instance?.SetResource(res.resourceId, res.amount);
            }
        }
        #endregion

        #region Save Slots
        /// <summary>
        /// Get info about all save slots.
        /// </summary>
        public List<SaveSlotInfo> GetSaveSlots()
        {
            var slots = new List<SaveSlotInfo>();
            
            for (int i = 0; i < _maxSaveSlots; i++)
            {
                slots.Add(GetSaveSlotInfo(i));
            }
            
            return slots;
        }

        /// <summary>
        /// Get info about a specific save slot.
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            
            if (!File.Exists(filePath))
            {
                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    isEmpty = true
                };
            }
            
            try
            {
                string json = File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<KingdomSaveData>(json);
                
                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    isEmpty = false,
                    saveName = saveData.saveName,
                    saveDate = saveData.saveDate,
                    kingdomLevel = saveData.kingdomData?.level ?? 1,
                    objectCount = saveData.objectCount,
                    playtimeHours = saveData.playtimeHours,
                    thumbnailBase64 = saveData.thumbnailBase64
                };
            }
            catch
            {
                return new SaveSlotInfo
                {
                    slotIndex = slotIndex,
                    isEmpty = true,
                    isCorrupted = true
                };
            }
        }

        /// <summary>
        /// Delete a save slot.
        /// </summary>
        public bool DeleteSave(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[KingdomSaveManager] Deleted save slot {slotIndex}");
                return true;
            }
            
            return false;
        }
        #endregion

        #region Export/Import
        /// <summary>
        /// Export save to shareable string.
        /// </summary>
        public string ExportKingdom(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            string json = File.ReadAllText(filePath);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Import save from shareable string.
        /// </summary>
        public bool ImportKingdom(string exportedData, int targetSlot)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(exportedData);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                
                // Validate JSON
                var saveData = JsonUtility.FromJson<KingdomSaveData>(json);
                if (saveData == null || string.IsNullOrEmpty(saveData.saveId))
                {
                    return false;
                }
                
                // Save to slot
                string filePath = GetSaveFilePath(targetSlot);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[KingdomSaveManager] Imported kingdom to slot {targetSlot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[KingdomSaveManager] Import failed: {e.Message}");
                return false;
            }
        }
        #endregion

        #region Cloud Sync
        private void SyncToCloud(int slotIndex, string json)
        {
            // Would integrate with Firebase Cloud Save
            Backend.FirebaseManager.Instance?.SaveCloudData($"kingdom_save_{slotIndex}", json);
        }

        /// <summary>
        /// Download cloud save.
        /// </summary>
        public void DownloadFromCloud(int slotIndex, Action<bool> callback)
        {
            // Would download from Firebase
            Backend.FirebaseManager.Instance?.LoadCloudData($"kingdom_save_{slotIndex}", (success, data) =>
            {
                if (success && !string.IsNullOrEmpty(data))
                {
                    string filePath = GetSaveFilePath(slotIndex);
                    File.WriteAllText(filePath, data);
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }
        #endregion

        #region Helpers
        private void EnsureSaveFolderExists()
        {
            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }
        }

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(_saveFolderPath, $"kingdom_save_{slotIndex}.json");
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class SaveSlotInfo
    {
        public int slotIndex;
        public bool isEmpty;
        public bool isCorrupted;
        public string saveName;
        public DateTime saveDate;
        public int kingdomLevel;
        public int objectCount;
        public float playtimeHours;
        public string thumbnailBase64;
    }
    #endregion
}

