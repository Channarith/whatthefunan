using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.RPG
{
    /// <summary>
    /// Manages player inventory, equipment, and item usage.
    /// Supports stacking, categories, and equipment slots.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        #region Singleton
        private static InventorySystem _instance;
        public static InventorySystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Item, int> OnItemAdded;
        public static event Action<Item, int> OnItemRemoved;
        public static event Action<Item> OnItemUsed;
        public static event Action<Item, EquipmentSlot> OnItemEquipped;
        public static event Action<Item, EquipmentSlot> OnItemUnequipped;
        public static event Action OnInventoryChanged;
        public static event Action OnEquipmentChanged;
        #endregion

        #region Inventory Data
        [Header("Item Database")]
        [SerializeField] private List<Item> _itemDatabase = new List<Item>();
        
        private Dictionary<string, Item> _itemLookup = new Dictionary<string, Item>();
        private Dictionary<string, InventorySlot> _inventory = new Dictionary<string, InventorySlot>();
        private Dictionary<EquipmentSlot, string> _equipment = new Dictionary<EquipmentSlot, string>();
        
        [Header("Settings")]
        [SerializeField] private int _maxInventorySlots = 50;
        [SerializeField] private int _maxStackSize = 99;
        
        public int UsedSlots => _inventory.Count;
        public int FreeSlots => _maxInventorySlots - UsedSlots;
        public bool HasFreeSlot => FreeSlots > 0;
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
            
            InitializeDatabase();
            InitializeEquipmentSlots();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeDatabase()
        {
            _itemLookup.Clear();
            foreach (var item in _itemDatabase)
            {
                if (!string.IsNullOrEmpty(item.itemId))
                {
                    _itemLookup[item.itemId] = item;
                }
            }
        }

        private void InitializeEquipmentSlots()
        {
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (!_equipment.ContainsKey(slot))
                {
                    _equipment[slot] = null;
                }
            }
        }
        #endregion

        #region Inventory Management
        /// <summary>
        /// Add an item to inventory.
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1)
        {
            if (!_itemLookup.TryGetValue(itemId, out Item item))
            {
                Debug.LogWarning($"[InventorySystem] Item not found: {itemId}");
                return false;
            }
            
            return AddItem(item, quantity);
        }

        /// <summary>
        /// Add an item to inventory.
        /// </summary>
        public bool AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            
            // Check if item is stackable and already in inventory
            if (item.isStackable && _inventory.TryGetValue(item.itemId, out InventorySlot existingSlot))
            {
                int spaceLeft = _maxStackSize - existingSlot.quantity;
                int toAdd = Mathf.Min(quantity, spaceLeft);
                
                if (toAdd > 0)
                {
                    existingSlot.quantity += toAdd;
                    quantity -= toAdd;
                    OnItemAdded?.Invoke(item, toAdd);
                }
            }
            
            // Add remaining as new stacks
            while (quantity > 0)
            {
                if (!HasFreeSlot)
                {
                    Debug.LogWarning("[InventorySystem] Inventory full");
                    return false;
                }
                
                int toAdd = item.isStackable ? Mathf.Min(quantity, _maxStackSize) : 1;
                
                // Generate unique slot key for non-stackable items
                string slotKey = item.isStackable ? item.itemId : $"{item.itemId}_{Guid.NewGuid():N}";
                
                _inventory[slotKey] = new InventorySlot
                {
                    itemId = item.itemId,
                    quantity = toAdd
                };
                
                quantity -= toAdd;
                OnItemAdded?.Invoke(item, toAdd);
            }
            
            OnInventoryChanged?.Invoke();
            
            // Notify quest system
            QuestSystem.Instance?.OnItemCollected(item.itemId);
            
            Debug.Log($"[InventorySystem] Added {item.itemName}");
            return true;
        }

        /// <summary>
        /// Remove an item from inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (!HasItem(itemId, quantity))
            {
                Debug.LogWarning($"[InventorySystem] Not enough {itemId}");
                return false;
            }
            
            var item = GetItemData(itemId);
            int remaining = quantity;
            
            // Find and remove from slots
            var slotsToModify = _inventory
                .Where(kv => kv.Value.itemId == itemId)
                .OrderBy(kv => kv.Value.quantity)
                .ToList();
            
            foreach (var slot in slotsToModify)
            {
                if (remaining <= 0) break;
                
                int toRemove = Mathf.Min(remaining, slot.Value.quantity);
                slot.Value.quantity -= toRemove;
                remaining -= toRemove;
                
                if (slot.Value.quantity <= 0)
                {
                    _inventory.Remove(slot.Key);
                }
            }
            
            OnItemRemoved?.Invoke(item, quantity);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"[InventorySystem] Removed {quantity} {item?.itemName}");
            return true;
        }

        /// <summary>
        /// Use an item (consume, equip, etc.).
        /// </summary>
        public bool UseItem(string itemId)
        {
            if (!HasItem(itemId))
            {
                Debug.LogWarning($"[InventorySystem] Don't have {itemId}");
                return false;
            }
            
            var item = GetItemData(itemId);
            if (item == null) return false;
            
            switch (item.type)
            {
                case ItemType.Consumable:
                    UseConsumable(item);
                    RemoveItem(itemId, 1);
                    break;
                    
                case ItemType.Equipment:
                    EquipItem(itemId);
                    break;
                    
                case ItemType.Quest:
                    // Quest items typically can't be "used"
                    Debug.Log($"[InventorySystem] Quest item: {item.itemName}");
                    break;
                    
                default:
                    Debug.Log($"[InventorySystem] Cannot use: {item.itemName}");
                    return false;
            }
            
            OnItemUsed?.Invoke(item);
            return true;
        }

        private void UseConsumable(Item item)
        {
            // Apply consumable effects
            foreach (var effect in item.effects)
            {
                ApplyEffect(effect);
            }
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
        }

        private void ApplyEffect(ItemEffect effect)
        {
            switch (effect.type)
            {
                case EffectType.Heal:
                    // CharacterStats.Instance?.Heal(effect.value);
                    Debug.Log($"[InventorySystem] Healed for {effect.value}");
                    break;
                    
                case EffectType.RestoreEnergy:
                    Debug.Log($"[InventorySystem] Restored {effect.value} energy");
                    break;
                    
                case EffectType.Buff:
                    // Apply temporary stat buff
                    Debug.Log($"[InventorySystem] Applied buff: {effect.buffId}");
                    break;
                    
                case EffectType.Experience:
                    // CharacterStats.Instance?.AddExperience((int)effect.value);
                    Debug.Log($"[InventorySystem] Gained {effect.value} XP");
                    break;
            }
        }
        #endregion

        #region Equipment
        /// <summary>
        /// Equip an item to its appropriate slot.
        /// </summary>
        public bool EquipItem(string itemId)
        {
            var item = GetItemData(itemId);
            if (item == null || item.type != ItemType.Equipment)
            {
                Debug.LogWarning($"[InventorySystem] Cannot equip: {itemId}");
                return false;
            }
            
            // Unequip current item in slot if any
            if (!string.IsNullOrEmpty(_equipment[item.equipSlot]))
            {
                UnequipItem(item.equipSlot);
            }
            
            _equipment[item.equipSlot] = itemId;
            
            // Apply stat bonuses
            foreach (var bonus in item.statBonuses)
            {
                // CharacterStats.Instance?.AddStatBonus(bonus.stat, bonus.value);
            }
            
            OnItemEquipped?.Invoke(item, item.equipSlot);
            OnEquipmentChanged?.Invoke();
            
            Debug.Log($"[InventorySystem] Equipped: {item.itemName} to {item.equipSlot}");
            return true;
        }

        /// <summary>
        /// Unequip an item from a slot.
        /// </summary>
        public bool UnequipItem(EquipmentSlot slot)
        {
            if (string.IsNullOrEmpty(_equipment[slot]))
            {
                return false;
            }
            
            var item = GetItemData(_equipment[slot]);
            
            // Remove stat bonuses
            if (item != null)
            {
                foreach (var bonus in item.statBonuses)
                {
                    // CharacterStats.Instance?.RemoveStatBonus(bonus.stat, bonus.value);
                }
                
                OnItemUnequipped?.Invoke(item, slot);
            }
            
            _equipment[slot] = null;
            OnEquipmentChanged?.Invoke();
            
            Debug.Log($"[InventorySystem] Unequipped from: {slot}");
            return true;
        }

        /// <summary>
        /// Get equipped item in a slot.
        /// </summary>
        public Item GetEquippedItem(EquipmentSlot slot)
        {
            var itemId = _equipment.GetValueOrDefault(slot, null);
            return string.IsNullOrEmpty(itemId) ? null : GetItemData(itemId);
        }

        /// <summary>
        /// Get all equipped items.
        /// </summary>
        public Dictionary<EquipmentSlot, Item> GetAllEquipment()
        {
            var result = new Dictionary<EquipmentSlot, Item>();
            foreach (var kv in _equipment)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    result[kv.Key] = GetItemData(kv.Value);
                }
            }
            return result;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Check if player has an item.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }

        /// <summary>
        /// Get the count of an item.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return _inventory
                .Where(kv => kv.Value.itemId == itemId)
                .Sum(kv => kv.Value.quantity);
        }

        /// <summary>
        /// Get item data from database.
        /// </summary>
        public Item GetItemData(string itemId)
        {
            return _itemLookup.GetValueOrDefault(itemId, null);
        }

        /// <summary>
        /// Get all inventory items.
        /// </summary>
        public List<InventorySlot> GetAllItems()
        {
            return _inventory.Values.ToList();
        }

        /// <summary>
        /// Get items by category.
        /// </summary>
        public List<InventorySlot> GetItemsByCategory(ItemCategory category)
        {
            return _inventory.Values
                .Where(slot => 
                {
                    var item = GetItemData(slot.itemId);
                    return item != null && item.category == category;
                })
                .ToList();
        }
        #endregion

        #region Save/Load
        public InventorySaveData GetSaveData()
        {
            return new InventorySaveData
            {
                items = _inventory.Values.ToList(),
                equipment = _equipment.ToDictionary(kv => (int)kv.Key, kv => kv.Value)
            };
        }

        public void LoadSaveData(InventorySaveData data)
        {
            _inventory.Clear();
            foreach (var slot in data.items)
            {
                var item = GetItemData(slot.itemId);
                string key = item?.isStackable == true ? slot.itemId : $"{slot.itemId}_{Guid.NewGuid():N}";
                _inventory[key] = slot;
            }
            
            _equipment.Clear();
            InitializeEquipmentSlots();
            foreach (var kv in data.equipment)
            {
                _equipment[(EquipmentSlot)kv.Key] = kv.Value;
            }
            
            OnInventoryChanged?.Invoke();
            OnEquipmentChanged?.Invoke();
        }

        [Serializable]
        public class InventorySaveData
        {
            public List<InventorySlot> items;
            public Dictionary<int, string> equipment;
        }
        #endregion
    }

    #region Item Data Classes
    public enum ItemType
    {
        Consumable,     // Potions, food
        Equipment,      // Weapons, armor
        Material,       // Crafting materials
        Quest,          // Quest items
        Cosmetic,       // Skins, outfits
        Currency,       // Special currency items
        Key             // Keys to unlock areas
    }

    public enum ItemCategory
    {
        All,
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material,
        Quest,
        Cosmetic
    }

    public enum EquipmentSlot
    {
        Weapon,
        Head,
        Body,
        Legs,
        Accessory1,
        Accessory2
    }

    public enum EffectType
    {
        Heal,
        RestoreEnergy,
        Buff,
        Experience,
        Currency
    }

    [Serializable]
    public class InventorySlot
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class Item
    {
        [Header("Identity")]
        public string itemId;
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;
        
        [Header("Classification")]
        public ItemType type;
        public ItemCategory category;
        public ItemRarity rarity = ItemRarity.Common;
        
        [Header("Stacking")]
        public bool isStackable = true;
        
        [Header("Equipment (if applicable)")]
        public EquipmentSlot equipSlot;
        public List<StatBonus> statBonuses = new List<StatBonus>();
        
        [Header("Effects (if consumable)")]
        public List<ItemEffect> effects = new List<ItemEffect>();
        
        [Header("Economy")]
        public int buyPrice;
        public int sellPrice;
        
        [Header("Requirements")]
        public int requiredLevel = 1;
        public List<string> requiredClasses = new List<string>();
    }

    [Serializable]
    public class StatBonus
    {
        public CharacterStats.StatType stat;
        public int value;
    }

    [Serializable]
    public class ItemEffect
    {
        public EffectType type;
        public float value;
        public float duration;
        public string buffId;
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    #endregion
}

