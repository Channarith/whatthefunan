using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WhatTheFunan.UI.Screens
{
    /// <summary>
    /// In-game HUD overlay during gameplay.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        #region UI References
        [Header("Health & Stamina")]
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Slider _staminaSlider;
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _staminaFill;
        [SerializeField] private TextMeshProUGUI _healthText;
        
        [Header("Character Portrait")]
        [SerializeField] private Image _characterPortrait;
        [SerializeField] private TextMeshProUGUI _characterLevel;
        [SerializeField] private Slider _xpSlider;
        
        [Header("Combat")]
        [SerializeField] private GameObject _combatModeIndicator;
        [SerializeField] private TextMeshProUGUI _combatModeText;
        [SerializeField] private TextMeshProUGUI _comboCounter;
        [SerializeField] private Animator _comboAnimator;
        
        [Header("Currency (Compact)")]
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _gemsText;
        
        [Header("Quest Tracker")]
        [SerializeField] private GameObject _questTracker;
        [SerializeField] private TextMeshProUGUI _questTitle;
        [SerializeField] private TextMeshProUGUI _questObjective;
        [SerializeField] private Slider _questProgress;
        
        [Header("Mini Map")]
        [SerializeField] private RawImage _miniMapImage;
        [SerializeField] private RectTransform _playerMarker;
        
        [Header("Action Buttons")]
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _specialButton;
        [SerializeField] private Button _dodgeButton;
        [SerializeField] private Button _interactButton;
        [SerializeField] private Image _specialCooldownOverlay;
        
        [Header("Pause & Menu")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _mapButton;
        
        [Header("Notifications")]
        [SerializeField] private RectTransform _notificationContainer;
        [SerializeField] private GameObject _notificationPrefab;
        #endregion

        #region State
        private int _currentCombo;
        private float _specialCooldown;
        private float _maxSpecialCooldown = 10f;
        #endregion

        #region Colors
        private Color _healthFullColor = new Color(0.2f, 0.8f, 0.2f);
        private Color _healthLowColor = new Color(0.9f, 0.2f, 0.2f);
        private Color _staminaColor = new Color(0.3f, 0.7f, 1f);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            InitializeHUD();
        }

        private void Update()
        {
            UpdateCooldowns();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Setup
        private void SetupButtons()
        {
            _attackButton?.onClick.AddListener(OnAttackPressed);
            _specialButton?.onClick.AddListener(OnSpecialPressed);
            _dodgeButton?.onClick.AddListener(OnDodgePressed);
            _interactButton?.onClick.AddListener(OnInteractPressed);
            _pauseButton?.onClick.AddListener(OnPausePressed);
            _inventoryButton?.onClick.AddListener(OnInventoryPressed);
            _mapButton?.onClick.AddListener(OnMapPressed);
        }

        private void InitializeHUD()
        {
            UpdateHealth(100, 100);
            UpdateStamina(100, 100);
            UpdateCombo(0);
            HideInteractButton();
        }

        private void SubscribeToEvents()
        {
            // Subscribe to game events
            Combat.CombatController.OnComboChanged += UpdateCombo;
            Combat.CombatController.OnCombatModeChanged += UpdateCombatMode;
            RPG.QuestSystem.OnQuestUpdated += UpdateQuestTracker;
            Economy.CurrencyManager.OnCurrencyChanged += UpdateCurrency;
        }

        private void UnsubscribeFromEvents()
        {
            Combat.CombatController.OnComboChanged -= UpdateCombo;
            Combat.CombatController.OnCombatModeChanged -= UpdateCombatMode;
            RPG.QuestSystem.OnQuestUpdated -= UpdateQuestTracker;
            Economy.CurrencyManager.OnCurrencyChanged -= UpdateCurrency;
        }
        #endregion

        #region Health & Stamina
        public void UpdateHealth(float current, float max)
        {
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = max;
                _healthSlider.value = current;
            }
            
            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
            
            if (_healthFill != null)
            {
                float percent = current / max;
                _healthFill.color = Color.Lerp(_healthLowColor, _healthFullColor, percent);
                
                // Pulse when low
                if (percent < 0.25f)
                {
                    StartCoroutine(PulseHealthBar());
                }
            }
        }

        public void UpdateStamina(float current, float max)
        {
            if (_staminaSlider != null)
            {
                _staminaSlider.maxValue = max;
                _staminaSlider.value = current;
            }
        }

        private IEnumerator PulseHealthBar()
        {
            if (_healthFill == null) yield break;
            
            float duration = 0.5f;
            float elapsed = 0;
            Color originalColor = _healthFill.color;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 4, 1);
                _healthFill.color = Color.Lerp(originalColor, Color.white, t * 0.3f);
                yield return null;
            }
            
            _healthFill.color = originalColor;
        }
        #endregion

        #region Combat
        public void UpdateCombo(int combo)
        {
            _currentCombo = combo;
            
            if (_comboCounter != null)
            {
                _comboCounter.gameObject.SetActive(combo > 1);
                _comboCounter.text = $"{combo}x COMBO!";
                
                // Scale animation
                if (combo > 1 && _comboAnimator != null)
                {
                    _comboAnimator.SetTrigger("Pop");
                }
            }
        }

        public void UpdateCombatMode(string mode)
        {
            if (_combatModeText != null)
            {
                _combatModeText.text = mode;
            }
            
            if (_combatModeIndicator != null)
            {
                _combatModeIndicator.SetActive(!string.IsNullOrEmpty(mode));
            }
        }

        private void UpdateCooldowns()
        {
            if (_specialCooldown > 0)
            {
                _specialCooldown -= Time.deltaTime;
                
                if (_specialCooldownOverlay != null)
                {
                    _specialCooldownOverlay.fillAmount = _specialCooldown / _maxSpecialCooldown;
                }
                
                if (_specialButton != null)
                {
                    _specialButton.interactable = _specialCooldown <= 0;
                }
            }
        }

        public void TriggerSpecialCooldown(float duration)
        {
            _maxSpecialCooldown = duration;
            _specialCooldown = duration;
        }
        #endregion

        #region Quest Tracker
        public void UpdateQuestTracker(RPG.Quest quest)
        {
            if (_questTracker == null) return;
            
            if (quest == null)
            {
                _questTracker.SetActive(false);
                return;
            }
            
            _questTracker.SetActive(true);
            
            if (_questTitle != null)
                _questTitle.text = quest.QuestName;
            
            if (_questObjective != null)
                _questObjective.text = quest.CurrentObjective;
            
            if (_questProgress != null)
            {
                _questProgress.maxValue = quest.TotalObjectives;
                _questProgress.value = quest.CompletedObjectives;
            }
        }
        #endregion

        #region Interact Button
        public void ShowInteractButton(string label = "Interact")
        {
            if (_interactButton != null)
            {
                _interactButton.gameObject.SetActive(true);
                var text = _interactButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = label;
            }
        }

        public void HideInteractButton()
        {
            if (_interactButton != null)
            {
                _interactButton.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Currency
        private void UpdateCurrency()
        {
            if (_coinsText != null)
            {
                int coins = Economy.CurrencyManager.Instance?.Coins ?? 0;
                _coinsText.text = FormatCompact(coins);
            }
            
            if (_gemsText != null)
            {
                int gems = Economy.CurrencyManager.Instance?.Gems ?? 0;
                _gemsText.text = FormatCompact(gems);
            }
        }

        private string FormatCompact(int num)
        {
            if (num >= 1000000) return $"{num / 1000000f:0.#}M";
            if (num >= 1000) return $"{num / 1000f:0.#}K";
            return num.ToString();
        }
        #endregion

        #region Notifications
        public void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            if (_notificationPrefab == null || _notificationContainer == null) return;
            
            var notification = Instantiate(_notificationPrefab, _notificationContainer);
            var text = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = message;
            
            // Auto-destroy after delay
            Destroy(notification, 3f);
        }

        public enum NotificationType { Info, Success, Warning, Error }
        #endregion

        #region Button Handlers
        private void OnAttackPressed()
        {
            Combat.CombatController.Instance?.TryAttack();
            Core.HapticManager.Instance?.TriggerLight();
        }

        private void OnSpecialPressed()
        {
            if (_specialCooldown <= 0)
            {
                Combat.CombatController.Instance?.TrySpecialAttack();
                Core.HapticManager.Instance?.TriggerMedium();
            }
        }

        private void OnDodgePressed()
        {
            Combat.CombatController.Instance?.TryDodge();
            Core.HapticManager.Instance?.TriggerLight();
        }

        private void OnInteractPressed()
        {
            // Broadcast interact event
            Characters.PlayerController.Instance?.TryInteract();
        }

        private void OnPausePressed()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Pause");
            Time.timeScale = 0;
        }

        private void OnInventoryPressed()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Inventory");
        }

        private void OnMapPressed()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Map");
        }
        #endregion

        #region Character Info
        public void UpdateCharacterInfo(string characterId, int level, float xpPercent)
        {
            if (_characterLevel != null)
                _characterLevel.text = $"Lv.{level}";
            
            if (_xpSlider != null)
            {
                _xpSlider.value = xpPercent;
            }
            
            // Load character portrait from resources
        }
        #endregion
    }
}

