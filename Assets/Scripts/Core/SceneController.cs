using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace WhatTheFunan.Core
{
    /// <summary>
    /// Handles all scene loading and transitions with loading screens and fade effects.
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        #region Singleton
        private static SceneController _instance;
        public static SceneController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SceneController>();
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action<string> OnSceneLoadStarted;
        public static event Action<float> OnSceneLoadProgress;
        public static event Action<string> OnSceneLoadCompleted;
        public static event Action OnTransitionStarted;
        public static event Action OnTransitionCompleted;
        #endregion

        #region Scene Names
        public static class Scenes
        {
            public const string Bootstrap = "Bootstrap";
            public const string MainMenu = "MainMenu";
            public const string FunanCity = "FunanCity";
            public const string TempleRuins = "TempleRuins";
            public const string JungleZone = "JungleZone";
            public const string NagaKingdom = "NagaKingdom";
            public const string Loading = "Loading";
        }
        #endregion

        #region Settings
        [Header("Transition Settings")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private float _minimumLoadTime = 1f;
        [SerializeField] private bool _showLoadingScreen = true;
        
        [Header("References")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        #endregion

        #region State
        public bool IsLoading { get; private set; }
        public string CurrentSceneName => SceneManager.GetActiveScene().name;
        public float LoadProgress { get; private set; }
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
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion

        #region Scene Loading
        /// <summary>
        /// Load a scene by name with transition effects.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneController] Already loading a scene. Ignoring request to load: {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Load a scene with a loading screen.
        /// </summary>
        public void LoadSceneWithLoadingScreen(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneController] Already loading a scene. Ignoring request to load: {sceneName}");
                return;
            }

            StartCoroutine(LoadSceneWithLoadingScreenAsync(sceneName));
        }

        /// <summary>
        /// Reload the current scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(CurrentSceneName);
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            IsLoading = true;
            LoadProgress = 0f;
            OnSceneLoadStarted?.Invoke(sceneName);
            OnTransitionStarted?.Invoke();

            Debug.Log($"[SceneController] Loading scene: {sceneName}");

            // Fade out
            yield return StartCoroutine(Fade(1f));

            // Load the scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float elapsedTime = 0f;

            while (!asyncLoad.isDone)
            {
                elapsedTime += Time.unscaledDeltaTime;
                
                // Unity caps progress at 0.9 until allowSceneActivation is true
                LoadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(LoadProgress);

                if (asyncLoad.progress >= 0.9f && elapsedTime >= _minimumLoadTime)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            LoadProgress = 1f;
            OnSceneLoadProgress?.Invoke(LoadProgress);

            // Fade in
            yield return StartCoroutine(Fade(0f));

            IsLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            OnTransitionCompleted?.Invoke();

            Debug.Log($"[SceneController] Scene loaded: {sceneName}");
        }

        private IEnumerator LoadSceneWithLoadingScreenAsync(string sceneName)
        {
            IsLoading = true;
            LoadProgress = 0f;
            OnSceneLoadStarted?.Invoke(sceneName);
            OnTransitionStarted?.Invoke();

            Debug.Log($"[SceneController] Loading scene with loading screen: {sceneName}");

            // Fade out
            yield return StartCoroutine(Fade(1f));

            // Load loading screen first
            if (_showLoadingScreen)
            {
                yield return SceneManager.LoadSceneAsync(Scenes.Loading);
            }

            // Fade in to show loading screen
            yield return StartCoroutine(Fade(0f));

            // Now load the target scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float elapsedTime = 0f;

            while (!asyncLoad.isDone)
            {
                elapsedTime += Time.unscaledDeltaTime;
                
                LoadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                OnSceneLoadProgress?.Invoke(LoadProgress);

                if (asyncLoad.progress >= 0.9f && elapsedTime >= _minimumLoadTime)
                {
                    // Fade out loading screen
                    yield return StartCoroutine(Fade(1f));
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            LoadProgress = 1f;
            OnSceneLoadProgress?.Invoke(LoadProgress);

            // Fade in to new scene
            yield return StartCoroutine(Fade(0f));

            IsLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            OnTransitionCompleted?.Invoke();

            Debug.Log($"[SceneController] Scene loaded: {sceneName}");
        }
        #endregion

        #region Transitions
        private IEnumerator Fade(float targetAlpha)
        {
            if (_fadeCanvasGroup == null)
            {
                // No fade canvas, just wait a frame
                yield return null;
                yield break;
            }

            float startAlpha = _fadeCanvasGroup.alpha;
            float elapsedTime = 0f;

            _fadeCanvasGroup.gameObject.SetActive(true);

            while (elapsedTime < _fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float t = elapsedTime / _fadeDuration;
                _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _fadeCanvasGroup.alpha = targetAlpha;

            if (targetAlpha == 0f)
            {
                _fadeCanvasGroup.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Fade to black (for cinematic transitions).
        /// </summary>
        public void FadeToBlack(Action onComplete = null)
        {
            StartCoroutine(FadeToBlackCoroutine(onComplete));
        }

        private IEnumerator FadeToBlackCoroutine(Action onComplete)
        {
            yield return StartCoroutine(Fade(1f));
            onComplete?.Invoke();
        }

        /// <summary>
        /// Fade from black (for cinematic transitions).
        /// </summary>
        public void FadeFromBlack(Action onComplete = null)
        {
            StartCoroutine(FadeFromBlackCoroutine(onComplete));
        }

        private IEnumerator FadeFromBlackCoroutine(Action onComplete)
        {
            yield return StartCoroutine(Fade(0f));
            onComplete?.Invoke();
        }
        #endregion

        #region Callbacks
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneController] Scene activated: {scene.name}");
        }
        #endregion
    }
}

