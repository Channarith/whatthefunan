using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Features
{
    /// <summary>
    /// Character emote system for expressions and dances.
    /// Unlockable emotes through gameplay and purchases.
    /// </summary>
    public class EmoteSystem : MonoBehaviour
    {
        #region Singleton
        private static EmoteSystem _instance;
        public static EmoteSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Emote> OnEmotePlayed;
        public static event Action<Emote> OnEmoteUnlocked;
        public static event Action OnEmoteCancelled;
        #endregion

        #region Emote Data
        [Header("Emotes")]
        [SerializeField] private List<Emote> _emotes = new List<Emote>();
        
        private Dictionary<string, Emote> _emoteLookup = new Dictionary<string, Emote>();
        private HashSet<string> _unlockedEmoteIds = new HashSet<string>();
        private List<string> _equippedEmoteIds = new List<string>();
        
        public IReadOnlyList<Emote> AllEmotes => _emotes;
        public IReadOnlyList<string> EquippedEmoteIds => _equippedEmoteIds;
        
        [Header("Wheel Settings")]
        [SerializeField] private int _emoteWheelSlots = 8;
        #endregion

        #region State
        private Emote _currentEmote;
        private bool _isPlaying;
        private Animator _characterAnimator;
        private float _emoteTimer;
        
        public bool IsPlaying => _isPlaying;
        public Emote CurrentEmote => _currentEmote;
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
            
            InitializeEmotes();
            LoadData();
        }

        private void Update()
        {
            if (_isPlaying)
            {
                UpdateEmote();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeEmotes()
        {
            _emoteLookup.Clear();
            foreach (var emote in _emotes)
            {
                _emoteLookup[emote.emoteId] = emote;
            }
        }
        #endregion

        #region Emote Playback
        /// <summary>
        /// Play an emote by ID.
        /// </summary>
        public bool PlayEmote(string emoteId)
        {
            if (!_emoteLookup.TryGetValue(emoteId, out Emote emote))
            {
                Debug.LogWarning($"[EmoteSystem] Emote not found: {emoteId}");
                return false;
            }
            
            return PlayEmote(emote);
        }

        /// <summary>
        /// Play an emote.
        /// </summary>
        public bool PlayEmote(Emote emote)
        {
            if (emote == null) return false;
            
            if (!_unlockedEmoteIds.Contains(emote.emoteId))
            {
                Debug.LogWarning($"[EmoteSystem] Emote not unlocked: {emote.emoteId}");
                return false;
            }
            
            // Stop current emote if playing
            if (_isPlaying)
            {
                StopEmote();
            }
            
            _currentEmote = emote;
            _isPlaying = true;
            _emoteTimer = 0f;
            
            // Play animation
            if (_characterAnimator != null && !string.IsNullOrEmpty(emote.animationTrigger))
            {
                _characterAnimator.SetTrigger(emote.animationTrigger);
            }
            
            // Play sound
            if (emote.soundClip != null)
            {
                Core.AudioManager.Instance?.PlaySFX("emote", emote.soundClip);
            }
            
            // Spawn particles
            if (emote.particleEffect != null)
            {
                // Instantiate particle effect on character
            }
            
            OnEmotePlayed?.Invoke(emote);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            Debug.Log($"[EmoteSystem] Playing emote: {emote.emoteName}");
            return true;
        }

        /// <summary>
        /// Play an equipped emote by wheel slot.
        /// </summary>
        public bool PlayEquippedEmote(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _equippedEmoteIds.Count)
            {
                return false;
            }
            
            return PlayEmote(_equippedEmoteIds[slotIndex]);
        }

        /// <summary>
        /// Stop the current emote.
        /// </summary>
        public void StopEmote()
        {
            if (!_isPlaying) return;
            
            // Return to idle animation
            if (_characterAnimator != null)
            {
                _characterAnimator.SetTrigger("Idle");
            }
            
            _currentEmote = null;
            _isPlaying = false;
            
            OnEmoteCancelled?.Invoke();
        }

        private void UpdateEmote()
        {
            if (_currentEmote == null) return;
            
            _emoteTimer += Time.deltaTime;
            
            // Check if emote finished
            if (!_currentEmote.isLooping && _emoteTimer >= _currentEmote.duration)
            {
                StopEmote();
            }
        }

        /// <summary>
        /// Set the character animator reference.
        /// </summary>
        public void SetCharacterAnimator(Animator animator)
        {
            _characterAnimator = animator;
        }
        #endregion

        #region Emote Wheel
        /// <summary>
        /// Equip an emote to the wheel.
        /// </summary>
        public bool EquipEmote(string emoteId, int slotIndex)
        {
            if (!_unlockedEmoteIds.Contains(emoteId))
            {
                return false;
            }
            
            if (slotIndex < 0 || slotIndex >= _emoteWheelSlots)
            {
                return false;
            }
            
            // Ensure list is large enough
            while (_equippedEmoteIds.Count <= slotIndex)
            {
                _equippedEmoteIds.Add("");
            }
            
            _equippedEmoteIds[slotIndex] = emoteId;
            SaveData();
            
            return true;
        }

        /// <summary>
        /// Unequip an emote from the wheel.
        /// </summary>
        public void UnequipEmote(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _equippedEmoteIds.Count)
            {
                _equippedEmoteIds[slotIndex] = "";
                SaveData();
            }
        }

        /// <summary>
        /// Get equipped emotes.
        /// </summary>
        public List<Emote> GetEquippedEmotes()
        {
            var result = new List<Emote>();
            
            for (int i = 0; i < _emoteWheelSlots; i++)
            {
                if (i < _equippedEmoteIds.Count && !string.IsNullOrEmpty(_equippedEmoteIds[i]))
                {
                    result.Add(_emoteLookup.GetValueOrDefault(_equippedEmoteIds[i], null));
                }
                else
                {
                    result.Add(null);
                }
            }
            
            return result;
        }
        #endregion

        #region Unlock
        /// <summary>
        /// Unlock an emote.
        /// </summary>
        public bool UnlockEmote(string emoteId)
        {
            if (_unlockedEmoteIds.Contains(emoteId))
            {
                return false;
            }
            
            if (!_emoteLookup.TryGetValue(emoteId, out Emote emote))
            {
                return false;
            }
            
            _unlockedEmoteIds.Add(emoteId);
            SaveData();
            
            OnEmoteUnlocked?.Invoke(emote);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[EmoteSystem] Unlocked emote: {emote.emoteName}");
            return true;
        }

        /// <summary>
        /// Check if an emote is unlocked.
        /// </summary>
        public bool IsUnlocked(string emoteId)
        {
            return _unlockedEmoteIds.Contains(emoteId);
        }

        /// <summary>
        /// Get unlocked emotes.
        /// </summary>
        public List<Emote> GetUnlockedEmotes()
        {
            return _emotes.Where(e => _unlockedEmoteIds.Contains(e.emoteId)).ToList();
        }

        /// <summary>
        /// Get emotes by category.
        /// </summary>
        public List<Emote> GetEmotesByCategory(EmoteCategory category)
        {
            return _emotes.Where(e => e.category == category).ToList();
        }
        #endregion

        #region Save/Load
        private void SaveData()
        {
            string unlocked = string.Join(",", _unlockedEmoteIds);
            PlayerPrefs.SetString("Emotes_Unlocked", unlocked);
            
            string equipped = string.Join(",", _equippedEmoteIds);
            PlayerPrefs.SetString("Emotes_Equipped", equipped);
            
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            // Load unlocked
            string unlocked = PlayerPrefs.GetString("Emotes_Unlocked", "");
            if (!string.IsNullOrEmpty(unlocked))
            {
                _unlockedEmoteIds = new HashSet<string>(unlocked.Split(','));
            }
            
            // Always have starter emotes unlocked
            foreach (var emote in _emotes)
            {
                if (emote.isStarter)
                {
                    _unlockedEmoteIds.Add(emote.emoteId);
                }
            }
            
            // Load equipped
            string equipped = PlayerPrefs.GetString("Emotes_Equipped", "");
            if (!string.IsNullOrEmpty(equipped))
            {
                _equippedEmoteIds = new List<string>(equipped.Split(','));
            }
            
            // Ensure minimum slots
            while (_equippedEmoteIds.Count < _emoteWheelSlots)
            {
                _equippedEmoteIds.Add("");
            }
        }
        #endregion
    }

    #region Emote Data Classes
    public enum EmoteCategory
    {
        Expression,     // Wave, thumbs up, etc.
        Dance,          // Apsara dances, etc.
        Action,         // Sit, sleep, etc.
        Celebration,    // Victory, cheers
        Seasonal        // Event-specific
    }

    public enum EmoteRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class Emote
    {
        [Header("Identity")]
        public string emoteId;
        public string emoteName;
        [TextArea] public string description;
        public Sprite icon;
        
        [Header("Classification")]
        public EmoteCategory category;
        public EmoteRarity rarity;
        
        [Header("Animation")]
        public string animationTrigger;
        public AnimationClip animationClip;
        public float duration = 2f;
        public bool isLooping = false;
        
        [Header("Effects")]
        public AudioClip soundClip;
        public GameObject particleEffect;
        
        [Header("Unlock")]
        public bool isStarter = false;
        public int unlockCost; // Gems
        public string unlockCondition;
    }
    #endregion
}

