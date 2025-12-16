using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// Core building system allowing players to construct their own Funan kingdom.
    /// Inspired by Minecraft/Roblox building mechanics with Southeast Asian aesthetics.
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        #region Singleton
        private static BuildingSystem _instance;
        public static BuildingSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<BuildableObject> OnObjectPlaced;
        public static event Action<BuildableObject> OnObjectRemoved;
        public static event Action<BuildMode> OnBuildModeChanged;
        public static event Action<int> OnGridSizeChanged;
        #endregion

        #region Enums
        public enum BuildMode
        {
            None,           // Normal gameplay
            Place,          // Placing new objects
            Move,           // Moving existing objects
            Rotate,         // Rotating objects
            Delete,         // Removing objects
            Paint,          // Changing colors/materials
            Copy            // Duplicating objects
        }

        public enum PlacementType
        {
            Grid,           // Snap to grid (Minecraft style)
            Free,           // Free placement (Roblox style)
            Surface         // Snap to surfaces
        }
        #endregion

        #region Settings
        [Header("Grid Settings")]
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private float _gridHeight = 0.5f;
        [SerializeField] private int _maxBuildHeight = 50;
        [SerializeField] private Vector2Int _kingdomSize = new Vector2Int(100, 100);
        
        [Header("Placement")]
        [SerializeField] private PlacementType _placementType = PlacementType.Grid;
        [SerializeField] private LayerMask _buildableLayers;
        [SerializeField] private LayerMask _obstacleLayers;
        [SerializeField] private float _rotationStep = 15f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private GameObject _gridVisualizerPrefab;
        [SerializeField] private Color _gridColor = new Color(1f, 0.8f, 0.2f, 0.3f);
        
        [Header("Limits")]
        [SerializeField] private int _maxObjectsPerKingdom = 1000;
        [SerializeField] private bool _requireResources = true;
        #endregion

        #region State
        private BuildMode _currentMode = BuildMode.None;
        private BuildableObject _selectedPrefab;
        private GameObject _previewObject;
        private BuildableObject _hoveredObject;
        private float _currentRotation;
        private Vector3 _lastValidPosition;
        private bool _canPlace;
        private List<PlacedObject> _placedObjects = new List<PlacedObject>();
        private GameObject _gridVisualizer;
        
        public BuildMode CurrentMode => _currentMode;
        public int PlacedObjectCount => _placedObjects.Count;
        public bool IsBuilding => _currentMode != BuildMode.None;
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
        }

        private void Update()
        {
            if (_currentMode == BuildMode.None) return;
            
            UpdateBuildingPreview();
            HandleBuildingInput();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Mode Control
        /// <summary>
        /// Enter building mode with specified mode type.
        /// </summary>
        public void EnterBuildMode(BuildMode mode)
        {
            if (_currentMode == mode) return;
            
            ExitBuildMode();
            _currentMode = mode;
            
            // Show grid
            if (_gridVisualizer == null && _gridVisualizerPrefab != null)
            {
                _gridVisualizer = Instantiate(_gridVisualizerPrefab);
            }
            if (_gridVisualizer != null)
            {
                _gridVisualizer.SetActive(true);
            }
            
            OnBuildModeChanged?.Invoke(mode);
            Core.HapticManager.Instance?.TriggerLight();
            
            Debug.Log($"[BuildingSystem] Entered {mode} mode");
        }

        /// <summary>
        /// Exit building mode.
        /// </summary>
        public void ExitBuildMode()
        {
            if (_currentMode == BuildMode.None) return;
            
            _currentMode = BuildMode.None;
            _selectedPrefab = null;
            
            // Destroy preview
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }
            
            // Hide grid
            if (_gridVisualizer != null)
            {
                _gridVisualizer.SetActive(false);
            }
            
            OnBuildModeChanged?.Invoke(BuildMode.None);
            Debug.Log("[BuildingSystem] Exited build mode");
        }

        /// <summary>
        /// Select a buildable object to place.
        /// </summary>
        public void SelectBuildable(BuildableObject prefab)
        {
            _selectedPrefab = prefab;
            _currentRotation = 0;
            
            // Create preview
            if (_previewObject != null)
            {
                Destroy(_previewObject);
            }
            
            if (prefab != null && prefab.PreviewPrefab != null)
            {
                _previewObject = Instantiate(prefab.PreviewPrefab);
                _previewObject.name = "BuildPreview";
                
                // Disable colliders on preview
                foreach (var col in _previewObject.GetComponentsInChildren<Collider>())
                {
                    col.enabled = false;
                }
            }
            
            EnterBuildMode(BuildMode.Place);
        }
        #endregion

        #region Building Preview
        private void UpdateBuildingPreview()
        {
            if (_previewObject == null || _selectedPrefab == null) return;
            
            // Raycast to find placement position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // For touch, use touch position
            if (Input.touchCount > 0)
            {
                ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            }
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _buildableLayers))
            {
                Vector3 targetPos = hit.point;
                
                // Snap to grid
                if (_placementType == PlacementType.Grid)
                {
                    targetPos = SnapToGrid(targetPos);
                }
                
                // Apply rotation
                Quaternion targetRot = Quaternion.Euler(0, _currentRotation, 0);
                
                // Update preview position
                _previewObject.transform.position = targetPos;
                _previewObject.transform.rotation = targetRot;
                
                // Check if placement is valid
                _canPlace = IsValidPlacement(targetPos, targetRot);
                
                // Update preview material
                UpdatePreviewMaterial(_canPlace);
                
                if (_canPlace)
                {
                    _lastValidPosition = targetPos;
                }
            }
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / _gridSize) * _gridSize;
            float y = Mathf.Round(position.y / _gridHeight) * _gridHeight;
            float z = Mathf.Round(position.z / _gridSize) * _gridSize;
            
            return new Vector3(x, y, z);
        }

        private bool IsValidPlacement(Vector3 position, Quaternion rotation)
        {
            if (_selectedPrefab == null) return false;
            
            // Check object limit
            if (_placedObjects.Count >= _maxObjectsPerKingdom)
            {
                return false;
            }
            
            // Check build height
            if (position.y > _maxBuildHeight * _gridHeight)
            {
                return false;
            }
            
            // Check kingdom bounds
            if (Mathf.Abs(position.x) > _kingdomSize.x / 2 ||
                Mathf.Abs(position.z) > _kingdomSize.y / 2)
            {
                return false;
            }
            
            // Check for overlapping objects
            Bounds bounds = _selectedPrefab.GetBounds();
            bounds.center = position;
            
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center, 
                bounds.extents * 0.9f, 
                rotation, 
                _obstacleLayers);
            
            if (overlaps.Length > 0)
            {
                return false;
            }
            
            // Check resources
            if (_requireResources && !HasRequiredResources(_selectedPrefab))
            {
                return false;
            }
            
            return true;
        }

        private void UpdatePreviewMaterial(bool valid)
        {
            if (_previewObject == null) return;
            
            Material mat = valid ? _validPlacementMaterial : _invalidPlacementMaterial;
            
            foreach (var renderer in _previewObject.GetComponentsInChildren<Renderer>())
            {
                renderer.material = mat;
            }
        }
        #endregion

        #region Building Input
        private void HandleBuildingInput()
        {
            // Rotation
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.E))
            {
                RotatePreview(Input.GetKey(KeyCode.LeftShift) ? -_rotationStep : _rotationStep);
            }
            
            // Mouse wheel rotation
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                RotatePreview(scroll > 0 ? _rotationStep : -_rotationStep);
            }
            
            // Place object
            if (Input.GetMouseButtonDown(0) || 
                (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                if (_currentMode == BuildMode.Place && _canPlace)
                {
                    PlaceObject();
                }
                else if (_currentMode == BuildMode.Delete)
                {
                    TryDeleteObject();
                }
                else if (_currentMode == BuildMode.Move)
                {
                    TryPickupObject();
                }
            }
            
            // Cancel
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                ExitBuildMode();
            }
        }

        private void RotatePreview(float degrees)
        {
            _currentRotation += degrees;
            _currentRotation = _currentRotation % 360;
            
            Core.HapticManager.Instance?.TriggerSelection();
        }
        #endregion

        #region Object Placement
        private void PlaceObject()
        {
            if (_selectedPrefab == null || !_canPlace) return;
            
            // Consume resources
            if (_requireResources)
            {
                ConsumeResources(_selectedPrefab);
            }
            
            // Instantiate actual object
            Vector3 position = _previewObject.transform.position;
            Quaternion rotation = _previewObject.transform.rotation;
            
            GameObject placed = Instantiate(_selectedPrefab.gameObject, position, rotation);
            placed.name = $"{_selectedPrefab.ObjectId}_{_placedObjects.Count}";
            
            // Track placement
            var placedData = new PlacedObject
            {
                objectId = _selectedPrefab.ObjectId,
                position = position,
                rotation = rotation.eulerAngles,
                timestamp = DateTime.Now
            };
            _placedObjects.Add(placedData);
            
            // Trigger events
            OnObjectPlaced?.Invoke(_selectedPrefab);
            Core.HapticManager.Instance?.TriggerMedium();
            Core.AudioManager.Instance?.PlaySFX("sfx_build_place");
            
            Debug.Log($"[BuildingSystem] Placed {_selectedPrefab.ObjectId} at {position}");
        }

        private void TryDeleteObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var buildable = hit.collider.GetComponentInParent<BuildableObject>();
                
                if (buildable != null && buildable.IsRemovable)
                {
                    RemoveObject(buildable);
                }
            }
        }

        private void RemoveObject(BuildableObject obj)
        {
            // Refund resources (partial)
            if (_requireResources)
            {
                RefundResources(obj, 0.5f);
            }
            
            // Remove from tracking
            _placedObjects.RemoveAll(p => p.objectId == obj.ObjectId && 
                Vector3.Distance(p.position, obj.transform.position) < 0.1f);
            
            // Destroy
            OnObjectRemoved?.Invoke(obj);
            Core.AudioManager.Instance?.PlaySFX("sfx_build_remove");
            Core.HapticManager.Instance?.TriggerLight();
            
            Destroy(obj.gameObject);
            
            Debug.Log($"[BuildingSystem] Removed {obj.ObjectId}");
        }

        private void TryPickupObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var buildable = hit.collider.GetComponentInParent<BuildableObject>();
                
                if (buildable != null && buildable.IsMovable)
                {
                    // Convert to preview
                    _selectedPrefab = buildable;
                    _currentRotation = buildable.transform.eulerAngles.y;
                    
                    // Create preview from existing
                    if (_previewObject != null) Destroy(_previewObject);
                    _previewObject = Instantiate(buildable.PreviewPrefab ?? buildable.gameObject);
                    
                    // Remove original
                    RemoveObject(buildable);
                    
                    // Switch to place mode
                    _currentMode = BuildMode.Place;
                }
            }
        }
        #endregion

        #region Resources
        private bool HasRequiredResources(BuildableObject obj)
        {
            foreach (var cost in obj.BuildCost)
            {
                int available = ResourceManager.Instance?.GetResourceCount(cost.resourceId) ?? 0;
                if (available < cost.amount)
                {
                    return false;
                }
            }
            return true;
        }

        private void ConsumeResources(BuildableObject obj)
        {
            foreach (var cost in obj.BuildCost)
            {
                ResourceManager.Instance?.ConsumeResource(cost.resourceId, cost.amount);
            }
        }

        private void RefundResources(BuildableObject obj, float refundPercent)
        {
            foreach (var cost in obj.BuildCost)
            {
                int refundAmount = Mathf.FloorToInt(cost.amount * refundPercent);
                ResourceManager.Instance?.AddResource(cost.resourceId, refundAmount);
            }
        }
        #endregion

        #region Save/Load
        /// <summary>
        /// Get all placed objects for saving.
        /// </summary>
        public List<PlacedObject> GetPlacedObjects()
        {
            return new List<PlacedObject>(_placedObjects);
        }

        /// <summary>
        /// Load placed objects from save data.
        /// </summary>
        public void LoadPlacedObjects(List<PlacedObject> objects)
        {
            // Clear existing
            ClearAllObjects();
            
            // Place loaded objects
            foreach (var obj in objects)
            {
                var prefab = BuildingDatabase.Instance?.GetBuildable(obj.objectId);
                if (prefab != null)
                {
                    GameObject placed = Instantiate(
                        prefab.gameObject, 
                        obj.position, 
                        Quaternion.Euler(obj.rotation));
                    placed.name = $"{obj.objectId}_loaded";
                }
                _placedObjects.Add(obj);
            }
            
            Debug.Log($"[BuildingSystem] Loaded {objects.Count} objects");
        }

        /// <summary>
        /// Clear all placed objects.
        /// </summary>
        public void ClearAllObjects()
        {
            var allBuildables = FindObjectsOfType<BuildableObject>();
            foreach (var obj in allBuildables)
            {
                if (obj.IsRemovable)
                {
                    Destroy(obj.gameObject);
                }
            }
            _placedObjects.Clear();
        }
        #endregion

        #region Grid Settings
        public void SetGridSize(float size)
        {
            _gridSize = Mathf.Max(0.25f, size);
            OnGridSizeChanged?.Invoke(Mathf.RoundToInt(_gridSize * 4));
        }

        public void SetPlacementType(PlacementType type)
        {
            _placementType = type;
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class PlacedObject
    {
        public string objectId;
        public Vector3 position;
        public Vector3 rotation;
        public DateTime timestamp;
        public string customData; // For signs, colors, etc.
    }

    [Serializable]
    public class ResourceCost
    {
        public string resourceId;
        public int amount;
    }
    #endregion
}

