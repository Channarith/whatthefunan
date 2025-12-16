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
            // Basic
            new ResourceDefinition { resourceId = "wood", displayName = "Wood", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "stone", displayName = "Stone", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "bamboo", displayName = "Bamboo", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "clay", displayName = "Clay", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "palm_leaves", displayName = "Palm Leaves", type = ResourceType.Basic },
            new ResourceDefinition { resourceId = "rope", displayName = "Rope", type = ResourceType.Basic },
            
            // Refined
            new ResourceDefinition { resourceId = "plank", displayName = "Wooden Plank", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "brick", displayName = "Brick", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "carved_stone", displayName = "Carved Stone", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "silk", displayName = "Silk", type = ResourceType.Refined },
            new ResourceDefinition { resourceId = "lacquer", displayName = "Lacquer", type = ResourceType.Refined },
            
            // Precious
            new ResourceDefinition { resourceId = "gold_ingot", displayName = "Gold Ingot", type = ResourceType.Precious },
            new ResourceDefinition { resourceId = "jade", displayName = "Jade", type = ResourceType.Precious },
            new ResourceDefinition { resourceId = "pearl", displayName = "Pearl", type = ResourceType.Precious },
            
            // Special
            new ResourceDefinition { resourceId = "naga_scale", displayName = "Naga Scale", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "apsara_feather", displayName = "Apsara Feather", type = ResourceType.Special },
            new ResourceDefinition { resourceId = "ancient_relic", displayName = "Ancient Relic", type = ResourceType.Special }
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
            // Define basic recipes
            return outputId switch
            {
                "plank" => new CraftingRecipe
                {
                    outputId = "plank",
                    outputAmount = 4,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "wood", amount = 2 }
                    }
                },
                "brick" => new CraftingRecipe
                {
                    outputId = "brick",
                    outputAmount = 4,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "clay", amount = 3 }
                    }
                },
                "carved_stone" => new CraftingRecipe
                {
                    outputId = "carved_stone",
                    outputAmount = 1,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "stone", amount = 4 }
                    }
                },
                "rope" => new CraftingRecipe
                {
                    outputId = "rope",
                    outputAmount = 2,
                    inputs = new List<ResourceCost>
                    {
                        new ResourceCost { resourceId = "palm_leaves", amount = 3 }
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
                // Starting resources
                _resources["wood"] = 50;
                _resources["stone"] = 30;
                _resources["bamboo"] = 20;
                _resources["palm_leaves"] = 15;
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

