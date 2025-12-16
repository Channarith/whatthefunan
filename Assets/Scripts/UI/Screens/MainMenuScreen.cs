using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WhatTheFunan.UI.Screens
{
    /// <summary>
    /// Main Menu screen controller with animated elements.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        #region UI References
        [Header("Logo & Title")]
        [SerializeField] private RectTransform _logoTransform;
        [SerializeField] private CanvasGroup _logoCanvasGroup;
        [SerializeField] private TextMeshProUGUI _versionText;
        
        [Header("Menu Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _codexButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _creditsButton;
        
        [Header("Character Showcase")]
        [SerializeField] private RectTransform _characterShowcase;
        [SerializeField] private Image _characterImage;
        [SerializeField] private TextMeshProUGUI _characterNameText;
        
        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _gemsText;
        
        [Header("Daily Reward")]
        [SerializeField] private Button _dailyRewardButton;
        [SerializeField] private GameObject _dailyRewardNotification;
        
        [Header("Animation Settings")]
        [SerializeField] private float _logoDropDuration = 0.8f;
        [SerializeField] private float _buttonStaggerDelay = 0.1f;
        [SerializeField] private AnimationCurve _bounceEase;
        #endregion

        #region State
        private bool _hasSaveData;
        private int _selectedCharacterIndex;
        private string[] _showcaseCharacters = { "champa", "kavi", "naga", "mealea", "makara", "prohm", "sena" };
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            CheckSaveData();
            UpdateCurrencyDisplay();
            CheckDailyReward();
            StartCoroutine(PlayIntroAnimation());
        }

        private void OnEnable()
        {
            // Subscribe to currency changes
            Economy.CurrencyManager.OnCurrencyChanged += UpdateCurrencyDisplay;
        }

        private void OnDisable()
        {
            Economy.CurrencyManager.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }
        #endregion

        #region Setup
        private void SetupButtons()
        {
            _playButton?.onClick.AddListener(OnPlayClicked);
            _continueButton?.onClick.AddListener(OnContinueClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _codexButton?.onClick.AddListener(OnCodexClicked);
            _shopButton?.onClick.AddListener(OnShopClicked);
            _creditsButton?.onClick.AddListener(OnCreditsClicked);
            _dailyRewardButton?.onClick.AddListener(OnDailyRewardClicked);
            
            // Set version
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void CheckSaveData()
        {
            _hasSaveData = Core.SaveSystem.Instance?.HasSaveData() ?? false;
            
            if (_continueButton != null)
            {
                _continueButton.interactable = _hasSaveData;
            }
        }

        private void CheckDailyReward()
        {
            bool canClaim = LiveOps.DailyRewards.Instance?.CanClaimToday() ?? false;
            
            if (_dailyRewardNotification != null)
            {
                _dailyRewardNotification.SetActive(canClaim);
            }
        }
        #endregion

        #region Animation
        private IEnumerator PlayIntroAnimation()
        {
            // Hide everything initially
            if (_logoCanvasGroup != null)
            {
                _logoCanvasGroup.alpha = 0;
            }
            
            var buttons = new Button[] { _playButton, _continueButton, _settingsButton, _codexButton, _shopButton, _creditsButton };
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    btn.transform.localScale = Vector3.zero;
                }
            }
            
            // Wait a moment
            yield return new WaitForSeconds(0.3f);
            
            // Animate logo drop
            yield return AnimateLogo();
            
            // Stagger animate buttons
            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    StartCoroutine(AnimateButtonIn(btn.transform));
                    yield return new WaitForSeconds(_buttonStaggerDelay);
                }
            }
            
            // Start character showcase rotation
            StartCoroutine(RotateCharacterShowcase());
        }

        private IEnumerator AnimateLogo()
        {
            if (_logoTransform == null || _logoCanvasGroup == null) yield break;
            
            Vector2 startPos = _logoTransform.anchoredPosition + Vector2.up * 200;
            Vector2 endPos = _logoTransform.anchoredPosition;
            _logoTransform.anchoredPosition = startPos;
            
            float elapsed = 0;
            while (elapsed < _logoDropDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _logoDropDuration;
                float easeT = _bounceEase?.Evaluate(t) ?? EaseOutBounce(t);
                
                _logoTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
                _logoCanvasGroup.alpha = Mathf.Lerp(0, 1, t * 2); // Fade in faster
                
                yield return null;
            }
            
            _logoTransform.anchoredPosition = endPos;
            _logoCanvasGroup.alpha = 1;
        }

        private IEnumerator AnimateButtonIn(Transform buttonTransform)
        {
            float duration = 0.3f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = EaseOutBack(t);
                buttonTransform.localScale = Vector3.one * scale;
                yield return null;
            }
            
            buttonTransform.localScale = Vector3.one;
        }

        private IEnumerator RotateCharacterShowcase()
        {
            while (true)
            {
                yield return new WaitForSeconds(5f);
                
                _selectedCharacterIndex = (_selectedCharacterIndex + 1) % _showcaseCharacters.Length;
                UpdateCharacterShowcase();
            }
        }

        private void UpdateCharacterShowcase()
        {
            string charId = _showcaseCharacters[_selectedCharacterIndex];
            
            // Would load character sprite and name from database
            if (_characterNameText != null)
            {
                _characterNameText.text = charId.ToUpper();
            }
        }
        #endregion

        #region Button Handlers
        private void OnPlayClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_button_click");
            Core.HapticManager.Instance?.TriggerLight();
            
            // Open character select or start new game
            UIManager.Instance?.ShowScreen("CharacterSelect");
        }

        private void OnContinueClicked()
        {
            if (!_hasSaveData) return;
            
            Core.AudioManager.Instance?.PlaySFX("sfx_button_click");
            Core.HapticManager.Instance?.TriggerLight();
            
            // Load save and go to game
            Core.SaveSystem.Instance?.LoadGame();
            Core.SceneController.Instance?.LoadScene("Gameplay");
        }

        private void OnSettingsClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Settings");
        }

        private void OnCodexClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Codex");
        }

        private void OnShopClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Shop");
        }

        private void OnCreditsClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_menu_open");
            UIManager.Instance?.ShowScreen("Credits");
        }

        private void OnDailyRewardClicked()
        {
            Core.AudioManager.Instance?.PlaySFX("sfx_coin_collect");
            UIManager.Instance?.ShowScreen("DailyReward");
        }
        #endregion

        #region Currency
        private void UpdateCurrencyDisplay()
        {
            if (_coinsText != null)
            {
                int coins = Economy.CurrencyManager.Instance?.Coins ?? 0;
                _coinsText.text = FormatNumber(coins);
            }
            
            if (_gemsText != null)
            {
                int gems = Economy.CurrencyManager.Instance?.Gems ?? 0;
                _gemsText.text = FormatNumber(gems);
            }
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:0.#}M";
            if (number >= 1000)
                return $"{number / 1000f:0.#}K";
            return number.ToString();
        }
        #endregion

        #region Easing Functions
        private float EaseOutBounce(float t)
        {
            if (t < 1 / 2.75f)
                return 7.5625f * t * t;
            else if (t < 2 / 2.75f)
                return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
            else if (t < 2.5f / 2.75f)
                return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
            else
                return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }
        #endregion
    }
}

