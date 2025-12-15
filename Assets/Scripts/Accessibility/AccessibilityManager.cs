using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Accessibility
{
    /// <summary>
    /// Manages accessibility features for inclusive gameplay.
    /// Includes text size, colorblind modes, subtitles, and input assistance.
    /// </summary>
    public class AccessibilityManager : MonoBehaviour
    {
        #region Singleton
        private static AccessibilityManager _instance;
        public static AccessibilityManager Instance => _instance;
        #endregion

        #region Events
        public static event Action OnSettingsChanged;
        public static event Action<float> OnTextScaleChanged;
        public static event Action<ColorblindMode> OnColorblindModeChanged;
        #endregion

        #region Settings
        [Header("Text & UI")]
        [SerializeField] private float _textScale = 1f;
        [SerializeField] private float _minTextScale = 0.75f;
        [SerializeField] private float _maxTextScale = 1.5f;
        [SerializeField] private bool _boldText = false;
        [SerializeField] private bool _highContrastUI = false;
        
        [Header("Colorblind Support")]
        [SerializeField] private ColorblindMode _colorblindMode = ColorblindMode.None;
        [SerializeField] private Material _colorblindMaterial;
        
        [Header("Audio")]
        [SerializeField] private bool _subtitlesEnabled = true;
        [SerializeField] private float _subtitleScale = 1f;
        [SerializeField] private bool _subtitleBackground = true;
        [SerializeField] private bool _visualSoundCues = false;
        
        [Header("Gameplay")]
        [SerializeField] private bool _reducedMotion = false;
        [SerializeField] private bool _autoAim = false;
        [SerializeField] private float _autoAimStrength = 0.5f;
        [SerializeField] private bool _holdToPress = false;
        [SerializeField] private float _holdDuration = 0.5f;
        
        [Header("Controls")]
        [SerializeField] private bool _oneHandedMode = false;
        [SerializeField] private bool _swapControls = false;
        [SerializeField] private float _touchSensitivity = 1f;
        [SerializeField] private float _doubleTapSpeed = 0.3f;
        
        [Header("Screen Reader")]
        [SerializeField] private bool _screenReaderSupport = false;
        #endregion

        #region Properties
        public float TextScale => _textScale;
        public bool BoldText => _boldText;
        public bool HighContrastUI => _highContrastUI;
        public ColorblindMode ColorblindSetting => _colorblindMode;
        public bool SubtitlesEnabled => _subtitlesEnabled;
        public float SubtitleScale => _subtitleScale;
        public bool ReducedMotion => _reducedMotion;
        public bool AutoAim => _autoAim;
        public float AutoAimStrength => _autoAimStrength;
        public bool HoldToPress => _holdToPress;
        public bool OneHandedMode => _oneHandedMode;
        public bool SwapControls => _swapControls;
        public float TouchSensitivity => _touchSensitivity;
        public bool ScreenReaderSupport => _screenReaderSupport;
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
            
            LoadSettings();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Text & UI
        /// <summary>
        /// Set text scale multiplier.
        /// </summary>
        public void SetTextScale(float scale)
        {
            _textScale = Mathf.Clamp(scale, _minTextScale, _maxTextScale);
            OnTextScaleChanged?.Invoke(_textScale);
            SaveSettings();
        }

        /// <summary>
        /// Toggle bold text.
        /// </summary>
        public void SetBoldText(bool enabled)
        {
            _boldText = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle high contrast UI.
        /// </summary>
        public void SetHighContrastUI(bool enabled)
        {
            _highContrastUI = enabled;
            ApplyHighContrast();
            SaveSettings();
        }
        #endregion

        #region Colorblind
        /// <summary>
        /// Set colorblind mode.
        /// </summary>
        public void SetColorblindMode(ColorblindMode mode)
        {
            _colorblindMode = mode;
            ApplyColorblindFilter();
            OnColorblindModeChanged?.Invoke(mode);
            SaveSettings();
        }

        private void ApplyColorblindFilter()
        {
            // Apply post-processing filter based on mode
            if (_colorblindMaterial != null)
            {
                switch (_colorblindMode)
                {
                    case ColorblindMode.None:
                        _colorblindMaterial.SetFloat("_Mode", 0);
                        break;
                    case ColorblindMode.Deuteranopia:
                        _colorblindMaterial.SetFloat("_Mode", 1);
                        break;
                    case ColorblindMode.Protanopia:
                        _colorblindMaterial.SetFloat("_Mode", 2);
                        break;
                    case ColorblindMode.Tritanopia:
                        _colorblindMaterial.SetFloat("_Mode", 3);
                        break;
                }
            }
        }
        #endregion

        #region Audio & Subtitles
        /// <summary>
        /// Toggle subtitles.
        /// </summary>
        public void SetSubtitles(bool enabled)
        {
            _subtitlesEnabled = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set subtitle scale.
        /// </summary>
        public void SetSubtitleScale(float scale)
        {
            _subtitleScale = Mathf.Clamp(scale, 0.75f, 1.5f);
            SaveSettings();
        }

        /// <summary>
        /// Toggle visual sound cues.
        /// </summary>
        public void SetVisualSoundCues(bool enabled)
        {
            _visualSoundCues = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Show a subtitle.
        /// </summary>
        public void ShowSubtitle(string text, float duration = 3f)
        {
            if (!_subtitlesEnabled) return;
            
            // Display subtitle UI
            Debug.Log($"[Subtitle] {text}");
        }
        #endregion

        #region Gameplay Assistance
        /// <summary>
        /// Toggle reduced motion.
        /// </summary>
        public void SetReducedMotion(bool enabled)
        {
            _reducedMotion = enabled;
            
            // Reduce camera shake, screen effects, etc.
            
            SaveSettings();
        }

        /// <summary>
        /// Toggle auto-aim.
        /// </summary>
        public void SetAutoAim(bool enabled)
        {
            _autoAim = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Set auto-aim strength.
        /// </summary>
        public void SetAutoAimStrength(float strength)
        {
            _autoAimStrength = Mathf.Clamp01(strength);
            SaveSettings();
        }

        /// <summary>
        /// Toggle hold-to-press mode.
        /// </summary>
        public void SetHoldToPress(bool enabled)
        {
            _holdToPress = enabled;
            SaveSettings();
        }
        #endregion

        #region Controls
        /// <summary>
        /// Toggle one-handed mode.
        /// </summary>
        public void SetOneHandedMode(bool enabled)
        {
            _oneHandedMode = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle swap controls (left/right handed).
        /// </summary>
        public void SetSwapControls(bool enabled)
        {
            _swapControls = enabled;
            SaveSettings();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set touch sensitivity.
        /// </summary>
        public void SetTouchSensitivity(float sensitivity)
        {
            _touchSensitivity = Mathf.Clamp(sensitivity, 0.5f, 2f);
            SaveSettings();
        }
        #endregion

        #region Screen Reader
        /// <summary>
        /// Toggle screen reader support.
        /// </summary>
        public void SetScreenReaderSupport(bool enabled)
        {
            _screenReaderSupport = enabled;
            SaveSettings();
        }

        /// <summary>
        /// Announce text for screen readers.
        /// </summary>
        public void Announce(string text)
        {
            if (!_screenReaderSupport) return;
            
            // Use platform-specific TTS
            // #if UNITY_IOS
            // AccessibilityManager.Announce(text);
            // #endif
            
            Debug.Log($"[ScreenReader] {text}");
        }
        #endregion

        #region Apply Settings
        private void ApplySettings()
        {
            ApplyColorblindFilter();
            ApplyHighContrast();
            ApplyReducedMotion();
            OnSettingsChanged?.Invoke();
        }

        private void ApplyHighContrast()
        {
            // Apply high contrast theme to UI
        }

        private void ApplyReducedMotion()
        {
            // Disable camera shake, reduce particle effects, etc.
            if (_reducedMotion)
            {
                // CameraShake.Disable();
                // ParticleReduction.Enable();
            }
        }
        #endregion

        #region Save/Load
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("Access_TextScale", _textScale);
            PlayerPrefs.SetInt("Access_BoldText", _boldText ? 1 : 0);
            PlayerPrefs.SetInt("Access_HighContrast", _highContrastUI ? 1 : 0);
            PlayerPrefs.SetInt("Access_ColorblindMode", (int)_colorblindMode);
            PlayerPrefs.SetInt("Access_Subtitles", _subtitlesEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("Access_SubtitleScale", _subtitleScale);
            PlayerPrefs.SetInt("Access_VisualCues", _visualSoundCues ? 1 : 0);
            PlayerPrefs.SetInt("Access_ReducedMotion", _reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt("Access_AutoAim", _autoAim ? 1 : 0);
            PlayerPrefs.SetFloat("Access_AutoAimStrength", _autoAimStrength);
            PlayerPrefs.SetInt("Access_HoldToPress", _holdToPress ? 1 : 0);
            PlayerPrefs.SetInt("Access_OneHanded", _oneHandedMode ? 1 : 0);
            PlayerPrefs.SetInt("Access_SwapControls", _swapControls ? 1 : 0);
            PlayerPrefs.SetFloat("Access_TouchSens", _touchSensitivity);
            PlayerPrefs.SetInt("Access_ScreenReader", _screenReaderSupport ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            _textScale = PlayerPrefs.GetFloat("Access_TextScale", 1f);
            _boldText = PlayerPrefs.GetInt("Access_BoldText", 0) == 1;
            _highContrastUI = PlayerPrefs.GetInt("Access_HighContrast", 0) == 1;
            _colorblindMode = (ColorblindMode)PlayerPrefs.GetInt("Access_ColorblindMode", 0);
            _subtitlesEnabled = PlayerPrefs.GetInt("Access_Subtitles", 1) == 1;
            _subtitleScale = PlayerPrefs.GetFloat("Access_SubtitleScale", 1f);
            _visualSoundCues = PlayerPrefs.GetInt("Access_VisualCues", 0) == 1;
            _reducedMotion = PlayerPrefs.GetInt("Access_ReducedMotion", 0) == 1;
            _autoAim = PlayerPrefs.GetInt("Access_AutoAim", 0) == 1;
            _autoAimStrength = PlayerPrefs.GetFloat("Access_AutoAimStrength", 0.5f);
            _holdToPress = PlayerPrefs.GetInt("Access_HoldToPress", 0) == 1;
            _oneHandedMode = PlayerPrefs.GetInt("Access_OneHanded", 0) == 1;
            _swapControls = PlayerPrefs.GetInt("Access_SwapControls", 0) == 1;
            _touchSensitivity = PlayerPrefs.GetFloat("Access_TouchSens", 1f);
            _screenReaderSupport = PlayerPrefs.GetInt("Access_ScreenReader", 0) == 1;
        }
        #endregion

        #region Presets
        /// <summary>
        /// Apply accessibility preset.
        /// </summary>
        public void ApplyPreset(AccessibilityPreset preset)
        {
            switch (preset)
            {
                case AccessibilityPreset.Default:
                    ResetToDefaults();
                    break;
                    
                case AccessibilityPreset.LowVision:
                    _textScale = 1.25f;
                    _boldText = true;
                    _highContrastUI = true;
                    _subtitlesEnabled = true;
                    _subtitleScale = 1.25f;
                    break;
                    
                case AccessibilityPreset.Colorblind:
                    _colorblindMode = ColorblindMode.Deuteranopia;
                    _visualSoundCues = true;
                    break;
                    
                case AccessibilityPreset.MotorImpaired:
                    _autoAim = true;
                    _autoAimStrength = 0.75f;
                    _holdToPress = true;
                    _reducedMotion = true;
                    break;
                    
                case AccessibilityPreset.HearingImpaired:
                    _subtitlesEnabled = true;
                    _subtitleScale = 1.25f;
                    _subtitleBackground = true;
                    _visualSoundCues = true;
                    break;
            }
            
            ApplySettings();
            SaveSettings();
        }

        private void ResetToDefaults()
        {
            _textScale = 1f;
            _boldText = false;
            _highContrastUI = false;
            _colorblindMode = ColorblindMode.None;
            _subtitlesEnabled = true;
            _subtitleScale = 1f;
            _visualSoundCues = false;
            _reducedMotion = false;
            _autoAim = false;
            _holdToPress = false;
            _oneHandedMode = false;
            _swapControls = false;
            _touchSensitivity = 1f;
        }
        #endregion
    }

    #region Enums
    public enum ColorblindMode
    {
        None,
        Deuteranopia,   // Red-green (most common)
        Protanopia,     // Red-green
        Tritanopia      // Blue-yellow
    }

    public enum AccessibilityPreset
    {
        Default,
        LowVision,
        Colorblind,
        MotorImpaired,
        HearingImpaired
    }
    #endregion
}

