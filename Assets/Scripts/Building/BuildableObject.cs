using UnityEngine;
using System.Collections.Generic;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Component for objects that can be placed in the kingdom builder.
    /// </summary>
    public class BuildableObject : MonoBehaviour
    {
        #region Enums
        public enum BuildCategory
        {
            Foundation,     // Floors, platforms
            Walls,          // Walls, fences
            Roofs,          // Roofing, canopies
            Structures,     // Complete buildings
            Decorations,    // Statues, plants, etc.
            Furniture,      // Interior items
            Functional,     // Crafting stations, storage
            Nature,         // Trees, rocks, water
            Special         // Unique/premium items
        }

        public enum BuildRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }
        #endregion

        #region Properties
        [Header("Identity")]
        [SerializeField] private string _objectId;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private BuildCategory _category;
        [SerializeField] private BuildRarity _rarity;
        
        [Header("Placement")]
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private bool _canRotate = true;
        [SerializeField] private bool _canStack = false;
        [SerializeField] private bool _requiresFoundation = false;
        [SerializeField] private bool _isFoundation = false;
        [SerializeField] private float _placementOffset = 0f;
        
        [Header("Behavior")]
        [SerializeField] private bool _isRemovable = true;
        [SerializeField] private bool _isMovable = true;
        [SerializeField] private bool _isPaintable = false;
        [SerializeField] private bool _hasInteraction = false;
        
        [Header("Cost")]
        [SerializeField] private List<ResourceCost> _buildCost = new List<ResourceCost>();
        [SerializeField] private int _coinCost = 0;
        [SerializeField] private int _gemCost = 0;
        
        [Header("Unlock")]
        [SerializeField] private bool _isUnlocked = true;
        [SerializeField] private string _unlockRequirement;
        [SerializeField] private int _unlockLevel = 1;
        
        [Header("Visuals")]
        [SerializeField] private GameObject _previewPrefab;
        [SerializeField] private Material[] _paintMaterials;
        [SerializeField] private ParticleSystem _placementEffect;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _placeSound;
        [SerializeField] private AudioClip _removeSound;
        [SerializeField] private AudioClip _interactSound;
        #endregion

        #region Accessors
        public string ObjectId => _objectId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public BuildCategory Category => _category;
        public BuildRarity Rarity => _rarity;
        public Vector3Int GridSize => _gridSize;
        public bool CanRotate => _canRotate;
        public bool CanStack => _canStack;
        public bool RequiresFoundation => _requiresFoundation;
        public bool IsFoundation => _isFoundation;
        public bool IsRemovable => _isRemovable;
        public bool IsMovable => _isMovable;
        public bool IsPaintable => _isPaintable;
        public List<ResourceCost> BuildCost => _buildCost;
        public int CoinCost => _coinCost;
        public int GemCost => _gemCost;
        public bool IsUnlocked => _isUnlocked;
        public int UnlockLevel => _unlockLevel;
        public GameObject PreviewPrefab => _previewPrefab ?? gameObject;
        #endregion

        #region State
        private int _currentPaintIndex;
        private string _customText;
        #endregion

        #region Methods
        /// <summary>
        /// Get the bounds of this buildable.
        /// </summary>
        public Bounds GetBounds()
        {
            // Calculate from grid size
            Vector3 size = new Vector3(
                _gridSize.x * BuildingSystem.Instance?.GetComponent<BuildingSystem>() != null ? 1f : 1f,
                _gridSize.y * 0.5f,
                _gridSize.z * 1f
            );
            
            // Or calculate from renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }
            
            return new Bounds(transform.position, size);
        }

        /// <summary>
        /// Apply paint material.
        /// </summary>
        public void ApplyPaint(int materialIndex)
        {
            if (!_isPaintable || _paintMaterials == null || _paintMaterials.Length == 0)
                return;
            
            materialIndex = Mathf.Clamp(materialIndex, 0, _paintMaterials.Length - 1);
            _currentPaintIndex = materialIndex;
            
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = _paintMaterials[materialIndex];
            }
        }

        /// <summary>
        /// Cycle to next paint color.
        /// </summary>
        public void CyclePaint()
        {
            if (!_isPaintable || _paintMaterials == null || _paintMaterials.Length == 0)
                return;
            
            int nextIndex = (_currentPaintIndex + 1) % _paintMaterials.Length;
            ApplyPaint(nextIndex);
        }

        /// <summary>
        /// Set custom text (for signs, etc).
        /// </summary>
        public void SetCustomText(string text)
        {
            _customText = text;
            
            var textMesh = GetComponentInChildren<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = text;
            }
        }

        /// <summary>
        /// Handle interaction.
        /// </summary>
        public void OnInteract()
        {
            if (!_hasInteraction) return;
            
            if (_interactSound != null)
            {
                Core.AudioManager.Instance?.PlaySFX(_interactSound);
            }
            
            // Trigger specific interaction based on category
            switch (_category)
            {
                case BuildCategory.Functional:
                    // Open crafting/storage UI
                    break;
                case BuildCategory.Decorations:
                    // Toggle animation or light
                    break;
            }
        }

        /// <summary>
        /// Play placement effect.
        /// </summary>
        public void PlayPlacementEffect()
        {
            if (_placementEffect != null)
            {
                var effect = Instantiate(_placementEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }
            
            if (_placeSound != null)
            {
                Core.AudioManager.Instance?.PlaySFX(_placeSound);
            }
        }
        #endregion

        #region Serialization
        /// <summary>
        /// Get serializable state.
        /// </summary>
        public BuildableState GetState()
        {
            return new BuildableState
            {
                objectId = _objectId,
                position = transform.position,
                rotation = transform.eulerAngles,
                paintIndex = _currentPaintIndex,
                customText = _customText
            };
        }

        /// <summary>
        /// Apply serialized state.
        /// </summary>
        public void ApplyState(BuildableState state)
        {
            transform.position = state.position;
            transform.eulerAngles = state.rotation;
            
            if (state.paintIndex > 0)
            {
                ApplyPaint(state.paintIndex);
            }
            
            if (!string.IsNullOrEmpty(state.customText))
            {
                SetCustomText(state.customText);
            }
        }
        #endregion
    }

    #region Data Classes
    [System.Serializable]
    public class BuildableState
    {
        public string objectId;
        public Vector3 position;
        public Vector3 rotation;
        public int paintIndex;
        public string customText;
    }
    #endregion
}

