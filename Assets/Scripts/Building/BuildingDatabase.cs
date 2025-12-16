using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Database of all buildable objects in the game.
    /// </summary>
    public class BuildingDatabase : MonoBehaviour
    {
        #region Singleton
        private static BuildingDatabase _instance;
        public static BuildingDatabase Instance => _instance;
        #endregion

        #region Data
        [Header("Buildable Prefabs")]
        [SerializeField] private List<BuildableObject> _buildables = new List<BuildableObject>();
        
        private Dictionary<string, BuildableObject> _buildableDict;
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
            
            // Build dictionary
            _buildableDict = new Dictionary<string, BuildableObject>();
            foreach (var buildable in _buildables)
            {
                if (buildable != null && !string.IsNullOrEmpty(buildable.ObjectId))
                {
                    _buildableDict[buildable.ObjectId] = buildable;
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Queries
        /// <summary>
        /// Get a buildable by ID.
        /// </summary>
        public BuildableObject GetBuildable(string objectId)
        {
            return _buildableDict.TryGetValue(objectId, out var buildable) ? buildable : null;
        }

        /// <summary>
        /// Get all buildables in a category.
        /// </summary>
        public List<BuildableObject> GetByCategory(BuildableObject.BuildCategory category)
        {
            return _buildables
                .Where(b => b != null && b.Category == category)
                .ToList();
        }

        /// <summary>
        /// Get all unlocked buildables.
        /// </summary>
        public List<BuildableObject> GetUnlocked()
        {
            var kingdomData = KingdomManager.Instance?.Data;
            if (kingdomData == null)
            {
                return _buildables.Where(b => b != null && b.IsUnlocked).ToList();
            }
            
            return _buildables
                .Where(b => b != null && 
                       (b.IsUnlocked || kingdomData.unlockedBuildings.Contains(b.ObjectId)))
                .ToList();
        }

        /// <summary>
        /// Get buildings unlocked at a specific level.
        /// </summary>
        public List<BuildableObject> GetBuildingsForLevel(int level)
        {
            return _buildables
                .Where(b => b != null && b.UnlockLevel == level)
                .ToList();
        }

        /// <summary>
        /// Get all buildables.
        /// </summary>
        public List<BuildableObject> GetAll()
        {
            return new List<BuildableObject>(_buildables);
        }

        /// <summary>
        /// Search buildables by name.
        /// </summary>
        public List<BuildableObject> Search(string query)
        {
            query = query.ToLower();
            return _buildables
                .Where(b => b != null && 
                       (b.DisplayName.ToLower().Contains(query) || 
                        b.ObjectId.ToLower().Contains(query)))
                .ToList();
        }
        #endregion

        #region Registration
        /// <summary>
        /// Register a buildable at runtime.
        /// </summary>
        public void RegisterBuildable(BuildableObject buildable)
        {
            if (buildable == null || string.IsNullOrEmpty(buildable.ObjectId))
                return;
            
            if (!_buildableDict.ContainsKey(buildable.ObjectId))
            {
                _buildables.Add(buildable);
                _buildableDict[buildable.ObjectId] = buildable;
            }
        }
        #endregion
    }
}

