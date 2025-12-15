using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// Centralized UI management system.
    /// Handles screen navigation, popups, overlays, and transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager _instance;
        public static UIManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<UIScreen> OnScreenOpened;
        public static event Action<UIScreen> OnScreenClosed;
        public static event Action<UIPopup> OnPopupOpened;
        public static event Action<UIPopup> OnPopupClosed;
        #endregion

        #region Screen Management
        [Header("Screens")]
        [SerializeField] private Transform _screenContainer;
        [SerializeField] private List<UIScreen> _screens = new List<UIScreen>();
        
        private Dictionary<string, UIScreen> _screenLookup = new Dictionary<string, UIScreen>();
        private Stack<UIScreen> _screenHistory = new Stack<UIScreen>();
        private UIScreen _currentScreen;
        
        public UIScreen CurrentScreen => _currentScreen;
        #endregion

        #region Popup Management
        [Header("Popups")]
        [SerializeField] private Transform _popupContainer;
        [SerializeField] private GameObject _popupBlocker;
        
        private List<UIPopup> _activePopups = new List<UIPopup>();
        public bool HasActivePopup => _activePopups.Count > 0;
        #endregion

        #region Overlay
        [Header("Overlays")]
        [SerializeField] private CanvasGroup _loadingOverlay;
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField] private float _fadeDuration = 0.3f;
        #endregion

        #region HUD
        [Header("HUD")]
        [SerializeField] private GameObject _gameHUD;
        
        public bool IsHUDVisible => _gameHUD != null && _gameHUD.activeSelf;
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
            
            InitializeScreens();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            // Handle back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleBackButton();
            }
        }

        private void InitializeScreens()
        {
            _screenLookup.Clear();
            foreach (var screen in _screens)
            {
                if (screen != null)
                {
                    _screenLookup[screen.ScreenId] = screen;
                    screen.gameObject.SetActive(false);
                }
            }
        }
        #endregion

        #region Screen Navigation
        /// <summary>
        /// Open a screen by ID.
        /// </summary>
        public void OpenScreen(string screenId, bool addToHistory = true)
        {
            if (!_screenLookup.TryGetValue(screenId, out UIScreen screen))
            {
                Debug.LogWarning($"[UIManager] Screen not found: {screenId}");
                return;
            }
            
            OpenScreen(screen, addToHistory);
        }

        /// <summary>
        /// Open a screen.
        /// </summary>
        public void OpenScreen(UIScreen screen, bool addToHistory = true)
        {
            if (screen == null) return;
            
            // Close current screen
            if (_currentScreen != null && _currentScreen != screen)
            {
                if (addToHistory)
                {
                    _screenHistory.Push(_currentScreen);
                }
                _currentScreen.Close();
                OnScreenClosed?.Invoke(_currentScreen);
            }
            
            // Open new screen
            _currentScreen = screen;
            screen.Open();
            
            OnScreenOpened?.Invoke(screen);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            Debug.Log($"[UIManager] Opened screen: {screen.ScreenId}");
        }

        /// <summary>
        /// Go back to previous screen.
        /// </summary>
        public void GoBack()
        {
            if (_screenHistory.Count > 0)
            {
                var previousScreen = _screenHistory.Pop();
                OpenScreen(previousScreen, false);
            }
            else if (_currentScreen != null)
            {
                _currentScreen.OnBackPressed();
            }
        }

        /// <summary>
        /// Clear screen history.
        /// </summary>
        public void ClearHistory()
        {
            _screenHistory.Clear();
        }

        /// <summary>
        /// Handle back button press.
        /// </summary>
        private void HandleBackButton()
        {
            // First close popups
            if (HasActivePopup)
            {
                CloseTopPopup();
                return;
            }
            
            // Then navigate back
            GoBack();
        }
        #endregion

        #region Popup Management
        /// <summary>
        /// Show a popup.
        /// </summary>
        public void ShowPopup(UIPopup popup)
        {
            if (popup == null) return;
            
            // Enable blocker
            if (_popupBlocker != null)
            {
                _popupBlocker.SetActive(true);
            }
            
            // Add to active list
            _activePopups.Add(popup);
            
            // Parent to popup container
            if (_popupContainer != null)
            {
                popup.transform.SetParent(_popupContainer, false);
            }
            
            popup.Show();
            OnPopupOpened?.Invoke(popup);
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
        }

        /// <summary>
        /// Close a popup.
        /// </summary>
        public void ClosePopup(UIPopup popup)
        {
            if (popup == null) return;
            
            _activePopups.Remove(popup);
            popup.Hide();
            
            // Disable blocker if no more popups
            if (!HasActivePopup && _popupBlocker != null)
            {
                _popupBlocker.SetActive(false);
            }
            
            OnPopupClosed?.Invoke(popup);
        }

        /// <summary>
        /// Close the top popup.
        /// </summary>
        public void CloseTopPopup()
        {
            if (_activePopups.Count > 0)
            {
                ClosePopup(_activePopups[_activePopups.Count - 1]);
            }
        }

        /// <summary>
        /// Close all popups.
        /// </summary>
        public void CloseAllPopups()
        {
            foreach (var popup in _activePopups.ToList())
            {
                ClosePopup(popup);
            }
        }
        #endregion

        #region Common Popups
        /// <summary>
        /// Show a confirmation dialog.
        /// </summary>
        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
            // TODO: Instantiate and configure confirmation popup
            Debug.Log($"[UIManager] Confirmation: {title} - {message}");
        }

        /// <summary>
        /// Show a message dialog.
        /// </summary>
        public void ShowMessage(string title, string message, Action onClose = null)
        {
            Debug.Log($"[UIManager] Message: {title} - {message}");
        }

        /// <summary>
        /// Show a reward popup.
        /// </summary>
        public void ShowReward(string title, int coins = 0, int gems = 0, string itemId = null)
        {
            Debug.Log($"[UIManager] Reward: {title} - Coins: {coins}, Gems: {gems}");
        }
        #endregion

        #region HUD
        /// <summary>
        /// Show the game HUD.
        /// </summary>
        public void ShowHUD()
        {
            if (_gameHUD != null)
            {
                _gameHUD.SetActive(true);
            }
        }

        /// <summary>
        /// Hide the game HUD.
        /// </summary>
        public void HideHUD()
        {
            if (_gameHUD != null)
            {
                _gameHUD.SetActive(false);
            }
        }
        #endregion

        #region Loading & Transitions
        /// <summary>
        /// Show loading overlay.
        /// </summary>
        public void ShowLoading()
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.gameObject.SetActive(true);
                _loadingOverlay.alpha = 1f;
            }
        }

        /// <summary>
        /// Hide loading overlay.
        /// </summary>
        public void HideLoading()
        {
            if (_loadingOverlay != null)
            {
                _loadingOverlay.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Fade to black.
        /// </summary>
        public void FadeOut(Action onComplete = null)
        {
            StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
        }

        /// <summary>
        /// Fade from black.
        /// </summary>
        public void FadeIn(Action onComplete = null)
        {
            StartCoroutine(FadeCoroutine(1f, 0f, onComplete));
        }

        private System.Collections.IEnumerator FadeCoroutine(float from, float to, Action onComplete)
        {
            if (_fadeOverlay == null) yield break;
            
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.alpha = from;
            
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            
            _fadeOverlay.alpha = to;
            
            if (to == 0f)
            {
                _fadeOverlay.gameObject.SetActive(false);
            }
            
            onComplete?.Invoke();
        }
        #endregion

        #region Toast Notifications
        /// <summary>
        /// Show a toast notification.
        /// </summary>
        public void ShowToast(string message, float duration = 2f)
        {
            // TODO: Implement toast UI
            Debug.Log($"[UIManager] Toast: {message}");
        }
        #endregion
    }

    #region Base UI Classes
    /// <summary>
    /// Base class for UI screens.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private string _screenId;
        public string ScreenId => _screenId;
        
        public virtual void Open()
        {
            gameObject.SetActive(true);
            OnOpen();
        }
        
        public virtual void Close()
        {
            OnClose();
            gameObject.SetActive(false);
        }
        
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        
        public virtual void OnBackPressed()
        {
            UIManager.Instance?.GoBack();
        }
    }

    /// <summary>
    /// Base class for UI popups.
    /// </summary>
    public abstract class UIPopup : MonoBehaviour
    {
        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }
        
        public virtual void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }
        
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        
        public void CloseThis()
        {
            UIManager.Instance?.ClosePopup(this);
        }
    }
    #endregion
}

