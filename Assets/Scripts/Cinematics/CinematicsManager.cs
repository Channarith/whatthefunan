using UnityEngine;
using UnityEngine.Video;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Cinematics
{
    /// <summary>
    /// Manages cinematic sequences using Unity Timeline and Video Player.
    /// Handles story transitions, chapter intros, and cutscenes.
    /// </summary>
    public class CinematicsManager : MonoBehaviour
    {
        #region Singleton
        private static CinematicsManager _instance;
        public static CinematicsManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<string> OnCinematicStarted;
        public static event Action<string> OnCinematicEnded;
        public static event Action OnCinematicSkipped;
        #endregion

        #region Cinematic Data
        [Header("Cinematics")]
        [SerializeField] private List<CinematicData> _cinematics = new List<CinematicData>();
        
        private Dictionary<string, CinematicData> _cinematicLookup = new Dictionary<string, CinematicData>();
        private HashSet<string> _watchedCinematics = new HashSet<string>();
        #endregion

        #region Components
        [Header("Video Player")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private UnityEngine.UI.RawImage _videoDisplay;
        [SerializeField] private AudioSource _videoAudioSource;
        
        [Header("UI")]
        [SerializeField] private GameObject _cinematicCanvas;
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField] private UnityEngine.UI.Button _skipButton;
        [SerializeField] private GameObject _skipPrompt;
        [SerializeField] private TMPro.TextMeshProUGUI _subtitleText;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private bool _allowSkip = true;
        [SerializeField] private float _skipHoldDuration = 1f;
        #endregion

        #region State
        private CinematicData _currentCinematic;
        private bool _isPlaying;
        private bool _isSkipping;
        private float _skipHoldTimer;
        private Action _onCompleteCallback;
        
        public bool IsPlaying => _isPlaying;
        public CinematicData CurrentCinematic => _currentCinematic;
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
            
            InitializeCinematics();
            SetupVideoPlayer();
            LoadWatchedCinematics();
        }

        private void Update()
        {
            if (_isPlaying && _allowSkip)
            {
                HandleSkipInput();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeCinematics()
        {
            _cinematicLookup.Clear();
            foreach (var cinematic in _cinematics)
            {
                _cinematicLookup[cinematic.cinematicId] = cinematic;
            }
        }

        private void SetupVideoPlayer()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.loopPointReached += OnVideoEnded;
                _videoPlayer.errorReceived += OnVideoError;
            }
        }
        #endregion

        #region Playback
        /// <summary>
        /// Play a cinematic by ID.
        /// </summary>
        public bool PlayCinematic(string cinematicId, Action onComplete = null)
        {
            if (!_cinematicLookup.TryGetValue(cinematicId, out CinematicData cinematic))
            {
                Debug.LogWarning($"[CinematicsManager] Cinematic not found: {cinematicId}");
                onComplete?.Invoke();
                return false;
            }
            
            return PlayCinematic(cinematic, onComplete);
        }

        /// <summary>
        /// Play a cinematic.
        /// </summary>
        public bool PlayCinematic(CinematicData cinematic, Action onComplete = null)
        {
            if (cinematic == null)
            {
                onComplete?.Invoke();
                return false;
            }
            
            if (_isPlaying)
            {
                Debug.LogWarning("[CinematicsManager] Cinematic already playing");
                return false;
            }
            
            _currentCinematic = cinematic;
            _onCompleteCallback = onComplete;
            
            StartCoroutine(PlayCinematicRoutine(cinematic));
            
            return true;
        }

        private IEnumerator PlayCinematicRoutine(CinematicData cinematic)
        {
            _isPlaying = true;
            
            // Pause game
            Core.GameManager.Instance?.PauseGame();
            UI.UIManager.Instance?.HideHUD();
            
            // Fade in
            yield return FadeIn();
            
            // Show cinematic canvas
            if (_cinematicCanvas != null)
            {
                _cinematicCanvas.SetActive(true);
            }
            
            // Show skip button
            if (_skipButton != null && _allowSkip && cinematic.canSkip)
            {
                _skipButton.gameObject.SetActive(true);
            }
            
            OnCinematicStarted?.Invoke(cinematic.cinematicId);
            
            // Play based on type
            switch (cinematic.type)
            {
                case CinematicType.Video:
                    yield return PlayVideoRoutine(cinematic);
                    break;
                    
                case CinematicType.Timeline:
                    yield return PlayTimelineRoutine(cinematic);
                    break;
                    
                case CinematicType.Slideshow:
                    yield return PlaySlideshowRoutine(cinematic);
                    break;
            }
            
            // Mark as watched
            _watchedCinematics.Add(cinematic.cinematicId);
            SaveWatchedCinematics();
            
            // Cleanup
            yield return EndCinematic();
        }

        private IEnumerator PlayVideoRoutine(CinematicData cinematic)
        {
            if (_videoPlayer == null || cinematic.videoClip == null)
            {
                yield break;
            }
            
            _videoPlayer.clip = cinematic.videoClip;
            _videoPlayer.Play();
            
            // Wait for video to end or skip
            while (_videoPlayer.isPlaying && !_isSkipping)
            {
                UpdateSubtitles(cinematic, (float)_videoPlayer.time);
                yield return null;
            }
            
            _videoPlayer.Stop();
        }

        private IEnumerator PlayTimelineRoutine(CinematicData cinematic)
        {
            // TODO: Play Unity Timeline
            // var director = GetComponent<PlayableDirector>();
            // director.Play(cinematic.timelineAsset);
            // while (director.state == PlayState.Playing && !_isSkipping)
            // {
            //     yield return null;
            // }
            
            yield return new WaitForSeconds(cinematic.duration);
        }

        private IEnumerator PlaySlideshowRoutine(CinematicData cinematic)
        {
            foreach (var slide in cinematic.slides)
            {
                if (_isSkipping) break;
                
                // Show slide
                // TODO: Display slide image with transition
                
                // Show text
                if (_subtitleText != null)
                {
                    _subtitleText.text = slide.text;
                }
                
                yield return new WaitForSeconds(slide.duration);
            }
        }

        private IEnumerator EndCinematic()
        {
            // Hide UI
            if (_skipButton != null)
            {
                _skipButton.gameObject.SetActive(false);
            }
            
            if (_subtitleText != null)
            {
                _subtitleText.text = "";
            }
            
            // Fade out
            yield return FadeOut();
            
            // Hide canvas
            if (_cinematicCanvas != null)
            {
                _cinematicCanvas.SetActive(false);
            }
            
            // Resume game
            Core.GameManager.Instance?.ResumeGame();
            UI.UIManager.Instance?.ShowHUD();
            
            OnCinematicEnded?.Invoke(_currentCinematic.cinematicId);
            
            _isPlaying = false;
            _isSkipping = false;
            _currentCinematic = null;
            
            // Invoke callback
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        }
        #endregion

        #region Skip
        private void HandleSkipInput()
        {
            // Hold to skip
            if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            {
                _skipHoldTimer += Time.unscaledDeltaTime;
                
                // Update skip progress UI
                if (_skipPrompt != null)
                {
                    _skipPrompt.SetActive(true);
                }
                
                if (_skipHoldTimer >= _skipHoldDuration)
                {
                    SkipCinematic();
                }
            }
            else
            {
                _skipHoldTimer = 0f;
                
                if (_skipPrompt != null)
                {
                    _skipPrompt.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Skip the current cinematic.
        /// </summary>
        public void SkipCinematic()
        {
            if (!_isPlaying || !_currentCinematic.canSkip) return;
            
            _isSkipping = true;
            OnCinematicSkipped?.Invoke();
            
            Debug.Log("[CinematicsManager] Cinematic skipped");
        }
        #endregion

        #region Subtitles
        private void UpdateSubtitles(CinematicData cinematic, float time)
        {
            if (_subtitleText == null) return;
            if (!Accessibility.AccessibilityManager.Instance?.SubtitlesEnabled ?? false)
            {
                _subtitleText.text = "";
                return;
            }
            
            // Find active subtitle
            foreach (var subtitle in cinematic.subtitles)
            {
                if (time >= subtitle.startTime && time <= subtitle.endTime)
                {
                    _subtitleText.text = subtitle.text;
                    return;
                }
            }
            
            _subtitleText.text = "";
        }
        #endregion

        #region Video Events
        private void OnVideoEnded(VideoPlayer vp)
        {
            Debug.Log("[CinematicsManager] Video ended");
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[CinematicsManager] Video error: {message}");
            _isSkipping = true; // Skip on error
        }
        #endregion

        #region Fade
        private IEnumerator FadeIn()
        {
            if (_fadeOverlay == null) yield break;
            
            _fadeOverlay.gameObject.SetActive(true);
            _fadeOverlay.alpha = 0f;
            
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = elapsed / _fadeDuration;
                yield return null;
            }
            
            _fadeOverlay.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (_fadeOverlay == null) yield break;
            
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _fadeOverlay.alpha = 1f - (elapsed / _fadeDuration);
                yield return null;
            }
            
            _fadeOverlay.alpha = 0f;
            _fadeOverlay.gameObject.SetActive(false);
        }
        #endregion

        #region Query
        /// <summary>
        /// Check if a cinematic has been watched.
        /// </summary>
        public bool HasWatched(string cinematicId)
        {
            return _watchedCinematics.Contains(cinematicId);
        }

        /// <summary>
        /// Get cinematic data.
        /// </summary>
        public CinematicData GetCinematic(string cinematicId)
        {
            return _cinematicLookup.GetValueOrDefault(cinematicId, null);
        }
        #endregion

        #region Save/Load
        private void SaveWatchedCinematics()
        {
            string data = string.Join(",", _watchedCinematics);
            PlayerPrefs.SetString("WatchedCinematics", data);
            PlayerPrefs.Save();
        }

        private void LoadWatchedCinematics()
        {
            string data = PlayerPrefs.GetString("WatchedCinematics", "");
            if (!string.IsNullOrEmpty(data))
            {
                _watchedCinematics = new HashSet<string>(data.Split(','));
            }
        }
        #endregion
    }

    #region Cinematic Data Classes
    public enum CinematicType
    {
        Video,      // Pre-rendered video (Runway/Pika generated)
        Timeline,   // Unity Timeline sequence
        Slideshow   // Image sequence with text
    }

    [Serializable]
    public class CinematicData
    {
        [Header("Identity")]
        public string cinematicId;
        public string cinematicName;
        public CinematicType type;
        
        [Header("Content")]
        public VideoClip videoClip;
        // public PlayableAsset timelineAsset;
        public List<CinematicSlide> slides = new List<CinematicSlide>();
        public float duration;
        
        [Header("Settings")]
        public bool canSkip = true;
        public bool playOnce = false;
        
        [Header("Subtitles")]
        public List<SubtitleEntry> subtitles = new List<SubtitleEntry>();
        
        [Header("Story")]
        public int chapter;
        public string description;
    }

    [Serializable]
    public class CinematicSlide
    {
        public Sprite image;
        [TextArea] public string text;
        public float duration = 3f;
        public string animationType;
    }

    [Serializable]
    public class SubtitleEntry
    {
        public float startTime;
        public float endTime;
        [TextArea] public string text;
        public string speaker;
    }
    #endregion
}

