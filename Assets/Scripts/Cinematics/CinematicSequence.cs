using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Video;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Cinematics
{
    /// <summary>
    /// Individual cinematic sequence that can mix Timeline, Video, and scripted events.
    /// </summary>
    public class CinematicSequence : MonoBehaviour
    {
        #region Enums
        public enum SequenceType
        {
            Timeline,       // Unity Timeline with Cinemachine
            Video,          // Pre-rendered video (AI-generated or recorded)
            Hybrid,         // Timeline with video insert
            Scripted        // Pure code-driven sequence
        }

        public enum TransitionType
        {
            Cut,
            Fade,
            CrossDissolve,
            Wipe,
            CircleOpen,
            CircleClose,
            Custom
        }
        #endregion

        #region Events
        public static event Action<string> OnSequenceStarted;
        public static event Action<string> OnSequenceEnded;
        public static event Action<string, float> OnSequenceProgress;
        public static event Action<string> OnSubtitleChanged;
        #endregion

        #region Sequence Data
        [Header("Sequence Info")]
        [SerializeField] private string _sequenceId;
        [SerializeField] private string _sequenceName;
        [SerializeField] private SequenceType _sequenceType = SequenceType.Timeline;
        [SerializeField] private bool _isSkippable = true;
        [SerializeField] private float _skipHoldTime = 1.5f;
        
        [Header("Timeline Settings")]
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private TimelineAsset _timelineAsset;
        
        [Header("Video Settings")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private VideoClip _videoClip;
        [SerializeField] private RenderTexture _videoRenderTexture;
        
        [Header("Transitions")]
        [SerializeField] private TransitionType _introTransition = TransitionType.Fade;
        [SerializeField] private TransitionType _outroTransition = TransitionType.Fade;
        [SerializeField] private float _transitionDuration = 1f;
        [SerializeField] private Material _transitionMaterial;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _voiceoverClip;
        [SerializeField] private AudioClip _musicClip;
        [SerializeField] private bool _fadeGameMusic = true;
        
        [Header("Subtitles")]
        [SerializeField] private List<SubtitleEntry> _subtitles = new List<SubtitleEntry>();
        
        [Header("Events")]
        [SerializeField] private List<SequenceEvent> _sequenceEvents = new List<SequenceEvent>();
        #endregion

        #region State
        private bool _isPlaying;
        private bool _isSkipping;
        private float _skipHoldProgress;
        private float _currentTime;
        private float _totalDuration;
        private int _currentSubtitleIndex;
        private Coroutine _playbackCoroutine;
        
        public bool IsPlaying => _isPlaying;
        public float Progress => _totalDuration > 0 ? _currentTime / _totalDuration : 0;
        public string SequenceId => _sequenceId;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetupComponents();
        }

        private void Update()
        {
            if (_isPlaying)
            {
                UpdateSkipInput();
                UpdateSubtitles();
            }
        }

        private void OnDisable()
        {
            Stop();
        }
        #endregion

        #region Setup
        private void SetupComponents()
        {
            if (_director == null)
                _director = GetComponent<PlayableDirector>();
            
            if (_videoPlayer == null)
                _videoPlayer = GetComponent<VideoPlayer>();
            
            // Configure video player
            if (_videoPlayer != null)
            {
                _videoPlayer.playOnAwake = false;
                _videoPlayer.isLooping = false;
                _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                
                if (_videoRenderTexture != null)
                    _videoPlayer.targetTexture = _videoRenderTexture;
            }
        }
        #endregion

        #region Playback Control
        /// <summary>
        /// Play the cinematic sequence.
        /// </summary>
        public void Play()
        {
            if (_isPlaying) return;
            
            _isPlaying = true;
            _currentTime = 0;
            _currentSubtitleIndex = 0;
            _skipHoldProgress = 0;
            
            OnSequenceStarted?.Invoke(_sequenceId);
            
            // Fade game music
            if (_fadeGameMusic)
            {
                Core.AudioManager.Instance?.FadeOutMusic(0.5f);
            }
            
            _playbackCoroutine = StartCoroutine(PlaySequence());
        }

        /// <summary>
        /// Stop the cinematic.
        /// </summary>
        public void Stop()
        {
            if (!_isPlaying) return;
            
            _isPlaying = false;
            
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
            
            // Stop Timeline
            if (_director != null)
            {
                _director.Stop();
            }
            
            // Stop Video
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
            }
            
            OnSequenceEnded?.Invoke(_sequenceId);
        }

        /// <summary>
        /// Skip to end (if allowed).
        /// </summary>
        public void Skip()
        {
            if (!_isSkippable || !_isPlaying) return;
            
            _isSkipping = true;
            
            // Quick fade out
            StartCoroutine(SkipSequence());
        }

        private IEnumerator PlaySequence()
        {
            // Intro transition
            yield return PlayTransition(_introTransition, true);
            
            // Play based on type
            switch (_sequenceType)
            {
                case SequenceType.Timeline:
                    yield return PlayTimeline();
                    break;
                    
                case SequenceType.Video:
                    yield return PlayVideo();
                    break;
                    
                case SequenceType.Hybrid:
                    yield return PlayHybrid();
                    break;
                    
                case SequenceType.Scripted:
                    yield return PlayScripted();
                    break;
            }
            
            // Outro transition
            yield return PlayTransition(_outroTransition, false);
            
            // Complete
            Stop();
            
            // Restore game music
            if (_fadeGameMusic)
            {
                Core.AudioManager.Instance?.FadeInMusic(0.5f);
            }
        }

        private IEnumerator PlayTimeline()
        {
            if (_director == null || _timelineAsset == null)
            {
                Debug.LogWarning($"[CinematicSequence] Timeline not configured for {_sequenceId}");
                yield break;
            }
            
            _director.playableAsset = _timelineAsset;
            _totalDuration = (float)_timelineAsset.duration;
            
            _director.Play();
            
            while (_director.state == PlayState.Playing && !_isSkipping)
            {
                _currentTime = (float)_director.time;
                OnSequenceProgress?.Invoke(_sequenceId, Progress);
                
                // Check for timed events
                ProcessSequenceEvents();
                
                yield return null;
            }
        }

        private IEnumerator PlayVideo()
        {
            if (_videoPlayer == null || _videoClip == null)
            {
                Debug.LogWarning($"[CinematicSequence] Video not configured for {_sequenceId}");
                yield break;
            }
            
            _videoPlayer.clip = _videoClip;
            _totalDuration = (float)_videoClip.length;
            
            // Wait for video to prepare
            _videoPlayer.Prepare();
            while (!_videoPlayer.isPrepared)
            {
                yield return null;
            }
            
            _videoPlayer.Play();
            
            while (_videoPlayer.isPlaying && !_isSkipping)
            {
                _currentTime = (float)_videoPlayer.time;
                OnSequenceProgress?.Invoke(_sequenceId, Progress);
                ProcessSequenceEvents();
                yield return null;
            }
        }

        private IEnumerator PlayHybrid()
        {
            // Play timeline with video insert points
            if (_director != null && _timelineAsset != null)
            {
                _director.playableAsset = _timelineAsset;
                _director.Play();
                
                // Timeline handles video through custom tracks
                while (_director.state == PlayState.Playing && !_isSkipping)
                {
                    _currentTime = (float)_director.time;
                    OnSequenceProgress?.Invoke(_sequenceId, Progress);
                    yield return null;
                }
            }
        }

        private IEnumerator PlayScripted()
        {
            // Execute scripted events in order
            foreach (var evt in _sequenceEvents)
            {
                if (_isSkipping) break;
                
                yield return new WaitForSeconds(evt.time - _currentTime);
                _currentTime = evt.time;
                
                ExecuteEvent(evt);
                OnSequenceProgress?.Invoke(_sequenceId, Progress);
            }
        }

        private IEnumerator SkipSequence()
        {
            yield return PlayTransition(TransitionType.Fade, false);
            Stop();
        }
        #endregion

        #region Transitions
        private IEnumerator PlayTransition(TransitionType type, bool isIntro)
        {
            float elapsed = 0;
            float duration = _transitionDuration;
            
            // Get transition overlay
            CanvasGroup transitionOverlay = CinematicsManager.Instance?.GetTransitionOverlay();
            
            if (transitionOverlay == null)
            {
                yield break;
            }
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                switch (type)
                {
                    case TransitionType.Fade:
                        transitionOverlay.alpha = isIntro ? 1 - t : t;
                        break;
                        
                    case TransitionType.CrossDissolve:
                        transitionOverlay.alpha = isIntro ? 1 - t : t;
                        break;
                        
                    // Other transitions would use shader effects
                    default:
                        transitionOverlay.alpha = isIntro ? 1 - t : t;
                        break;
                }
                
                yield return null;
            }
            
            transitionOverlay.alpha = isIntro ? 0 : 1;
        }
        #endregion

        #region Skip Input
        private void UpdateSkipInput()
        {
            if (!_isSkippable) return;
            
            bool skipPressed = Input.GetKey(KeyCode.Escape) || 
                              Input.GetKey(KeyCode.Space) ||
                              Input.touchCount > 0;
            
            if (skipPressed)
            {
                _skipHoldProgress += Time.deltaTime;
                
                // Show skip progress UI
                CinematicsManager.Instance?.UpdateSkipProgress(_skipHoldProgress / _skipHoldTime);
                
                if (_skipHoldProgress >= _skipHoldTime)
                {
                    Skip();
                }
            }
            else
            {
                _skipHoldProgress = Mathf.Max(0, _skipHoldProgress - Time.deltaTime * 2);
                CinematicsManager.Instance?.UpdateSkipProgress(_skipHoldProgress / _skipHoldTime);
            }
        }
        #endregion

        #region Subtitles
        private void UpdateSubtitles()
        {
            if (_subtitles == null || _subtitles.Count == 0) return;
            
            // Find current subtitle
            for (int i = _subtitles.Count - 1; i >= 0; i--)
            {
                if (_currentTime >= _subtitles[i].startTime && _currentTime <= _subtitles[i].endTime)
                {
                    if (i != _currentSubtitleIndex)
                    {
                        _currentSubtitleIndex = i;
                        OnSubtitleChanged?.Invoke(_subtitles[i].text);
                    }
                    return;
                }
            }
            
            // No subtitle active
            OnSubtitleChanged?.Invoke("");
        }
        #endregion

        #region Sequence Events
        private void ProcessSequenceEvents()
        {
            foreach (var evt in _sequenceEvents)
            {
                if (!evt.triggered && _currentTime >= evt.time)
                {
                    evt.triggered = true;
                    ExecuteEvent(evt);
                }
            }
        }

        private void ExecuteEvent(SequenceEvent evt)
        {
            switch (evt.eventType)
            {
                case SequenceEventType.PlaySound:
                    Core.AudioManager.Instance?.PlaySFX(evt.stringValue);
                    break;
                    
                case SequenceEventType.ShowCharacter:
                    // Trigger character reveal
                    break;
                    
                case SequenceEventType.CameraShake:
                    CinematicsManager.Instance?.TriggerCameraShake(evt.floatValue);
                    break;
                    
                case SequenceEventType.SlowMotion:
                    Time.timeScale = evt.floatValue;
                    break;
                    
                case SequenceEventType.UnlockCodex:
                    Codex.CodexSystem.Instance?.UnlockEntry(evt.stringValue);
                    break;
                    
                case SequenceEventType.Achievement:
                    Achievements.AchievementSystem.Instance?.Unlock(evt.stringValue);
                    break;
                    
                case SequenceEventType.Custom:
                    evt.customCallback?.Invoke();
                    break;
            }
        }
        #endregion
    }

    #region Data Classes
    [Serializable]
    public class SubtitleEntry
    {
        public float startTime;
        public float endTime;
        [TextArea(2, 4)]
        public string text;
        public string speakerName;
        public string localizationKey;
    }

    [Serializable]
    public class SequenceEvent
    {
        public float time;
        public SequenceEventType eventType;
        public string stringValue;
        public float floatValue;
        public Action customCallback;
        [HideInInspector] public bool triggered;
    }

    public enum SequenceEventType
    {
        PlaySound,
        ShowCharacter,
        CameraShake,
        SlowMotion,
        UnlockCodex,
        Achievement,
        Custom
    }
    #endregion
}

