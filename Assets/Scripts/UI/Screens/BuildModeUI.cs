using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace WhatTheFunan.UI.Screens
{
    /// <summary>
    /// UI for kingdom building mode.
    /// </summary>
    public class BuildModeUI : MonoBehaviour
    {
        #region UI References
        [Header("Main Panel")]
        [SerializeField] private GameObject _buildPanel;
        [SerializeField] private CanvasGroup _buildPanelCanvasGroup;
        
        [Header("Category Tabs")]
        [SerializeField] private Transform _categoryTabsContainer;
        [SerializeField] private GameObject _categoryTabPrefab;
        
        [Header("Item Grid")]
        [SerializeField] private Transform _itemGridContainer;
        [SerializeField] private GameObject _buildableItemPrefab;
        [SerializeField] private ScrollRect _itemScrollRect;
        
        [Header("Selected Item Info")]
        [SerializeField] private GameObject _itemInfoPanel;
        [SerializeField] private Image _selectedItemIcon;
        [SerializeField] private TextMeshProUGUI _selectedItemName;
        [SerializeField] private TextMeshProUGUI _selectedItemDescription;
        [SerializeField] private Transform _costContainer;
        [SerializeField] private GameObject _costItemPrefab;
        
        [Header("Mode Buttons")]
        [SerializeField] private Button _placeModeButton;
        [SerializeField] private Button _moveModeButton;
        [SerializeField] private Button _deleteModeButton;
        [SerializeField] private Button _rotateModeButton;
        [SerializeField] private Button _exitButton;
        
        [Header("Resources Display")]
        [SerializeField] private Transform _resourcesContainer;
        [SerializeField] private GameObject _resourceDisplayPrefab;
        
        [Header("Kingdom Stats")]
        [SerializeField] private TextMeshProUGUI _kingdomLevelText;
        [SerializeField] private TextMeshProUGUI _populationText;
        [SerializeField] private TextMeshProUGUI _happinessText;
        [SerializeField] private Slider _storageSlider;
        [SerializeField] private TextMeshProUGUI _storageText;
        
        [Header("Confirmation")]
        [SerializeField] private GameObject _confirmPanel;
        [SerializeField] private TextMeshProUGUI _confirmText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;
        
        [Header("Grid Toggle")]
        [SerializeField] private Toggle _gridToggle;
        [SerializeField] private Slider _gridSizeSlider;
        #endregion

        #region State
        private Building.BuildableObject.BuildCategory _currentCategory;
        private Building.BuildableObject _selectedBuildable;
        private List<GameObject> _spawnedItems = new List<GameObject>();
        private List<GameObject> _spawnedCosts = new List<GameObject>();
        private List<GameObject> _spawnedResources = new List<GameObject>();
        private Dictionary<Building.BuildableObject.BuildCategory, Button> _categoryButtons = 
            new Dictionary<Building.BuildableObject.BuildCategory, Button>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            CreateCategoryTabs();
            UpdateResourcesDisplay();
            UpdateKingdomStats();
            
            // Default to first category
            SelectCategory(Building.BuildableObject.BuildCategory.Foundation);
        }

        private void OnEnable()
        {
            // Subscribe to events
            Building.BuildingSystem.OnBuildModeChanged += OnBuildModeChanged;
            Building.ResourceManager.OnResourceChanged += OnResourceChanged;
            Building.KingdomManager.OnStatsChanged += OnKingdomStatsChanged;
        }

        private void OnDisable()
        {
            Building.BuildingSystem.OnBuildModeChanged -= OnBuildModeChanged;
            Building.ResourceManager.OnResourceChanged -= OnResourceChanged;
            Building.KingdomManager.OnStatsChanged -= OnKingdomStatsChanged;
        }
        #endregion

        #region Setup
        private void SetupButtons()
        {
            _placeModeButton?.onClick.AddListener(() => EnterMode(Building.BuildingSystem.BuildMode.Place));
            _moveModeButton?.onClick.AddListener(() => EnterMode(Building.BuildingSystem.BuildMode.Move));
            _deleteModeButton?.onClick.AddListener(() => EnterMode(Building.BuildingSystem.BuildMode.Delete));
            _rotateModeButton?.onClick.AddListener(RotateSelected);
            _exitButton?.onClick.AddListener(ExitBuildMode);
            
            _gridToggle?.onValueChanged.AddListener(OnGridToggled);
            _gridSizeSlider?.onValueChanged.AddListener(OnGridSizeChanged);
            
            _confirmYesButton?.onClick.AddListener(OnConfirmYes);
            _confirmNoButton?.onClick.AddListener(OnConfirmNo);
        }

        private void CreateCategoryTabs()
        {
            if (_categoryTabsContainer == null || _categoryTabPrefab == null) return;
            
            // Clear existing
            foreach (Transform child in _categoryTabsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create tabs for each category
            var categories = System.Enum.GetValues(typeof(Building.BuildableObject.BuildCategory));
            
            foreach (Building.BuildableObject.BuildCategory category in categories)
            {
                GameObject tab = Instantiate(_categoryTabPrefab, _categoryTabsContainer);
                
                var text = tab.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = GetCategoryDisplayName(category);
                }
                
                var button = tab.GetComponent<Button>();
                if (button != null)
                {
                    var cat = category; // Capture for lambda
                    button.onClick.AddListener(() => SelectCategory(cat));
                    _categoryButtons[category] = button;
                }
                
                // Icon
                var icon = tab.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = GetCategoryIcon(category);
                }
            }
        }
        #endregion

        #region Categories
        private void SelectCategory(Building.BuildableObject.BuildCategory category)
        {
            _currentCategory = category;
            
            // Update tab highlights
            foreach (var kvp in _categoryButtons)
            {
                var colors = kvp.Value.colors;
                colors.normalColor = kvp.Key == category ? Color.yellow : Color.white;
                kvp.Value.colors = colors;
            }
            
            // Refresh items
            RefreshItemGrid();
            
            Core.AudioManager.Instance?.PlaySFX("sfx_button_click");
        }

        private string GetCategoryDisplayName(Building.BuildableObject.BuildCategory category)
        {
            return category switch
            {
                Building.BuildableObject.BuildCategory.Foundation => "Floors",
                Building.BuildableObject.BuildCategory.Walls => "Walls",
                Building.BuildableObject.BuildCategory.Roofs => "Roofs",
                Building.BuildableObject.BuildCategory.Structures => "Buildings",
                Building.BuildableObject.BuildCategory.Decorations => "Decor",
                Building.BuildableObject.BuildCategory.Furniture => "Furniture",
                Building.BuildableObject.BuildCategory.Functional => "Crafting",
                Building.BuildableObject.BuildCategory.Nature => "Nature",
                Building.BuildableObject.BuildCategory.Special => "Special",
                _ => category.ToString()
            };
        }

        private Sprite GetCategoryIcon(Building.BuildableObject.BuildCategory category)
        {
            // Would load from resources
            return null;
        }
        #endregion

        #region Item Grid
        private void RefreshItemGrid()
        {
            // Clear existing
            foreach (var item in _spawnedItems)
            {
                Destroy(item);
            }
            _spawnedItems.Clear();
            
            // Get items for category
            var items = Building.BuildingDatabase.Instance?.GetByCategory(_currentCategory);
            if (items == null) return;
            
            // Get unlocked buildings
            var unlockedIds = Building.KingdomManager.Instance?.Data?.unlockedBuildings ?? new List<string>();
            
            foreach (var buildable in items)
            {
                bool isUnlocked = buildable.IsUnlocked || unlockedIds.Contains(buildable.ObjectId);
                
                GameObject itemObj = Instantiate(_buildableItemPrefab, _itemGridContainer);
                _spawnedItems.Add(itemObj);
                
                // Icon
                var icon = itemObj.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && buildable.Icon != null)
                {
                    icon.sprite = buildable.Icon;
                    icon.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f);
                }
                
                // Name
                var nameText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = buildable.DisplayName;
                }
                
                // Lock overlay
                var lockIcon = itemObj.transform.Find("Lock");
                if (lockIcon != null)
                {
                    lockIcon.gameObject.SetActive(!isUnlocked);
                }
                
                // Button
                var button = itemObj.GetComponent<Button>();
                if (button != null)
                {
                    var item = buildable; // Capture for lambda
                    button.interactable = isUnlocked;
                    button.onClick.AddListener(() => SelectBuildable(item));
                }
                
                // Rarity border color
                var border = itemObj.transform.Find("Border")?.GetComponent<Image>();
                if (border != null)
                {
                    border.color = GetRarityColor(buildable.Rarity);
                }
            }
        }

        private Color GetRarityColor(Building.BuildableObject.BuildRarity rarity)
        {
            return rarity switch
            {
                Building.BuildableObject.BuildRarity.Common => new Color(0.6f, 0.6f, 0.6f),
                Building.BuildableObject.BuildRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                Building.BuildableObject.BuildRarity.Rare => new Color(0.2f, 0.4f, 1f),
                Building.BuildableObject.BuildRarity.Epic => new Color(0.7f, 0.2f, 0.9f),
                Building.BuildableObject.BuildRarity.Legendary => new Color(1f, 0.8f, 0.2f),
                _ => Color.white
            };
        }
        #endregion

        #region Item Selection
        private void SelectBuildable(Building.BuildableObject buildable)
        {
            _selectedBuildable = buildable;
            
            // Show info panel
            if (_itemInfoPanel != null)
            {
                _itemInfoPanel.SetActive(true);
            }
            
            // Update info
            if (_selectedItemIcon != null)
                _selectedItemIcon.sprite = buildable.Icon;
            
            if (_selectedItemName != null)
                _selectedItemName.text = buildable.DisplayName;
            
            if (_selectedItemDescription != null)
                _selectedItemDescription.text = buildable.Description;
            
            // Show costs
            UpdateCostDisplay(buildable);
            
            // Select in building system
            Building.BuildingSystem.Instance?.SelectBuildable(buildable);
            
            Core.AudioManager.Instance?.PlaySFX("sfx_button_click");
        }

        private void UpdateCostDisplay(Building.BuildableObject buildable)
        {
            // Clear existing
            foreach (var cost in _spawnedCosts)
            {
                Destroy(cost);
            }
            _spawnedCosts.Clear();
            
            if (_costContainer == null || _costItemPrefab == null) return;
            
            // Show coin cost
            if (buildable.CoinCost > 0)
            {
                CreateCostItem("coins", buildable.CoinCost);
            }
            
            // Show gem cost
            if (buildable.GemCost > 0)
            {
                CreateCostItem("gems", buildable.GemCost);
            }
            
            // Show resource costs
            foreach (var cost in buildable.BuildCost)
            {
                CreateCostItem(cost.resourceId, cost.amount);
            }
        }

        private void CreateCostItem(string resourceId, int amount)
        {
            GameObject costObj = Instantiate(_costItemPrefab, _costContainer);
            _spawnedCosts.Add(costObj);
            
            var def = Building.ResourceManager.Instance?.GetDefinition(resourceId);
            
            // Icon
            var icon = costObj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && def?.icon != null)
            {
                icon.sprite = def.icon;
            }
            
            // Amount
            var amountText = costObj.GetComponentInChildren<TextMeshProUGUI>();
            if (amountText != null)
            {
                int have = Building.ResourceManager.Instance?.GetResourceCount(resourceId) ?? 0;
                amountText.text = $"{have}/{amount}";
                amountText.color = have >= amount ? Color.white : Color.red;
            }
        }
        #endregion

        #region Mode Control
        private void EnterMode(Building.BuildingSystem.BuildMode mode)
        {
            Building.BuildingSystem.Instance?.EnterBuildMode(mode);
        }

        private void RotateSelected()
        {
            // Rotation handled by BuildingSystem
            Core.HapticManager.Instance?.TriggerSelection();
        }

        private void ExitBuildMode()
        {
            Building.BuildingSystem.Instance?.ExitBuildMode();
            gameObject.SetActive(false);
        }
        #endregion

        #region Resources Display
        private void UpdateResourcesDisplay()
        {
            if (_resourcesContainer == null) return;
            
            // Clear existing
            foreach (var res in _spawnedResources)
            {
                Destroy(res);
            }
            _spawnedResources.Clear();
            
            // Show main resources
            string[] mainResources = { "wood", "stone", "bamboo", "clay" };
            
            foreach (var resId in mainResources)
            {
                int count = Building.ResourceManager.Instance?.GetResourceCount(resId) ?? 0;
                var def = Building.ResourceManager.Instance?.GetDefinition(resId);
                
                if (_resourceDisplayPrefab != null)
                {
                    GameObject resObj = Instantiate(_resourceDisplayPrefab, _resourcesContainer);
                    _spawnedResources.Add(resObj);
                    
                    var icon = resObj.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null && def?.icon != null)
                    {
                        icon.sprite = def.icon;
                    }
                    
                    var text = resObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = count.ToString();
                    }
                }
            }
        }

        private void OnResourceChanged(string resourceId, int newAmount)
        {
            UpdateResourcesDisplay();
            UpdateCostDisplay(_selectedBuildable);
        }
        #endregion

        #region Kingdom Stats
        private void UpdateKingdomStats()
        {
            var kingdom = Building.KingdomManager.Instance;
            if (kingdom == null) return;
            
            if (_kingdomLevelText != null)
                _kingdomLevelText.text = $"Level {kingdom.Level}";
            
            if (_populationText != null)
                _populationText.text = $"{kingdom.Population}/{kingdom.Data?.maxPopulation ?? 10}";
            
            if (_happinessText != null)
                _happinessText.text = kingdom.GetHappinessEmoji();
            
            var resources = Building.ResourceManager.Instance;
            if (resources != null)
            {
                if (_storageSlider != null)
                    _storageSlider.value = resources.StoragePercent;
                
                if (_storageText != null)
                    _storageText.text = $"{resources.StorageUsed}/{resources.StorageCapacity}";
            }
        }

        private void OnKingdomStatsChanged(Building.KingdomManager.KingdomStats stats)
        {
            UpdateKingdomStats();
        }
        #endregion

        #region Events
        private void OnBuildModeChanged(Building.BuildingSystem.BuildMode mode)
        {
            // Update button highlights
            UpdateModeButtonHighlights(mode);
        }

        private void UpdateModeButtonHighlights(Building.BuildingSystem.BuildMode mode)
        {
            SetButtonHighlight(_placeModeButton, mode == Building.BuildingSystem.BuildMode.Place);
            SetButtonHighlight(_moveModeButton, mode == Building.BuildingSystem.BuildMode.Move);
            SetButtonHighlight(_deleteModeButton, mode == Building.BuildingSystem.BuildMode.Delete);
        }

        private void SetButtonHighlight(Button button, bool highlighted)
        {
            if (button == null) return;
            
            var colors = button.colors;
            colors.normalColor = highlighted ? new Color(1f, 0.8f, 0.2f) : Color.white;
            button.colors = colors;
        }
        #endregion

        #region Grid
        private void OnGridToggled(bool isOn)
        {
            // Toggle grid visibility
        }

        private void OnGridSizeChanged(float value)
        {
            Building.BuildingSystem.Instance?.SetGridSize(value);
        }
        #endregion

        #region Confirmation
        private System.Action _confirmAction;

        public void ShowConfirmation(string message, System.Action onConfirm)
        {
            _confirmPanel?.SetActive(true);
            if (_confirmText != null)
                _confirmText.text = message;
            _confirmAction = onConfirm;
        }

        private void OnConfirmYes()
        {
            _confirmPanel?.SetActive(false);
            _confirmAction?.Invoke();
            _confirmAction = null;
        }

        private void OnConfirmNo()
        {
            _confirmPanel?.SetActive(false);
            _confirmAction = null;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Open build mode UI.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            Building.BuildingSystem.Instance?.EnterBuildMode(Building.BuildingSystem.BuildMode.Place);
            RefreshItemGrid();
            UpdateResourcesDisplay();
            UpdateKingdomStats();
        }

        /// <summary>
        /// Close build mode UI.
        /// </summary>
        public void Hide()
        {
            Building.BuildingSystem.Instance?.ExitBuildMode();
            gameObject.SetActive(false);
        }
        #endregion
    }
}

