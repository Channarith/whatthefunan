using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Gameplay
{
    /// <summary>
    /// Manages rideable mounts - elephants, serpents, and mystical creatures.
    /// Mounts provide traversal bonuses and can be customized.
    /// </summary>
    public class MountSystem : MonoBehaviour
    {
        #region Singleton
        private static MountSystem _instance;
        public static MountSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Mount> OnMountUnlocked;
        public static event Action<Mount> OnMountEquipped;
        public static event Action OnMountDismounted;
        public static event Action<Mount, MountSkin> OnMountSkinApplied;
        #endregion

        #region Mount Data
        [Header("Mount Database")]
        [SerializeField] private List<Mount> _mounts = new List<Mount>();
        
        private Dictionary<string, Mount> _mountLookup = new Dictionary<string, Mount>();
        private HashSet<string> _unlockedMountIds = new HashSet<string>();
        private Dictionary<string, string> _equippedSkins = new Dictionary<string, string>();
        
        private Mount _currentMount;
        private bool _isMounted;
        
        public Mount CurrentMount => _currentMount;
        public bool IsMounted => _isMounted;
        public IReadOnlyList<Mount> AllMounts => _mounts;
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
            
            InitializeMounts();
            LoadUnlockedMounts();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeMounts()
        {
            _mountLookup.Clear();
            foreach (var mount in _mounts)
            {
                _mountLookup[mount.mountId] = mount;
            }
        }
        #endregion

        #region Mount Management
        /// <summary>
        /// Unlock a mount.
        /// </summary>
        public bool UnlockMount(string mountId)
        {
            if (_unlockedMountIds.Contains(mountId))
            {
                Debug.Log($"[MountSystem] Mount already unlocked: {mountId}");
                return false;
            }
            
            if (!_mountLookup.TryGetValue(mountId, out Mount mount))
            {
                Debug.LogWarning($"[MountSystem] Mount not found: {mountId}");
                return false;
            }
            
            _unlockedMountIds.Add(mountId);
            SaveUnlockedMounts();
            
            OnMountUnlocked?.Invoke(mount);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[MountSystem] Unlocked mount: {mount.mountName}");
            return true;
        }

        /// <summary>
        /// Check if a mount is unlocked.
        /// </summary>
        public bool IsMountUnlocked(string mountId)
        {
            return _unlockedMountIds.Contains(mountId);
        }

        /// <summary>
        /// Get all unlocked mounts.
        /// </summary>
        public List<Mount> GetUnlockedMounts()
        {
            return _mounts.Where(m => _unlockedMountIds.Contains(m.mountId)).ToList();
        }
        #endregion

        #region Mounting/Dismounting
        /// <summary>
        /// Mount a specific mount.
        /// </summary>
        public bool MountUp(string mountId)
        {
            if (_isMounted)
            {
                Debug.Log("[MountSystem] Already mounted");
                return false;
            }
            
            if (!_unlockedMountIds.Contains(mountId))
            {
                Debug.LogWarning($"[MountSystem] Mount not unlocked: {mountId}");
                return false;
            }
            
            if (!_mountLookup.TryGetValue(mountId, out Mount mount))
            {
                Debug.LogWarning($"[MountSystem] Mount not found: {mountId}");
                return false;
            }
            
            _currentMount = mount;
            _isMounted = true;
            
            // Apply mount bonuses
            ApplyMountBonuses(mount);
            
            OnMountEquipped?.Invoke(mount);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Medium);
            
            Debug.Log($"[MountSystem] Mounted: {mount.mountName}");
            return true;
        }

        /// <summary>
        /// Dismount from current mount.
        /// </summary>
        public void Dismount()
        {
            if (!_isMounted) return;
            
            // Remove mount bonuses
            if (_currentMount != null)
            {
                RemoveMountBonuses(_currentMount);
            }
            
            _currentMount = null;
            _isMounted = false;
            
            OnMountDismounted?.Invoke();
            
            Debug.Log("[MountSystem] Dismounted");
        }

        /// <summary>
        /// Toggle mount on/off.
        /// </summary>
        public void ToggleMount(string mountId)
        {
            if (_isMounted)
            {
                Dismount();
            }
            else
            {
                MountUp(mountId);
            }
        }
        #endregion

        #region Mount Bonuses
        private void ApplyMountBonuses(Mount mount)
        {
            // Speed bonus
            // CharacterController.Instance?.SetSpeedMultiplier(mount.speedMultiplier);
            
            // Special abilities based on mount type
            switch (mount.type)
            {
                case MountType.Land:
                    // Ground speed bonus
                    break;
                case MountType.Water:
                    // Water traversal enabled
                    break;
                case MountType.Flying:
                    // Flight enabled
                    break;
            }
        }

        private void RemoveMountBonuses(Mount mount)
        {
            // Reset speed
            // CharacterController.Instance?.SetSpeedMultiplier(1f);
            
            // Remove special abilities
        }
        #endregion

        #region Skins
        /// <summary>
        /// Apply a skin to a mount.
        /// </summary>
        public bool ApplySkin(string mountId, string skinId)
        {
            if (!_mountLookup.TryGetValue(mountId, out Mount mount))
            {
                return false;
            }
            
            var skin = mount.skins.FirstOrDefault(s => s.skinId == skinId);
            if (skin == null)
            {
                Debug.LogWarning($"[MountSystem] Skin not found: {skinId}");
                return false;
            }
            
            if (!skin.isUnlocked)
            {
                Debug.LogWarning($"[MountSystem] Skin not unlocked: {skinId}");
                return false;
            }
            
            _equippedSkins[mountId] = skinId;
            SaveEquippedSkins();
            
            OnMountSkinApplied?.Invoke(mount, skin);
            
            Debug.Log($"[MountSystem] Applied skin {skinId} to {mountId}");
            return true;
        }

        /// <summary>
        /// Unlock a mount skin.
        /// </summary>
        public bool UnlockSkin(string mountId, string skinId)
        {
            if (!_mountLookup.TryGetValue(mountId, out Mount mount))
            {
                return false;
            }
            
            var skin = mount.skins.FirstOrDefault(s => s.skinId == skinId);
            if (skin == null) return false;
            
            skin.isUnlocked = true;
            
            // Save unlock status
            PlayerPrefs.SetInt($"MountSkin_{mountId}_{skinId}", 1);
            PlayerPrefs.Save();
            
            return true;
        }

        /// <summary>
        /// Get current skin for a mount.
        /// </summary>
        public string GetEquippedSkin(string mountId)
        {
            return _equippedSkins.GetValueOrDefault(mountId, "default");
        }
        #endregion

        #region Save/Load
        private void SaveUnlockedMounts()
        {
            string data = string.Join(",", _unlockedMountIds);
            PlayerPrefs.SetString("UnlockedMounts", data);
            PlayerPrefs.Save();
        }

        private void LoadUnlockedMounts()
        {
            string data = PlayerPrefs.GetString("UnlockedMounts", "");
            if (!string.IsNullOrEmpty(data))
            {
                _unlockedMountIds = new HashSet<string>(data.Split(','));
            }
            
            // Always have starter mount unlocked
            if (_mounts.Count > 0)
            {
                _unlockedMountIds.Add(_mounts[0].mountId);
            }
            
            LoadEquippedSkins();
        }

        private void SaveEquippedSkins()
        {
            foreach (var kvp in _equippedSkins)
            {
                PlayerPrefs.SetString($"MountSkin_{kvp.Key}", kvp.Value);
            }
            PlayerPrefs.Save();
        }

        private void LoadEquippedSkins()
        {
            foreach (var mount in _mounts)
            {
                string skinId = PlayerPrefs.GetString($"MountSkin_{mount.mountId}", "default");
                _equippedSkins[mount.mountId] = skinId;
                
                // Load skin unlock status
                foreach (var skin in mount.skins)
                {
                    skin.isUnlocked = PlayerPrefs.GetInt($"MountSkin_{mount.mountId}_{skin.skinId}", 
                        skin.isDefault ? 1 : 0) == 1;
                }
            }
        }
        #endregion
    }

    #region Mount Data Classes
    public enum MountType
    {
        Land,       // Elephants, horses
        Water,      // Nagas, boats
        Flying      // Dragons, Garudas
    }

    public enum MountRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class Mount
    {
        [Header("Identity")]
        public string mountId;
        public string mountName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject prefab;
        
        [Header("Classification")]
        public MountType type;
        public MountRarity rarity;
        
        [Header("Stats")]
        public float speedMultiplier = 1.5f;
        public float stamina = 100f;
        public bool canJump = true;
        public bool canSwim = false;
        public bool canFly = false;
        
        [Header("Unlock")]
        public bool isStarterMount = false;
        public int unlockCost; // Gems
        public string unlockQuestId;
        
        [Header("Skins")]
        public List<MountSkin> skins = new List<MountSkin>();
    }

    [Serializable]
    public class MountSkin
    {
        public string skinId;
        public string skinName;
        public Sprite icon;
        public Material material;
        public bool isDefault = false;
        public bool isUnlocked = false;
        public int unlockCost;
    }
    #endregion
}

