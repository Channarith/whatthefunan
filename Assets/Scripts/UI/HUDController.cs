using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// Controls the in-game HUD elements.
    /// Shows health, currency, minimap, quest tracker, etc.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        #region Singleton
        private static HUDController _instance;
        public static HUDController Instance => _instance;
        #endregion

        #region UI References
        [Header("Health & Energy")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Slider _energyBar;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _levelText;
        
        [Header("Currency")]
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _gemsText;
        
        [Header("Quest Tracker")]
        [SerializeField] private GameObject _questTrackerPanel;
        [SerializeField] private TextMeshProUGUI _questNameText;
        [SerializeField] private TextMeshProUGUI _questObjectiveText;
        [SerializeField] private Slider _questProgressBar;
        
        [Header("Minimap")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private GameObject _minimapPanel;
        
        [Header("Quick Access")]
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _mapButton;
        [SerializeField] private Button _questLogButton;
        
        [Header("Combat")]
        [SerializeField] private GameObject _combatPanel;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _dodgeButton;
        [SerializeField] private Button _specialButton;
        [SerializeField] private Slider _comboMeter;
        
        [Header("Interaction")]
        [SerializeField] private GameObject _interactionPrompt;
        [SerializeField] private TextMeshProUGUI _interactionText;
        
        [Header("Notifications")]
        [SerializeField] private Transform _notificationContainer;
        [SerializeField] private GameObject _notificationPrefab;
        
        [Header("Time/Weather")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Image _weatherIcon;
        #endregion

        #region Events
        public static event Action OnMenuPressed;
        public static event Action OnInventoryPressed;
        public static event Action OnMapPressed;
        public static event Action OnQuestLogPressed;
        public static event Action OnAttackPressed;
        public static event Action OnDodgePressed;
        public static event Action OnSpecialPressed;
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
            
            SetupButtons();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void SetupButtons()
        {
            if (_menuButton != null)
                _menuButton.onClick.AddListener(() => OnMenuPressed?.Invoke());
            if (_inventoryButton != null)
                _inventoryButton.onClick.AddListener(() => OnInventoryPressed?.Invoke());
            if (_mapButton != null)
                _mapButton.onClick.AddListener(() => OnMapPressed?.Invoke());
            if (_questLogButton != null)
                _questLogButton.onClick.AddListener(() => OnQuestLogPressed?.Invoke());
            if (_attackButton != null)
                _attackButton.onClick.AddListener(() => OnAttackPressed?.Invoke());
            if (_dodgeButton != null)
                _dodgeButton.onClick.AddListener(() => OnDodgePressed?.Invoke());
            if (_specialButton != null)
                _specialButton.onClick.AddListener(() => OnSpecialPressed?.Invoke());
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            Economy.CurrencyManager.OnCoinsChanged += UpdateCoins;
            Economy.CurrencyManager.OnGemsChanged += UpdateGems;
            RPG.QuestSystem.OnQuestStarted += UpdateQuestTracker;
            RPG.QuestSystem.OnObjectiveUpdated += OnObjectiveUpdated;
            Gameplay.WeatherSystem.OnTimeOfDayChanged += UpdateTimeDisplay;
        }

        private void UnsubscribeFromEvents()
        {
            Economy.CurrencyManager.OnCoinsChanged -= UpdateCoins;
            Economy.CurrencyManager.OnGemsChanged -= UpdateGems;
            RPG.QuestSystem.OnQuestStarted -= UpdateQuestTracker;
            RPG.QuestSystem.OnObjectiveUpdated -= OnObjectiveUpdated;
            Gameplay.WeatherSystem.OnTimeOfDayChanged -= UpdateTimeDisplay;
        }
        #endregion

        #region Health & Stats
        /// <summary>
        /// Update health display.
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            if (_healthBar != null)
            {
                _healthBar.value = current / max;
            }
            
            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        /// <summary>
        /// Update energy display.
        /// </summary>
        public void UpdateEnergy(float current, float max)
        {
            if (_energyBar != null)
            {
                _energyBar.value = current / max;
            }
        }

        /// <summary>
        /// Update level display.
        /// </summary>
        public void UpdateLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Lv.{level}";
            }
        }
        #endregion

        #region Currency
        private void UpdateCoins(int amount)
        {
            if (_coinsText != null)
            {
                _coinsText.text = FormatNumber(amount);
                // Animate
                AnimateCurrencyChange(_coinsText);
            }
        }

        private void UpdateGems(int amount)
        {
            if (_gemsText != null)
            {
                _gemsText.text = FormatNumber(amount);
                AnimateCurrencyChange(_gemsText);
            }
        }

        private void AnimateCurrencyChange(TextMeshProUGUI text)
        {
            // TODO: Animate with scale punch
            LeanTween.cancel(text.gameObject);
            text.transform.localScale = Vector3.one;
            LeanTween.scale(text.gameObject, Vector3.one * 1.2f, 0.1f)
                .setEaseOutQuad()
                .setOnComplete(() => 
                    LeanTween.scale(text.gameObject, Vector3.one, 0.1f).setEaseInQuad());
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString();
        }
        #endregion

        #region Quest Tracker
        private void UpdateQuestTracker(RPG.Quest quest)
        {
            if (_questTrackerPanel == null) return;
            
            _questTrackerPanel.SetActive(true);
            
            if (_questNameText != null)
                _questNameText.text = quest.questName;
            
            UpdateQuestObjective(quest);
        }

        private void UpdateQuestObjective(RPG.Quest quest)
        {
            if (quest.objectives.Count == 0) return;
            
            var currentObjective = quest.objectives.Find(o => !o.isCompleted) ?? quest.objectives[0];
            
            if (_questObjectiveText != null)
            {
                _questObjectiveText.text = $"{currentObjective.description} ({currentObjective.currentProgress}/{currentObjective.requiredProgress})";
            }
            
            if (_questProgressBar != null)
            {
                _questProgressBar.value = currentObjective.ProgressPercent;
            }
        }

        private void OnObjectiveUpdated(RPG.Quest quest, RPG.QuestObjective objective)
        {
            UpdateQuestObjective(quest);
        }

        /// <summary>
        /// Hide quest tracker.
        /// </summary>
        public void HideQuestTracker()
        {
            if (_questTrackerPanel != null)
            {
                _questTrackerPanel.SetActive(false);
            }
        }
        #endregion

        #region Combat
        /// <summary>
        /// Show combat UI.
        /// </summary>
        public void ShowCombatUI()
        {
            if (_combatPanel != null)
            {
                _combatPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Hide combat UI.
        /// </summary>
        public void HideCombatUI()
        {
            if (_combatPanel != null)
            {
                _combatPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Update combo meter.
        /// </summary>
        public void UpdateComboMeter(float value)
        {
            if (_comboMeter != null)
            {
                _comboMeter.value = value;
            }
        }
        #endregion

        #region Interaction
        /// <summary>
        /// Show interaction prompt.
        /// </summary>
        public void ShowInteractionPrompt(string text)
        {
            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(true);
            }
            
            if (_interactionText != null)
            {
                _interactionText.text = text;
            }
        }

        /// <summary>
        /// Hide interaction prompt.
        /// </summary>
        public void HideInteractionPrompt()
        {
            if (_interactionPrompt != null)
            {
                _interactionPrompt.SetActive(false);
            }
        }
        #endregion

        #region Notifications
        /// <summary>
        /// Show a HUD notification.
        /// </summary>
        public void ShowNotification(string message, Sprite icon = null)
        {
            if (_notificationContainer == null || _notificationPrefab == null) return;
            
            var notification = Instantiate(_notificationPrefab, _notificationContainer);
            
            // Configure notification
            var text = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = message;
            
            var image = notification.GetComponentInChildren<Image>();
            if (image != null && icon != null) image.sprite = icon;
            
            // Auto-destroy after delay
            Destroy(notification, 3f);
        }
        #endregion

        #region Time/Weather
        private void UpdateTimeDisplay(Gameplay.TimeOfDay timeOfDay)
        {
            if (_timeText != null)
            {
                _timeText.text = Gameplay.WeatherSystem.Instance?.GetTimeString() ?? "";
            }
        }
        #endregion

        #region Minimap
        /// <summary>
        /// Toggle minimap visibility.
        /// </summary>
        public void ToggleMinimap()
        {
            if (_minimapPanel != null)
            {
                _minimapPanel.SetActive(!_minimapPanel.activeSelf);
            }
        }
        #endregion
    }
}

