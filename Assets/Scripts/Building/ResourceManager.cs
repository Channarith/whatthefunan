using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Manages building resources (wood, stone, etc.) for the kingdom builder.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region Singleton
        private static ResourceManager _instance;
        public static ResourceManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<string, int> OnResourceChanged;
        public static event Action<string, int> OnResourceGathered;
        public static event Action OnStorageFull;
        #endregion

        #region Resource Definitions
        [Serializable]
        public class ResourceDefinition
        {
            public string resourceId;
            public string displayName;
            public Sprite icon;
            public Color color;
            public ResourceType type;
            public int maxStack = 999;
            public bool isStorable = true;
        }

        public enum ResourceType
        {
            Basic,          // Wood, Stone, etc.
            Refined,        // Planks, Bricks, etc.
            Precious,       // Gold, Gems, etc.
            Special,        // Event items, rare materials
            Seasonal        // Limited time resources
        }
        #endregion

        #region Settings
        [Header("Resource Definitions")]
        [SerializeField] private List<ResourceDefinition> _resourceDefinitions = new List<ResourceDefinition>
        {
            // =================================================================
            // SACRED PLANT MATERIALS
            // =================================================================
            new ResourceDefinition { resourceId = "spirit_bamboo", displayName = "Spirit Bamboo", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "blessed_lotus_fiber", displayName = "Blessed Lotus Fiber", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "moonlit_palm", displayName = "Moonlit Palm Frond", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "sacred_banyan_root", displayName = "Sacred Banyan Root", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "whisper_vine", displayName = "Whisper Vine", type = ResourceType.Basic },
            
            // =================================================================
            // MYSTICAL EARTH MATERIALS
            // =================================================================
            new ResourceDefinition { resourceId = "sacred_mud", displayName = "Sacred Temple Mud", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "blessed_clay", displayName = "Blessed Riverside Clay", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "ancestor_ash", displayName = "Ancestor Ash", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "blessed_thatch", displayName = "Blessed Temple Straw", type = ResourceType.Basic },
            
            // =================================================================
            // CELESTIAL STONES
            // =================================================================
            new ResourceDefinition { resourceId = "star_stone", displayName = "Star Stone", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "temple_granite", displayName = "Temple Granite", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "naga_pearl_stone", displayName = "Naga Pearl Stone", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "volcano_obsidian", displayName = "Dragon's Obsidian", type = ResourceType.Refined },
            
            // =================================================================
            // CREATURE MATERIALS (Gifted, never harmed)
            // =================================================================
            new ResourceDefinition { resourceId = "naga_scale", displayName = "Shed Naga Scale", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "makara_hide", displayName = "Makara Dragon Hide", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "sena_mane_thread", displayName = "Sena Mane Thread", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "elephant_blessing_ivory", displayName = "Blessed Ivory Fragment", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "apsara_feather", displayName = "Apsara Celestial Feather", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "prohm_fossil_shard", displayName = "Prohm's Fossil Shard", type = ResourceType.Special },
            
            // =================================================================
            // ENCHANTED MATERIALS
            // =================================================================
            new ResourceDefinition { resourceId = "sunforged_bronze", displayName = "Sunforged Bronze", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "moonwoven_silk", displayName = "Moonwoven Silk", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "spirit_lacquer", displayName = "Spirit Lacquer", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "spirit_rope", displayName = "Spirit-Woven Rope", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "sacred_plank", displayName = "Sacred Spirit Planks", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "temple_brick", displayName = "Temple Brick", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "celestial_tile", displayName = "Celestial Roof Tile", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "carved_relic_stone", displayName = "Carved Relic Stone", type = ResourceType.Refined },
            
            // =================================================================
            // LEGENDARY MATERIALS
            // =================================================================
            new ResourceDefinition { resourceId = "celestial_jade", displayName = "Celestial Jade", type = ResourceType.Precious },
            new ResourceDefinition { resourceId = "dragon_gold", displayName = "Dragon-Touched Gold", type = ResourceType.Precious },
            new ResourceDefinition { resourceId = "ancient_relic_shard", displayName = "Ancient Relic Shard", type = ResourceType.Precious },
            new ResourceDefinition { resourceId = "naga_king_tear", displayName = "Tear of the Naga King", type = ResourceType.Precious }
        };
        
        [Header("Storage")]
        [SerializeField] private int _baseStorageCapacity = 500;
        [SerializeField] private int _storagePerChest = 100;
        #endregion

        #region State
        private Dictionary<string, int> _resources = new Dictionary<string, int>();
        private int _totalStorageCapacity;
        private int _currentStorageUsed;
        
        public int StorageCapacity => _totalStorageCapacity;
        public int StorageUsed => _currentStorageUsed;
        public float StoragePercent => _totalStorageCapacity > 0 ? (float)_currentStorageUsed / _totalStorageCapacity : 0;
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
            
            LoadResources();
            CalculateStorage();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            SaveResources();
        }
        #endregion

        #region Resource Access
        /// <summary>
        /// Get current count of a resource.
        /// </summary>
        public int GetResourceCount(string resourceId)
        {
            return _resources.TryGetValue(resourceId, out int count) ? count : 0;
        }

        /// <summary>
        /// Get resource definition.
        /// </summary>
        public ResourceDefinition GetDefinition(string resourceId)
        {
            return _resourceDefinitions.Find(r => r.resourceId == resourceId);
        }

        /// <summary>
        /// Get all resources with counts > 0.
        /// </summary>
        public Dictionary<string, int> GetAllResources()
        {
            return new Dictionary<string, int>(_resources);
        }

        /// <summary>
        /// Check if player has enough of a resource.
        /// </summary>
        public bool HasResource(string resourceId, int amount)
        {
            return GetResourceCount(resourceId) >= amount;
        }

        /// <summary>
        /// Check if player has all required resources.
        /// </summary>
        public bool HasResources(List<ResourceCost> costs)
        {
            foreach (var cost in costs)
            {
                if (!HasResource(cost.resourceId, cost.amount))
                    return false;
            }
            return true;
        }
        #endregion

        #region Resource Modification
        /// <summary>
        /// Add resources.
        /// </summary>
        public bool AddResource(string resourceId, int amount)
        {
            if (amount <= 0) return false;
            
            // Check storage
            var def = GetDefinition(resourceId);
            if (def != null && def.isStorable && _currentStorageUsed + amount > _totalStorageCapacity)
            {
                OnStorageFull?.Invoke();
                
                // Add what we can
                int canAdd = _totalStorageCapacity - _currentStorageUsed;
                if (canAdd <= 0) return false;
                amount = canAdd;
            }
            
            if (!_resources.ContainsKey(resourceId))
            {
                _resources[resourceId] = 0;
            }
            
            _resources[resourceId] += amount;
            
            // Apply max stack
            if (def != null)
            {
                _resources[resourceId] = Mathf.Min(_resources[resourceId], def.maxStack);
            }
            
            _currentStorageUsed += amount;
            
            OnResourceChanged?.Invoke(resourceId, _resources[resourceId]);
            OnResourceGathered?.Invoke(resourceId, amount);
            
            return true;
        }

        /// <summary>
        /// Consume resources.
        /// </summary>
        public bool ConsumeResource(string resourceId, int amount)
        {
            if (amount <= 0) return false;
            
            if (!HasResource(resourceId, amount))
            {
                Debug.LogWarning($"[ResourceManager] Not enough {resourceId}");
                return false;
            }
            
            _resources[resourceId] -= amount;
            _currentStorageUsed -= amount;
            
            if (_resources[resourceId] <= 0)
            {
                _resources.Remove(resourceId);
            }
            
            OnResourceChanged?.Invoke(resourceId, GetResourceCount(resourceId));
            
            return true;
        }

        /// <summary>
        /// Consume multiple resources at once.
        /// </summary>
        public bool ConsumeResources(List<ResourceCost> costs)
        {
            // First check if we have all
            if (!HasResources(costs))
            {
                return false;
            }
            
            // Then consume
            foreach (var cost in costs)
            {
                ConsumeResource(cost.resourceId, cost.amount);
            }
            
            return true;
        }

        /// <summary>
        /// Set a resource to a specific value.
        /// </summary>
        public void SetResource(string resourceId, int amount)
        {
            int current = GetResourceCount(resourceId);
            _currentStorageUsed -= current;
            
            _resources[resourceId] = Mathf.Max(0, amount);
            _currentStorageUsed += _resources[resourceId];
            
            if (_resources[resourceId] <= 0)
            {
                _resources.Remove(resourceId);
            }
            
            OnResourceChanged?.Invoke(resourceId, GetResourceCount(resourceId));
        }
        #endregion

        #region Storage
        /// <summary>
        /// Calculate total storage capacity.
        /// </summary>
        public void CalculateStorage()
        {
            _totalStorageCapacity = _baseStorageCapacity;
            
            // Count storage buildings
            var placedObjects = BuildingSystem.Instance?.GetPlacedObjects();
            if (placedObjects != null)
            {
                foreach (var obj in placedObjects)
                {
                    if (obj.objectId.Contains("chest") || obj.objectId.Contains("storage"))
                    {
                        _totalStorageCapacity += _storagePerChest;
                    }
                }
            }
            
            // Update used storage
            _currentStorageUsed = 0;
            foreach (var kvp in _resources)
            {
                var def = GetDefinition(kvp.Key);
                if (def != null && def.isStorable)
                {
                    _currentStorageUsed += kvp.Value;
                }
            }
        }

        /// <summary>
        /// Increase base storage (from upgrades).
        /// </summary>
        public void IncreaseBaseStorage(int amount)
        {
            _baseStorageCapacity += amount;
            CalculateStorage();
        }
        #endregion

        #region Crafting
        /// <summary>
        /// Craft a refined resource from basic resources.
        /// </summary>
        public bool CraftResource(string outputId, int amount = 1)
        {
            var recipe = GetCraftingRecipe(outputId);
            if (recipe == null)
            {
                Debug.LogWarning($"[ResourceManager] No recipe for {outputId}");
                return false;
            }
            
            // Scale recipe by amount
            var scaledInputs = new List<ResourceCost>();
            foreach (var input in recipe.inputs)
            {
                scaledInputs.Add(new ResourceCost 
                { 
                    resourceId = input.resourceId, 
                    amount = input.amount * amount 
                });
            }
            
            if (!HasResources(scaledInputs))
            {
                return false;
            }
            
            // Consume inputs
            ConsumeResources(scaledInputs);
            
            // Add output
            AddResource(outputId, recipe.outputAmount * amount);
            
            Core.AudioManager.Instance?.PlaySFX("sfx_craft");
            
            return true;
        }

        /// <summary>
        /// Get crafting recipe for a resource.
        /// </summary>
        public CraftingRecipe GetCraftingRecipe(string outputId)
        {
            // Mystical crafting recipes
            return outputId switch
            {
                "sacred_plank" => new CraftingRecipe
                {
                    outputId = "sacred_plank",
                    outputAmount = 6,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "spirit_bamboo", amount = 4 }
                    }
                },
                "temple_brick" => new CraftingRecipe
                {
                    outputId = "temple_brick",
                    outputAmount = 8,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "blessed_clay", amount = 4 },
                        new ResourceCost { resourceId = "ancestor_ash", amount = 1 }
                    }
                },
                "carved_relic_stone" => new CraftingRecipe
                {
                    outputId = "carved_relic_stone",
                    outputAmount = 1,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "temple_granite", amount = 4 },
                        new ResourceCost { resourceId = "star_stone", amount = 2 }
                    }
                },
                "spirit_rope" => new CraftingRecipe
                {
                    outputId = "spirit_rope",
                    outputAmount = 4,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "whisper_vine", amount = 3 },
                        new ResourceCost { resourceId = "blessed_lotus_fiber", amount = 2 }
                    }
                },
                "celestial_tile" => new CraftingRecipe
                {
                    outputId = "celestial_tile",
                    outputAmount = 4,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "blessed_clay", amount = 3 },
                        new ResourceCost { resourceId = "star_stone", amount = 1 }
                    }
                },
                "moonwoven_silk" => new CraftingRecipe
                {
                    outputId = "moonwoven_silk",
                    outputAmount = 2,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "blessed_lotus_fiber", amount = 6 },
                        new ResourceCost { resourceId = "apsara_feather", amount = 1 }
                    }
                },
                "sunforged_bronze" => new CraftingRecipe
                {
                    outputId = "sunforged_bronze",
                    outputAmount = 3,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "star_stone", amount = 2 },
                        new ResourceCost { resourceId = "volcano_obsidian", amount = 1 }
                    }
                },
                "spirit_lacquer" => new CraftingRecipe
                {
                    outputId = "spirit_lacquer",
                    outputAmount = 4,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "sacred_banyan_root", amount = 2 },
                        new ResourceCost { resourceId = "ancestor_ash", amount = 1 }
                    }
                },
                _ => null
            };
        }
        #endregion

        #region Gathering
        /// <summary>
        /// Gather resources from a source.
        /// </summary>
        public void GatherFromSource(ResourceSource source)
        {
            if (source == null || source.IsEmpty) return;
            
            int gathered = source.Gather();
            if (gathered > 0)
            {
                AddResource(source.ResourceId, gathered);
                
                Core.AudioManager.Instance?.PlaySFX("sfx_gather");
                Core.HapticManager.Instance?.TriggerLight();
            }
        }
        #endregion

        #region Save/Load
        private void SaveResources()
        {
            string json = JsonUtility.ToJson(new ResourceSaveData 
            { 
                resources = GetSerializableResources() 
            });
            PlayerPrefs.SetString("ResourceData", json);
            PlayerPrefs.Save();
        }

        private void LoadResources()
        {
            string json = PlayerPrefs.GetString("ResourceData", "");
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<ResourceSaveData>(json);
                _resources.Clear();
                foreach (var kvp in data.resources)
                {
                    _resources[kvp.key] = kvp.value;
                }
            }
            else
            {
                // Starting resources - mystical materials!
                _resources["spirit_bamboo"] = 50;
                _resources["temple_granite"] = 30;
                _resources["blessed_lotus_fiber"] = 25;
                _resources["sacred_mud"] = 40;
                _resources["moonlit_palm"] = 20;
                _resources["whisper_vine"] = 15;
                _resources["blessed_thatch"] = 30;
            }
        }

        private List<SerializableKeyValue> GetSerializableResources()
        {
            var list = new List<SerializableKeyValue>();
            foreach (var kvp in _resources)
            {
                list.Add(new SerializableKeyValue { key = kvp.Key, value = kvp.Value });
            }
            return list;
        }

        [Serializable]
        private class ResourceSaveData
        {
            public List<SerializableKeyValue> resources;
        }

        [Serializable]
        private class SerializableKeyValue
        {
            public string key;
            public int value;
        }
        #endregion

        #region Cheats (Dev Only)
        [ContextMenu("Add 100 of Each Basic Resource")]
        private void CheatAddBasicResources()
        {
            AddResource("wood", 100);
            AddResource("stone", 100);
            AddResource("bamboo", 100);
            AddResource("clay", 100);
            AddResource("palm_leaves", 100);
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class CraftingRecipe
    {
        public string outputId;
        public int outputAmount;
        public List<ResourceCost> inputs;
    }
    #endregion
}

