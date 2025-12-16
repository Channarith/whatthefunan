using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Manages the player's kingdom including stats, villagers, and progression.
    /// </summary>
    public class KingdomManager : MonoBehaviour
    {
        #region Singleton
        private static KingdomManager _instance;
        public static KingdomManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<int> OnKingdomLevelUp;
        public static event Action<KingdomStats> OnStatsChanged;
        public static event Action<Villager> OnVillagerArrived;
        public static event Action<string> OnBuildingUnlocked;
        #endregion

        #region Kingdom Data
        [Serializable]
        public class KingdomData
        {
            public string kingdomName = "My Kingdom";
            public int level = 1;
            public int experience = 0;
            public int prosperity = 0;
            public int happiness = 50;
            public int population = 0;
            public int maxPopulation = 10;
            public List<string> unlockedBuildings = new List<string>();
            public List<VillagerData> villagers = new List<VillagerData>();
            public DateTime lastVisit;
            public float totalPlayTime;
        }
        #endregion

        #region Stats
        [Serializable]
        public class KingdomStats
        {
            public int totalBuildings;
            public int decorationCount;
            public int functionalCount;
            public int housingCapacity;
            public int storageCapacity;
            public float beautyScore;
            public float efficiencyScore;
        }
        #endregion

        #region Settings
        [Header("Level Progression")]
        [SerializeField] private int[] _levelExpRequirements = { 100, 250, 500, 1000, 2000, 4000, 8000, 15000, 30000, 50000 };
        [SerializeField] private int[] _populationPerLevel = { 10, 15, 20, 30, 40, 55, 70, 90, 120, 150 };
        
        [Header("Happiness Modifiers")]
        [SerializeField] private float _decorationHappinessBonus = 0.5f;
        [SerializeField] private float _overcrowdingPenalty = 2f;
        [SerializeField] private float _beautyMultiplier = 0.1f;
        
        [Header("Resource Generation")]
        [SerializeField] private float _baseIncomePerVillager = 5f;
        [SerializeField] private float _happinessIncomeMultiplier = 0.02f;
        #endregion

        #region State
        private KingdomData _data = new KingdomData();
        private KingdomStats _stats = new KingdomStats();
        private float _offlineEarningsAccumulator;
        
        public KingdomData Data => _data;
        public KingdomStats Stats => _stats;
        public int Level => _data.level;
        public int Population => _data.population;
        public int Happiness => _data.happiness;
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

        private void Start()
        {
            LoadKingdom();
            CalculateOfflineProgress();
            CalculateStats();
        }

        private void Update()
        {
            _data.totalPlayTime += Time.deltaTime;
            
            // Periodic stats update
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                CalculateStats();
                UpdateHappiness();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _data.lastVisit = DateTime.Now;
                SaveKingdom();
            }
            else
            {
                CalculateOfflineProgress();
            }
        }

        private void OnApplicationQuit()
        {
            _data.lastVisit = DateTime.Now;
            SaveKingdom();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Kingdom Management
        /// <summary>
        /// Set the kingdom name.
        /// </summary>
        public void SetKingdomName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            
            // Content filter for children's game
            _data.kingdomName = SanitizeName(name);
            SaveKingdom();
        }

        private string SanitizeName(string name)
        {
            // Remove inappropriate content
            name = name.Trim();
            if (name.Length > 20) name = name.Substring(0, 20);
            return name;
        }

        /// <summary>
        /// Add experience to the kingdom.
        /// </summary>
        public void AddExperience(int amount)
        {
            _data.experience += amount;
            
            // Check for level up
            while (_data.level < _levelExpRequirements.Length && 
                   _data.experience >= GetExpForNextLevel())
            {
                LevelUp();
            }
            
            OnStatsChanged?.Invoke(_stats);
        }

        private void LevelUp()
        {
            _data.level++;
            _data.maxPopulation = _populationPerLevel[Mathf.Min(_data.level - 1, _populationPerLevel.Length - 1)];
            
            // Unlock new buildings
            UnlockBuildingsForLevel(_data.level);
            
            OnKingdomLevelUp?.Invoke(_data.level);
            Core.AudioManager.Instance?.PlaySFX("sfx_level_up");
            Core.HapticManager.Instance?.TriggerHeavy();
            
            // Show notification
            Notifications.NotificationManager.Instance?.ShowNotification(
                $"Kingdom Level {_data.level}!",
                "New buildings unlocked!");
            
            Debug.Log($"[KingdomManager] Kingdom leveled up to {_data.level}!");
        }

        private int GetExpForNextLevel()
        {
            if (_data.level >= _levelExpRequirements.Length)
                return int.MaxValue;
            return _levelExpRequirements[_data.level - 1];
        }

        private void UnlockBuildingsForLevel(int level)
        {
            // Would check BuildingDatabase for level-gated buildings
            var newBuildings = BuildingDatabase.Instance?.GetBuildingsForLevel(level);
            if (newBuildings != null)
            {
                foreach (var building in newBuildings)
                {
                    if (!_data.unlockedBuildings.Contains(building.ObjectId))
                    {
                        _data.unlockedBuildings.Add(building.ObjectId);
                        OnBuildingUnlocked?.Invoke(building.ObjectId);
                    }
                }
            }
        }
        #endregion

        #region Stats Calculation
        private void CalculateStats()
        {
            var placedObjects = BuildingSystem.Instance?.GetPlacedObjects();
            if (placedObjects == null) return;
            
            _stats.totalBuildings = placedObjects.Count;
            _stats.decorationCount = 0;
            _stats.functionalCount = 0;
            _stats.housingCapacity = 0;
            _stats.storageCapacity = 0;
            _stats.beautyScore = 0;
            _stats.efficiencyScore = 0;
            
            foreach (var placed in placedObjects)
            {
                var buildable = BuildingDatabase.Instance?.GetBuildable(placed.objectId);
                if (buildable == null) continue;
                
                switch (buildable.Category)
                {
                    case BuildableObject.BuildCategory.Decorations:
                        _stats.decorationCount++;
                        _stats.beautyScore += GetBeautyValue(buildable);
                        break;
                    case BuildableObject.BuildCategory.Functional:
                        _stats.functionalCount++;
                        _stats.efficiencyScore += 1;
                        break;
                    case BuildableObject.BuildCategory.Structures:
                        _stats.housingCapacity += GetHousingValue(buildable);
                        break;
                }
            }
            
            // Normalize scores
            _stats.beautyScore = Mathf.Clamp(_stats.beautyScore / 100f, 0, 1);
            _stats.efficiencyScore = Mathf.Clamp(_stats.efficiencyScore / 50f, 0, 1);
            
            // Update max population based on housing
            _data.maxPopulation = Mathf.Max(
                _populationPerLevel[Mathf.Min(_data.level - 1, _populationPerLevel.Length - 1)],
                _stats.housingCapacity);
            
            OnStatsChanged?.Invoke(_stats);
        }

        private float GetBeautyValue(BuildableObject buildable)
        {
            // Rarity increases beauty
            return buildable.Rarity switch
            {
                BuildableObject.BuildRarity.Common => 1f,
                BuildableObject.BuildRarity.Uncommon => 2f,
                BuildableObject.BuildRarity.Rare => 4f,
                BuildableObject.BuildRarity.Epic => 8f,
                BuildableObject.BuildRarity.Legendary => 15f,
                _ => 1f
            };
        }

        private int GetHousingValue(BuildableObject buildable)
        {
            // Would be defined in buildable data
            return buildable.Rarity switch
            {
                BuildableObject.BuildRarity.Common => 2,
                BuildableObject.BuildRarity.Uncommon => 3,
                BuildableObject.BuildRarity.Rare => 5,
                BuildableObject.BuildRarity.Epic => 8,
                BuildableObject.BuildRarity.Legendary => 12,
                _ => 2
            };
        }
        #endregion

        #region Happiness
        private void UpdateHappiness()
        {
            int targetHappiness = 50; // Base happiness
            
            // Decoration bonus
            targetHappiness += Mathf.FloorToInt(_stats.decorationCount * _decorationHappinessBonus);
            
            // Beauty bonus
            targetHappiness += Mathf.FloorToInt(_stats.beautyScore * 100 * _beautyMultiplier);
            
            // Overcrowding penalty
            if (_data.population > _data.maxPopulation * 0.9f)
            {
                float overcrowdingRatio = (float)_data.population / _data.maxPopulation;
                targetHappiness -= Mathf.FloorToInt((overcrowdingRatio - 0.9f) * 100 * _overcrowdingPenalty);
            }
            
            // Clamp and smooth
            targetHappiness = Mathf.Clamp(targetHappiness, 0, 100);
            _data.happiness = Mathf.RoundToInt(Mathf.Lerp(_data.happiness, targetHappiness, 0.1f));
        }

        /// <summary>
        /// Get happiness emoji for UI.
        /// </summary>
        public string GetHappinessEmoji()
        {
            if (_data.happiness >= 80) return "😄";
            if (_data.happiness >= 60) return "🙂";
            if (_data.happiness >= 40) return "😐";
            if (_data.happiness >= 20) return "😟";
            return "😢";
        }
        #endregion

        #region Villagers
        /// <summary>
        /// Try to attract a new villager.
        /// </summary>
        public bool TryAttractVillager()
        {
            if (_data.population >= _data.maxPopulation)
            {
                Debug.Log("[KingdomManager] Kingdom at max population");
                return false;
            }
            
            // Happiness affects chance
            float baseChance = 0.3f;
            float happinessBonus = _data.happiness / 200f;
            float chance = baseChance + happinessBonus;
            
            if (UnityEngine.Random.value < chance)
            {
                SpawnVillager();
                return true;
            }
            
            return false;
        }

        private void SpawnVillager()
        {
            var villagerData = new VillagerData
            {
                id = Guid.NewGuid().ToString(),
                name = GenerateVillagerName(),
                species = GetRandomSpecies(),
                job = VillagerJob.Idle,
                happiness = _data.happiness,
                arrivalDate = DateTime.Now
            };
            
            _data.villagers.Add(villagerData);
            _data.population = _data.villagers.Count;
            
            OnVillagerArrived?.Invoke(new Villager(villagerData));
            
            Debug.Log($"[KingdomManager] New villager arrived: {villagerData.name} the {villagerData.species}");
        }

        private string GenerateVillagerName()
        {
            // Khmer-inspired names
            string[] names = { 
                "Sokha", "Dara", "Phirun", "Channa", "Mony",
                "Srey", "Kosal", "Bopha", "Rith", "Malis",
                "Leap", "Keo", "Sovan", "Kunthea", "Rithy"
            };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }

        private string GetRandomSpecies()
        {
            string[] species = { "Elephant", "Monkey", "Bird", "Frog", "Rabbit", "Cat" };
            return species[UnityEngine.Random.Range(0, species.Length)];
        }

        /// <summary>
        /// Assign a job to a villager.
        /// </summary>
        public void AssignJob(string villagerId, VillagerJob job)
        {
            var villager = _data.villagers.Find(v => v.id == villagerId);
            if (villager != null)
            {
                villager.job = job;
            }
        }
        #endregion

        #region Offline Progress
        private void CalculateOfflineProgress()
        {
            if (_data.lastVisit == default) return;
            
            TimeSpan offlineTime = DateTime.Now - _data.lastVisit;
            
            // Cap at 24 hours
            float hours = Mathf.Min((float)offlineTime.TotalHours, 24f);
            
            if (hours < 0.01f) return;
            
            // Calculate earnings
            float incomePerHour = _data.population * _baseIncomePerVillager * 
                                  (1 + _data.happiness * _happinessIncomeMultiplier);
            
            int offlineCoins = Mathf.FloorToInt(incomePerHour * hours);
            
            if (offlineCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(offlineCoins);
                
                // Show welcome back popup
                Notifications.NotificationManager.Instance?.ShowNotification(
                    "Welcome Back!",
                    $"Your villagers earned {offlineCoins} coins while you were away!");
                
                Debug.Log($"[KingdomManager] Offline earnings: {offlineCoins} coins for {hours:F1} hours");
            }
        }
        #endregion

        #region Save/Load
        private void SaveKingdom()
        {
            string json = JsonUtility.ToJson(_data);
            PlayerPrefs.SetString("KingdomData", json);
            PlayerPrefs.Save();
        }

        private void LoadKingdom()
        {
            string json = PlayerPrefs.GetString("KingdomData", "");
            if (!string.IsNullOrEmpty(json))
            {
                _data = JsonUtility.FromJson<KingdomData>(json);
            }
            else
            {
                // New kingdom
                _data = new KingdomData();
                _data.unlockedBuildings.Add("foundation_wood");
                _data.unlockedBuildings.Add("wall_bamboo");
                _data.unlockedBuildings.Add("roof_thatch");
            }
        }

        /// <summary>
        /// Reset kingdom (with confirmation).
        /// </summary>
        public void ResetKingdom()
        {
            _data = new KingdomData();
            _data.unlockedBuildings.Add("foundation_wood");
            BuildingSystem.Instance?.ClearAllObjects();
            SaveKingdom();
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class VillagerData
    {
        public string id;
        public string name;
        public string species;
        public VillagerJob job;
        public int happiness;
        public DateTime arrivalDate;
    }

    public enum VillagerJob
    {
        Idle,
        Farmer,
        Builder,
        Crafter,
        Guard,
        Merchant,
        Entertainer
    }

    public class Villager
    {
        public VillagerData Data { get; private set; }
        
        public Villager(VillagerData data)
        {
            Data = data;
        }
    }
    #endregion
}

