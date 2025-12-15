using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Core
{
    /// <summary>
    /// Manages all audio in the game including music, SFX, and voice.
    /// Supports adaptive music system for dynamic soundtrack changes.
    /// Features traditional Cambodian/Southeast Asian instrument integration.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action<string> OnMusicChanged;
        public static event Action<float> OnMusicVolumeChanged;
        public static event Action<float> OnSFXVolumeChanged;
        #endregion

        #region Audio Mixer Groups
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _voiceGroup;
        [SerializeField] private AudioMixerGroup _ambientGroup;
        #endregion

        #region Audio Sources
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSourceA;
        [SerializeField] private AudioSource _musicSourceB;
        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private int _sfxPoolSize = 10;
        
        private List<AudioSource> _sfxPool;
        private AudioSource _currentMusicSource;
        private bool _usingSourceA = true;
        #endregion

        #region Music Categories (Authentic Instruments)
        /// <summary>
        /// Music categories using traditional Southeast Asian instruments:
        /// - Chapei Dong Veng (string) - melancholic, storytelling
        /// - Roneat Ek/Thung (xylophone) - upbeat, adventure
        /// - Skor Thom (drum) - combat, intensity
        /// - Tro (fiddle) - emotional scenes
        /// - Khim (hammered dulcimer) - mystery, temples
        /// - Sralai (oboe) - ceremonial, apsara
        /// </summary>
        public enum MusicCategory
        {
            MainMenu,
            Exploration,
            ExplorationCalm,
            ExplorationTense,
            Combat,
            CombatBoss,
            Victory,
            Defeat,
            Cutscene,
            CutsceneEmotional,
            Dialogue,
            Temple,
            MiniGame,
            FunanCity,
            JungleZone,
            NagaKingdom
        }
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private float _musicCrossfadeDuration = 2f;
        [SerializeField] private float _defaultMusicVolume = 0.8f;
        [SerializeField] private float _defaultSFXVolume = 1f;
        [SerializeField] private float _defaultVoiceVolume = 1f;
        
        private float _musicVolume;
        private float _sfxVolume;
        private float _voiceVolume;
        #endregion

        #region Music Library
        [Header("Music Tracks")]
        [SerializeField] private List<MusicTrack> _musicLibrary = new List<MusicTrack>();
        
        [Serializable]
        public class MusicTrack
        {
            public string id;
            public string displayName;
            public MusicCategory category;
            public AudioClip clip;
            public bool loops = true;
            [Range(0f, 1f)] public float volume = 1f;
            [TextArea] public string description; // e.g., "Traditional Roneat melody for jungle exploration"
        }
        
        private Dictionary<string, MusicTrack> _musicLookup;
        private string _currentMusicId;
        #endregion

        #region SFX Library
        [Header("Sound Effects")]
        [SerializeField] private List<SFXEntry> _sfxLibrary = new List<SFXEntry>();
        
        [Serializable]
        public class SFXEntry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitchMin = 1f;
            [Range(0.1f, 3f)] public float pitchMax = 1f;
        }
        
        private Dictionary<string, SFXEntry> _sfxLookup;
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
            
            Initialize();
        }

        private void Initialize()
        {
            // Create audio sources if not assigned
            if (_musicSourceA == null)
            {
                _musicSourceA = CreateAudioSource("MusicSourceA", _musicGroup, true);
            }
            if (_musicSourceB == null)
            {
                _musicSourceB = CreateAudioSource("MusicSourceB", _musicGroup, true);
            }
            if (_ambientSource == null)
            {
                _ambientSource = CreateAudioSource("AmbientSource", _ambientGroup, true);
            }
            
            _currentMusicSource = _musicSourceA;
            
            // Create SFX pool
            _sfxPool = new List<AudioSource>();
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                AudioSource sfxSource = CreateAudioSource($"SFXSource_{i}", _sfxGroup, false);
                _sfxPool.Add(sfxSource);
            }
            
            // Build lookup dictionaries
            _musicLookup = new Dictionary<string, MusicTrack>();
            foreach (var track in _musicLibrary)
            {
                if (!string.IsNullOrEmpty(track.id))
                {
                    _musicLookup[track.id] = track;
                }
            }
            
            _sfxLookup = new Dictionary<string, SFXEntry>();
            foreach (var sfx in _sfxLibrary)
            {
                if (!string.IsNullOrEmpty(sfx.id))
                {
                    _sfxLookup[sfx.id] = sfx;
                }
            }
            
            // Load saved volumes
            LoadVolumeSettings();
            
            Debug.Log("[AudioManager] Initialized");
        }

        private AudioSource CreateAudioSource(string name, AudioMixerGroup group, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            
            AudioSource source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            source.loop = loop;
            source.playOnAwake = false;
            
            return source;
        }
        #endregion

        #region Music Control
        /// <summary>
        /// Play music by ID with crossfade.
        /// </summary>
        public void PlayMusic(string musicId)
        {
            if (string.IsNullOrEmpty(musicId) || musicId == _currentMusicId)
            {
                return;
            }

            if (!_musicLookup.TryGetValue(musicId, out MusicTrack track))
            {
                Debug.LogWarning($"[AudioManager] Music not found: {musicId}");
                return;
            }

            StartCoroutine(CrossfadeMusic(track));
        }

        /// <summary>
        /// Play music by category.
        /// </summary>
        public void PlayMusicByCategory(MusicCategory category)
        {
            foreach (var track in _musicLibrary)
            {
                if (track.category == category)
                {
                    PlayMusic(track.id);
                    return;
                }
            }
            
            Debug.LogWarning($"[AudioManager] No music found for category: {category}");
        }

        /// <summary>
        /// Stop music with fade out.
        /// </summary>
        public void StopMusic(float fadeTime = 1f)
        {
            StartCoroutine(FadeOutMusic(fadeTime));
        }

        /// <summary>
        /// Pause current music.
        /// </summary>
        public void PauseMusic()
        {
            _musicSourceA.Pause();
            _musicSourceB.Pause();
        }

        /// <summary>
        /// Resume paused music.
        /// </summary>
        public void ResumeMusic()
        {
            _currentMusicSource.UnPause();
        }

        private IEnumerator CrossfadeMusic(MusicTrack newTrack)
        {
            AudioSource outSource = _currentMusicSource;
            AudioSource inSource = _usingSourceA ? _musicSourceB : _musicSourceA;
            
            inSource.clip = newTrack.clip;
            inSource.loop = newTrack.loops;
            inSource.volume = 0f;
            inSource.Play();
            
            float elapsed = 0f;
            float outStartVolume = outSource.volume;
            float inTargetVolume = newTrack.volume * _musicVolume;
            
            while (elapsed < _musicCrossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _musicCrossfadeDuration;
                
                outSource.volume = Mathf.Lerp(outStartVolume, 0f, t);
                inSource.volume = Mathf.Lerp(0f, inTargetVolume, t);
                
                yield return null;
            }
            
            outSource.Stop();
            outSource.volume = 0f;
            inSource.volume = inTargetVolume;
            
            _currentMusicSource = inSource;
            _usingSourceA = !_usingSourceA;
            _currentMusicId = newTrack.id;
            
            OnMusicChanged?.Invoke(newTrack.id);
            Debug.Log($"[AudioManager] Now playing: {newTrack.displayName}");
        }

        private IEnumerator FadeOutMusic(float fadeTime)
        {
            float startVolume = _currentMusicSource.volume;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }
            
            _currentMusicSource.Stop();
            _currentMusicId = null;
        }
        #endregion

        #region SFX Control
        /// <summary>
        /// Play a sound effect by ID.
        /// </summary>
        public void PlaySFX(string sfxId)
        {
            if (!_sfxLookup.TryGetValue(sfxId, out SFXEntry sfx))
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {sfxId}");
                return;
            }

            PlaySFX(sfx.clip, sfx.volume, sfx.pitchMin, sfx.pitchMax);
        }

        /// <summary>
        /// Play a sound effect clip directly.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] No available SFX source");
                return;
            }

            source.clip = clip;
            source.volume = volume * _sfxVolume;
            source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            source.Play();
        }

        /// <summary>
        /// Play a sound effect at a specific position (3D sound).
        /// </summary>
        public void PlaySFXAtPosition(string sfxId, Vector3 position)
        {
            if (!_sfxLookup.TryGetValue(sfxId, out SFXEntry sfx))
            {
                Debug.LogWarning($"[AudioManager] SFX not found: {sfxId}");
                return;
            }

            AudioSource.PlayClipAtPoint(sfx.clip, position, sfx.volume * _sfxVolume);
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }
            return null;
        }
        #endregion

        #region Ambient Sound
        /// <summary>
        /// Play ambient loop (environment sounds).
        /// </summary>
        public void PlayAmbient(AudioClip clip, float fadeTime = 1f)
        {
            StartCoroutine(CrossfadeAmbient(clip, fadeTime));
        }

        /// <summary>
        /// Stop ambient sound.
        /// </summary>
        public void StopAmbient(float fadeTime = 1f)
        {
            StartCoroutine(FadeOutAmbient(fadeTime));
        }

        private IEnumerator CrossfadeAmbient(AudioClip clip, float fadeTime)
        {
            float startVolume = _ambientSource.volume;
            
            // Fade out current
            if (_ambientSource.isPlaying)
            {
                float elapsed = 0f;
                while (elapsed < fadeTime / 2)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeTime / 2));
                    yield return null;
                }
            }
            
            // Switch and fade in
            _ambientSource.clip = clip;
            _ambientSource.Play();
            
            float elapsed2 = 0f;
            while (elapsed2 < fadeTime / 2)
            {
                elapsed2 += Time.unscaledDeltaTime;
                _ambientSource.volume = Mathf.Lerp(0f, 1f, elapsed2 / (fadeTime / 2));
                yield return null;
            }
        }

        private IEnumerator FadeOutAmbient(float fadeTime)
        {
            float startVolume = _ambientSource.volume;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _ambientSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }
            
            _ambientSource.Stop();
        }
        #endregion

        #region Volume Control
        /// <summary>
        /// Set music volume (0-1).
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            
            if (_masterMixer != null)
            {
                float db = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _masterMixer.SetFloat("MusicVolume", db);
            }
            else
            {
                _musicSourceA.volume = _musicVolume;
                _musicSourceB.volume = _musicVolume;
            }
            
            SaveVolumeSettings();
            OnMusicVolumeChanged?.Invoke(_musicVolume);
        }

        /// <summary>
        /// Set SFX volume (0-1).
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            
            if (_masterMixer != null)
            {
                float db = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _masterMixer.SetFloat("SFXVolume", db);
            }
            
            SaveVolumeSettings();
            OnSFXVolumeChanged?.Invoke(_sfxVolume);
        }

        /// <summary>
        /// Set voice volume (0-1).
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            
            if (_masterMixer != null)
            {
                float db = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _masterMixer.SetFloat("VoiceVolume", db);
            }
            
            SaveVolumeSettings();
        }

        /// <summary>
        /// Set master volume (0-1).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (_masterMixer != null)
            {
                float db = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
                _masterMixer.SetFloat("MasterVolume", db);
            }
        }

        /// <summary>
        /// Mute/unmute all audio.
        /// </summary>
        public void SetMute(bool mute)
        {
            AudioListener.volume = mute ? 0f : 1f;
        }

        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public float VoiceVolume => _voiceVolume;

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
            PlayerPrefs.SetFloat("VoiceVolume", _voiceVolume);
            PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", _defaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", _defaultSFXVolume);
            _voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", _defaultVoiceVolume);
            
            SetMusicVolume(_musicVolume);
            SetSFXVolume(_sfxVolume);
            SetVoiceVolume(_voiceVolume);
        }
        #endregion

        #region Adaptive Music
        /// <summary>
        /// Transition music based on game state (called by GameManager).
        /// </summary>
        public void UpdateMusicForGameState(GameManager.GameState state)
        {
            switch (state)
            {
                case GameManager.GameState.MainMenu:
                    PlayMusicByCategory(MusicCategory.MainMenu);
                    break;
                case GameManager.GameState.Playing:
                    // Will be determined by current zone
                    break;
                case GameManager.GameState.InCombat:
                    PlayMusicByCategory(MusicCategory.Combat);
                    break;
                case GameManager.GameState.InCutscene:
                    PlayMusicByCategory(MusicCategory.Cutscene);
                    break;
                case GameManager.GameState.InDialogue:
                    // Keep current music or play dialogue music
                    break;
                case GameManager.GameState.InMiniGame:
                    PlayMusicByCategory(MusicCategory.MiniGame);
                    break;
            }
        }

        /// <summary>
        /// Update music based on current zone/scene.
        /// </summary>
        public void UpdateMusicForZone(string zoneName)
        {
            switch (zoneName)
            {
                case "FunanCity":
                    PlayMusicByCategory(MusicCategory.FunanCity);
                    break;
                case "JungleZone":
                    PlayMusicByCategory(MusicCategory.JungleZone);
                    break;
                case "NagaKingdom":
                    PlayMusicByCategory(MusicCategory.NagaKingdom);
                    break;
                case "TempleRuins":
                    PlayMusicByCategory(MusicCategory.Temple);
                    break;
                default:
                    PlayMusicByCategory(MusicCategory.Exploration);
                    break;
            }
        }
        #endregion
    }
}

