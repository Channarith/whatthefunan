using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.RPG
{
    /// <summary>
    /// Manages character stats, leveling, and attributes.
    /// Supports multiple playable characters.
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        #region Events
        public static event Action<int> OnLevelUp;
        public static event Action<int> OnExperienceGained;
        public static event Action<float, float> OnHealthChanged; // current, max
        public static event Action OnCharacterDied;
        public static event Action<StatType, int> OnStatChanged;
        #endregion

        #region Stat Types
        public enum StatType
        {
            Health,
            Attack,
            Defense,
            Speed,
            CritChance,
            CritDamage
        }
        #endregion

        #region Character Data
        [Header("Character Info")]
        [SerializeField] private string _characterId = "Domrey";
        [SerializeField] private string _characterName = "Domrey the Elephant";
        [SerializeField] private CharacterClass _characterClass = CharacterClass.Warrior;
        
        public string CharacterId => _characterId;
        public string CharacterName => _characterName;
        public CharacterClass Class => _characterClass;
        
        public enum CharacterClass
        {
            Warrior,    // Elephant - Tank, high health
            Scout,      // Monkey - Agile, high speed
            Monk,       // Panda - Balanced, martial arts
            Mage,       // Naga - Magic, ranged
            Dancer      // Apsara - Support, healer
        }
        #endregion

        #region Level and Experience
        [Header("Level")]
        [SerializeField] private int _level = 1;
        [SerializeField] private int _experience = 0;
        [SerializeField] private int _maxLevel = 50;
        
        public int Level => _level;
        public int Experience => _experience;
        public int MaxLevel => _maxLevel;
        
        // Experience required for next level
        public int ExperienceToNextLevel => CalculateExperienceRequired(_level + 1);
        public float LevelProgress => (float)_experience / ExperienceToNextLevel;
        #endregion

        #region Base Stats
        [Header("Base Stats")]
        [SerializeField] private int _baseHealth = 100;
        [SerializeField] private int _baseAttack = 10;
        [SerializeField] private int _baseDefense = 5;
        [SerializeField] private int _baseSpeed = 5;
        [SerializeField] private float _baseCritChance = 0.05f;
        [SerializeField] private float _baseCritDamage = 1.5f;
        
        // Stat growth per level (varies by class)
        [Header("Stat Growth Per Level")]
        [SerializeField] private int _healthPerLevel = 10;
        [SerializeField] private int _attackPerLevel = 2;
        [SerializeField] private int _defensePerLevel = 1;
        [SerializeField] private int _speedPerLevel = 1;
        #endregion

        #region Current Stats
        private Dictionary<StatType, int> _stats = new Dictionary<StatType, int>();
        private Dictionary<StatType, int> _bonusStats = new Dictionary<StatType, int>();
        
        private float _currentHealth;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => GetStat(StatType.Health);
        public bool IsAlive => _currentHealth > 0;
        public float HealthPercent => _currentHealth / MaxHealth;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeStats();
        }

        private void Start()
        {
            // Set health to max on start
            _currentHealth = MaxHealth;
        }
        #endregion

        #region Initialization
        private void InitializeStats()
        {
            // Initialize stat dictionaries
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _stats[stat] = 0;
                _bonusStats[stat] = 0;
            }
            
            RecalculateStats();
        }

        private void RecalculateStats()
        {
            // Calculate stats based on level
            _stats[StatType.Health] = _baseHealth + (_healthPerLevel * (_level - 1));
            _stats[StatType.Attack] = _baseAttack + (_attackPerLevel * (_level - 1));
            _stats[StatType.Defense] = _baseDefense + (_defensePerLevel * (_level - 1));
            _stats[StatType.Speed] = _baseSpeed + (_speedPerLevel * (_level - 1));
            _stats[StatType.CritChance] = (int)(_baseCritChance * 100);
            _stats[StatType.CritDamage] = (int)(_baseCritDamage * 100);
            
            // Apply class modifiers
            ApplyClassModifiers();
        }

        private void ApplyClassModifiers()
        {
            switch (_characterClass)
            {
                case CharacterClass.Warrior:
                    _stats[StatType.Health] = (int)(_stats[StatType.Health] * 1.3f);
                    _stats[StatType.Defense] = (int)(_stats[StatType.Defense] * 1.2f);
                    break;
                    
                case CharacterClass.Scout:
                    _stats[StatType.Speed] = (int)(_stats[StatType.Speed] * 1.5f);
                    _stats[StatType.CritChance] = (int)(_stats[StatType.CritChance] * 1.3f);
                    break;
                    
                case CharacterClass.Monk:
                    // Balanced - no specific modifier
                    _stats[StatType.Attack] = (int)(_stats[StatType.Attack] * 1.1f);
                    _stats[StatType.Defense] = (int)(_stats[StatType.Defense] * 1.1f);
                    break;
                    
                case CharacterClass.Mage:
                    _stats[StatType.Attack] = (int)(_stats[StatType.Attack] * 1.4f);
                    _stats[StatType.Health] = (int)(_stats[StatType.Health] * 0.8f);
                    break;
                    
                case CharacterClass.Dancer:
                    _stats[StatType.Speed] = (int)(_stats[StatType.Speed] * 1.3f);
                    // Healer has support abilities
                    break;
            }
        }
        #endregion

        #region Stat Access
        /// <summary>
        /// Get the final value of a stat (base + bonus).
        /// </summary>
        public int GetStat(StatType stat)
        {
            return _stats.GetValueOrDefault(stat, 0) + _bonusStats.GetValueOrDefault(stat, 0);
        }

        /// <summary>
        /// Get the base value of a stat (without bonuses).
        /// </summary>
        public int GetBaseStat(StatType stat)
        {
            return _stats.GetValueOrDefault(stat, 0);
        }

        /// <summary>
        /// Get the bonus value of a stat (from equipment, buffs).
        /// </summary>
        public int GetBonusStat(StatType stat)
        {
            return _bonusStats.GetValueOrDefault(stat, 0);
        }

        /// <summary>
        /// Add a bonus to a stat (from equipment, buffs).
        /// </summary>
        public void AddStatBonus(StatType stat, int amount)
        {
            _bonusStats[stat] = _bonusStats.GetValueOrDefault(stat, 0) + amount;
            OnStatChanged?.Invoke(stat, GetStat(stat));
        }

        /// <summary>
        /// Remove a bonus from a stat.
        /// </summary>
        public void RemoveStatBonus(StatType stat, int amount)
        {
            _bonusStats[stat] = Mathf.Max(0, _bonusStats.GetValueOrDefault(stat, 0) - amount);
            OnStatChanged?.Invoke(stat, GetStat(stat));
        }

        /// <summary>
        /// Clear all stat bonuses.
        /// </summary>
        public void ClearAllBonuses()
        {
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _bonusStats[stat] = 0;
            }
        }
        #endregion

        #region Health
        /// <summary>
        /// Take damage, reduced by defense.
        /// </summary>
        public void TakeDamage(float rawDamage)
        {
            // Calculate damage reduction from defense
            float defense = GetStat(StatType.Defense);
            float damageReduction = defense / (defense + 100f); // Diminishing returns
            float finalDamage = rawDamage * (1f - damageReduction);
            
            _currentHealth -= finalDamage;
            _currentHealth = Mathf.Max(0, _currentHealth);
            
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the character.
        /// </summary>
        public void Heal(float amount)
        {
            _currentHealth += amount;
            _currentHealth = Mathf.Min(_currentHealth, MaxHealth);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        /// <summary>
        /// Heal to full health.
        /// </summary>
        public void FullHeal()
        {
            _currentHealth = MaxHealth;
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        /// <summary>
        /// Set health to a specific percentage (0-1).
        /// </summary>
        public void SetHealthPercent(float percent)
        {
            _currentHealth = MaxHealth * Mathf.Clamp01(percent);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        }

        private void Die()
        {
            OnCharacterDied?.Invoke();
            Debug.Log($"[CharacterStats] {_characterName} died");
        }
        #endregion

        #region Experience and Leveling
        /// <summary>
        /// Add experience points.
        /// </summary>
        public void AddExperience(int amount)
        {
            if (_level >= _maxLevel) return;
            
            _experience += amount;
            OnExperienceGained?.Invoke(amount);
            
            // Check for level up
            while (_experience >= ExperienceToNextLevel && _level < _maxLevel)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            _experience -= ExperienceToNextLevel;
            _level++;
            
            // Recalculate stats
            float healthPercent = HealthPercent;
            RecalculateStats();
            
            // Maintain health percentage
            _currentHealth = MaxHealth * healthPercent;
            
            OnLevelUp?.Invoke(_level);
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            
            // Haptic feedback
            Core.HapticManager.Instance?.OnLevelUp();
            
            Debug.Log($"[CharacterStats] {_characterName} leveled up to {_level}!");
        }

        /// <summary>
        /// Calculate experience required for a specific level.
        /// </summary>
        public int CalculateExperienceRequired(int level)
        {
            // Exponential curve: each level requires more XP
            return (int)(100 * Mathf.Pow(1.5f, level - 1));
        }

        /// <summary>
        /// Set level directly (for testing or loading save).
        /// </summary>
        public void SetLevel(int level)
        {
            _level = Mathf.Clamp(level, 1, _maxLevel);
            RecalculateStats();
            _currentHealth = MaxHealth;
        }
        #endregion

        #region Combat Calculations
        /// <summary>
        /// Calculate attack damage.
        /// </summary>
        public float CalculateDamage()
        {
            return GetStat(StatType.Attack);
        }

        /// <summary>
        /// Check if an attack is a critical hit.
        /// </summary>
        public bool RollCritical()
        {
            float critChance = GetStat(StatType.CritChance) / 100f;
            return UnityEngine.Random.value <= critChance;
        }

        /// <summary>
        /// Get the critical damage multiplier.
        /// </summary>
        public float GetCritMultiplier()
        {
            return GetStat(StatType.CritDamage) / 100f;
        }
        #endregion

        #region Persistence
        /// <summary>
        /// Get data for saving.
        /// </summary>
        public CharacterSaveData GetSaveData()
        {
            return new CharacterSaveData
            {
                characterId = _characterId,
                level = _level,
                experience = _experience,
                currentHealthPercent = HealthPercent
            };
        }

        /// <summary>
        /// Load data from save.
        /// </summary>
        public void LoadSaveData(CharacterSaveData data)
        {
            if (data.characterId != _characterId)
            {
                Debug.LogWarning("[CharacterStats] Save data character mismatch");
                return;
            }
            
            _level = data.level;
            _experience = data.experience;
            RecalculateStats();
            SetHealthPercent(data.currentHealthPercent);
        }

        [Serializable]
        public class CharacterSaveData
        {
            public string characterId;
            public int level;
            public int experience;
            public float currentHealthPercent;
        }
        #endregion
    }
}

