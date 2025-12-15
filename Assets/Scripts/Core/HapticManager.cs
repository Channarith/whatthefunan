using UnityEngine;
using System;

namespace WhatTheFunan.Core
{
    /// <summary>
    /// Manages haptic feedback (vibration) for mobile devices.
    /// Provides different vibration patterns for various game events.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        #region Singleton
        private static HapticManager _instance;
        public static HapticManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HapticManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Haptic Types
        public enum HapticType
        {
            Light,          // UI touches, button presses
            Medium,         // Combat hits landed
            Heavy,          // Strong impacts, boss hits
            Success,        // Quest complete, level up
            Warning,        // Low health, danger
            Error,          // Failed action
            Selection,      // Menu selection
            Impact,         // Collision, landing
            Notification    // Alerts, rewards
        }

        public enum HapticIntensity
        {
            Off = 0,
            Low = 1,
            Medium = 2,
            High = 3
        }
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private HapticIntensity _intensity = HapticIntensity.Medium;
        [SerializeField] private bool _enableHaptics = true;

        public bool HapticsEnabled => _enableHaptics && _intensity != HapticIntensity.Off;
        public HapticIntensity Intensity => _intensity;
        #endregion

        #region Platform Detection
        private bool _isIOS;
        private bool _isAndroid;
        private bool _supportsHaptics;
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
            #if UNITY_IOS
            _isIOS = true;
            _supportsHaptics = UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.Unknown;
            #elif UNITY_ANDROID
            _isAndroid = true;
            _supportsHaptics = SystemInfo.supportsVibration;
            #else
            _supportsHaptics = false;
            #endif

            // Load saved settings
            _enableHaptics = PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
            _intensity = (HapticIntensity)PlayerPrefs.GetInt("HapticIntensity", (int)HapticIntensity.Medium);

            Debug.Log($"[HapticManager] Initialized. Supports haptics: {_supportsHaptics}");
        }
        #endregion

        #region Public API
        /// <summary>
        /// Trigger haptic feedback by type.
        /// </summary>
        public void TriggerHaptic(HapticType type)
        {
            if (!HapticsEnabled || !_supportsHaptics) return;

            switch (type)
            {
                case HapticType.Light:
                    TriggerLight();
                    break;
                case HapticType.Medium:
                    TriggerMedium();
                    break;
                case HapticType.Heavy:
                    TriggerHeavy();
                    break;
                case HapticType.Success:
                    TriggerSuccess();
                    break;
                case HapticType.Warning:
                    TriggerWarning();
                    break;
                case HapticType.Error:
                    TriggerError();
                    break;
                case HapticType.Selection:
                    TriggerSelection();
                    break;
                case HapticType.Impact:
                    TriggerImpact();
                    break;
                case HapticType.Notification:
                    TriggerNotification();
                    break;
            }
        }

        /// <summary>
        /// Enable or disable haptics.
        /// </summary>
        public void SetHapticsEnabled(bool enabled)
        {
            _enableHaptics = enabled;
            PlayerPrefs.SetInt("HapticsEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Set haptic intensity level.
        /// </summary>
        public void SetIntensity(HapticIntensity intensity)
        {
            _intensity = intensity;
            PlayerPrefs.SetInt("HapticIntensity", (int)intensity);
            PlayerPrefs.Save();
        }
        #endregion

        #region Haptic Implementations
        private void TriggerLight()
        {
            float duration = GetDurationForIntensity(10f);
            Vibrate((long)duration);
            
            #if UNITY_IOS && !UNITY_EDITOR
            // iOS Taptic Engine - Light impact
            iOSHapticFeedback.ImpactLight();
            #endif
        }

        private void TriggerMedium()
        {
            float duration = GetDurationForIntensity(25f);
            Vibrate((long)duration);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.ImpactMedium();
            #endif
        }

        private void TriggerHeavy()
        {
            float duration = GetDurationForIntensity(50f);
            Vibrate((long)duration);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.ImpactHeavy();
            #endif
        }

        private void TriggerSuccess()
        {
            // Double tap pattern
            Vibrate(20);
            StartCoroutine(DelayedVibration(100, 30));
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.NotificationSuccess();
            #endif
        }

        private void TriggerWarning()
        {
            Vibrate(40);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.NotificationWarning();
            #endif
        }

        private void TriggerError()
        {
            // Triple tap pattern
            Vibrate(15);
            StartCoroutine(DelayedVibration(80, 15));
            StartCoroutine(DelayedVibration(160, 15));
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.NotificationError();
            #endif
        }

        private void TriggerSelection()
        {
            float duration = GetDurationForIntensity(5f);
            Vibrate((long)duration);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.SelectionChanged();
            #endif
        }

        private void TriggerImpact()
        {
            float duration = GetDurationForIntensity(35f);
            Vibrate((long)duration);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.ImpactMedium();
            #endif
        }

        private void TriggerNotification()
        {
            Vibrate(30);
            
            #if UNITY_IOS && !UNITY_EDITOR
            iOSHapticFeedback.NotificationSuccess();
            #endif
        }
        #endregion

        #region Utility
        private float GetDurationForIntensity(float baseDuration)
        {
            switch (_intensity)
            {
                case HapticIntensity.Low:
                    return baseDuration * 0.5f;
                case HapticIntensity.Medium:
                    return baseDuration;
                case HapticIntensity.High:
                    return baseDuration * 1.5f;
                default:
                    return 0f;
            }
        }

        private void Vibrate(long milliseconds)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    // Check Android version for VibrationEffect
                    if (AndroidVersion >= 26)
                    {
                        using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                                "createOneShot", 
                                milliseconds, 
                                GetVibrationAmplitude()
                            );
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            #elif !UNITY_EDITOR
            Handheld.Vibrate();
            #endif
        }

        private System.Collections.IEnumerator DelayedVibration(int delayMs, int durationMs)
        {
            yield return new WaitForSeconds(delayMs / 1000f);
            Vibrate(durationMs);
        }

        private int GetVibrationAmplitude()
        {
            switch (_intensity)
            {
                case HapticIntensity.Low:
                    return 64;  // Light
                case HapticIntensity.Medium:
                    return 128; // Medium
                case HapticIntensity.High:
                    return 255; // Strong
                default:
                    return 0;
            }
        }

        private int AndroidVersion
        {
            get
            {
                #if UNITY_ANDROID && !UNITY_EDITOR
                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
                #else
                return 0;
                #endif
            }
        }
        #endregion

        #region Game Event Helpers
        // Convenience methods for common game events
        
        public void OnButtonPressed() => TriggerHaptic(HapticType.Selection);
        public void OnCombatHit() => TriggerHaptic(HapticType.Medium);
        public void OnCombatHitReceived() => TriggerHaptic(HapticType.Heavy);
        public void OnCriticalHit() => TriggerHaptic(HapticType.Heavy);
        public void OnLevelUp() => TriggerHaptic(HapticType.Success);
        public void OnQuestComplete() => TriggerHaptic(HapticType.Success);
        public void OnTreasureFound() => TriggerHaptic(HapticType.Notification);
        public void OnLowHealth() => TriggerHaptic(HapticType.Warning);
        public void OnBossPhaseChange() => TriggerHaptic(HapticType.Heavy);
        public void OnMiniGameSuccess() => TriggerHaptic(HapticType.Success);
        public void OnPurchaseComplete() => TriggerHaptic(HapticType.Success);
        #endregion
    }

    #region iOS Haptic Helper
    #if UNITY_IOS
    /// <summary>
    /// iOS-specific haptic feedback using Taptic Engine.
    /// </summary>
    public static class iOSHapticFeedback
    {
        public static void ImpactLight()
        {
            _ImpactOccurred(0);
        }

        public static void ImpactMedium()
        {
            _ImpactOccurred(1);
        }

        public static void ImpactHeavy()
        {
            _ImpactOccurred(2);
        }

        public static void NotificationSuccess()
        {
            _NotificationOccurred(0);
        }

        public static void NotificationWarning()
        {
            _NotificationOccurred(1);
        }

        public static void NotificationError()
        {
            _NotificationOccurred(2);
        }

        public static void SelectionChanged()
        {
            _SelectionChanged();
        }

        // These would be implemented via native iOS plugin
        // For now, they're stubs that would call into native code
        private static void _ImpactOccurred(int style) { }
        private static void _NotificationOccurred(int type) { }
        private static void _SelectionChanged() { }
    }
    #endif
    #endregion
}

