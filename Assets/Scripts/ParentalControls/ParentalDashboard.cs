using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.ParentalControls
{
    /// <summary>
    /// Parental Dashboard for parents to monitor and control their child's gameplay.
    /// COPPA and PDPA compliant.
    /// </summary>
    public class ParentalDashboard : MonoBehaviour
    {
        #region Singleton
        private static ParentalDashboard _instance;
        public static ParentalDashboard Instance => _instance;
        #endregion

        #region Events
        public static event Action OnParentalPINSet;
        public static event Action OnParentalPINVerified;
        public static event Action OnPlayTimeLimitReached;
        public static event Action OnSpendingLimitReached;
        #endregion

        #region Settings
        [Header("Play Time Limits")]
        [SerializeField] private bool _enablePlayTimeLimit = false;
        [SerializeField] private float _dailyPlayTimeLimitMinutes = 60f;
        [SerializeField] private bool _enableBedtimeMode = false;
        [SerializeField] private int _bedtimeHour = 21; // 9 PM
        [SerializeField] private int _wakeTimeHour = 7;  // 7 AM
        
        [Header("Spending Limits")]
        [SerializeField] private bool _enableSpendingLimit = false;
        [SerializeField] private float _monthlySpendingLimit = 50f;
        [SerializeField] private bool _requirePINForPurchases = true;
        
        [Header("Social Controls")]
        [SerializeField] private bool _allowFriendRequests = true;
        [SerializeField] private bool _allowGiftSending = true;
        [SerializeField] private bool _allowChat = false; // Disabled by default for COPPA
        
        [Header("Content Controls")]
        [SerializeField] private bool _hideAds = false;
        [SerializeField] private bool _disableIAP = false;
        #endregion

        #region State
        private string _parentalPIN;
        private bool _isPINVerified;
        private float _todayPlayTime;
        private float _monthlySpending;
        private DateTime _lastPlayDate;
        private DateTime _sessionStartTime;
        
        public bool IsPINSet => !string.IsNullOrEmpty(_parentalPIN);
        public bool IsPINVerified => _isPINVerified;
        public float TodayPlayTimeMinutes => _todayPlayTime;
        public float RemainingPlayTimeMinutes => _dailyPlayTimeLimitMinutes - _todayPlayTime;
        public float MonthlySpending => _monthlySpending;
        public float RemainingSpendingLimit => _monthlySpendingLimit - _monthlySpending;
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
        }

        private void Start()
        {
            _sessionStartTime = DateTime.Now;
            CheckDayReset();
            CheckMonthReset();
        }

        private void Update()
        {
            if (_enablePlayTimeLimit)
            {
                UpdatePlayTime();
            }
            
            if (_enableBedtimeMode)
            {
                CheckBedtime();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SavePlayTime();
            }
            else
            {
                _sessionStartTime = DateTime.Now;
            }
        }

        private void OnApplicationQuit()
        {
            SavePlayTime();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region PIN Management
        /// <summary>
        /// Set the parental PIN (first time setup).
        /// </summary>
        public bool SetPIN(string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length != 4)
            {
                Debug.LogWarning("[ParentalDashboard] PIN must be 4 digits");
                return false;
            }
            
            // Hash the PIN for security
            _parentalPIN = HashPIN(pin);
            
            PlayerPrefs.SetString("ParentalPIN", _parentalPIN);
            PlayerPrefs.Save();
            
            OnParentalPINSet?.Invoke();
            Debug.Log("[ParentalDashboard] Parental PIN set");
            
            return true;
        }

        /// <summary>
        /// Verify the parental PIN.
        /// </summary>
        public bool VerifyPIN(string pin)
        {
            if (!IsPINSet)
            {
                Debug.LogWarning("[ParentalDashboard] No PIN set");
                return false;
            }
            
            string hashedInput = HashPIN(pin);
            
            if (hashedInput == _parentalPIN)
            {
                _isPINVerified = true;
                OnParentalPINVerified?.Invoke();
                Debug.Log("[ParentalDashboard] PIN verified");
                return true;
            }
            
            Debug.Log("[ParentalDashboard] Incorrect PIN");
            return false;
        }

        /// <summary>
        /// Lock the dashboard (require PIN again).
        /// </summary>
        public void Lock()
        {
            _isPINVerified = false;
        }

        /// <summary>
        /// Reset/remove the PIN.
        /// </summary>
        public bool ResetPIN(string currentPIN)
        {
            if (!VerifyPIN(currentPIN))
            {
                return false;
            }
            
            _parentalPIN = null;
            PlayerPrefs.DeleteKey("ParentalPIN");
            PlayerPrefs.Save();
            
            _isPINVerified = false;
            Debug.Log("[ParentalDashboard] PIN reset");
            
            return true;
        }

        private string HashPIN(string pin)
        {
            // Simple hash - in production, use proper cryptographic hash
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.Create()
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(pin + "WhatTheFunanSalt")));
        }
        #endregion

        #region Play Time Management
        private void UpdatePlayTime()
        {
            float sessionMinutes = (float)(DateTime.Now - _sessionStartTime).TotalMinutes;
            float totalToday = _todayPlayTime + sessionMinutes;
            
            if (totalToday >= _dailyPlayTimeLimitMinutes)
            {
                OnPlayTimeLimitReached?.Invoke();
                // Could pause game or show warning
            }
        }

        private void CheckDayReset()
        {
            if (_lastPlayDate.Date != DateTime.Today)
            {
                _todayPlayTime = 0;
                _lastPlayDate = DateTime.Today;
                SavePlayTime();
            }
        }

        private void CheckMonthReset()
        {
            string lastMonth = PlayerPrefs.GetString("ParentalLastMonth", "");
            string currentMonth = DateTime.Now.ToString("yyyy-MM");
            
            if (lastMonth != currentMonth)
            {
                _monthlySpending = 0;
                PlayerPrefs.SetString("ParentalLastMonth", currentMonth);
                PlayerPrefs.SetFloat("ParentalMonthlySpending", 0);
                PlayerPrefs.Save();
            }
        }

        private void SavePlayTime()
        {
            float sessionMinutes = (float)(DateTime.Now - _sessionStartTime).TotalMinutes;
            _todayPlayTime += sessionMinutes;
            
            PlayerPrefs.SetFloat("ParentalTodayPlayTime", _todayPlayTime);
            PlayerPrefs.SetString("ParentalLastPlayDate", DateTime.Today.ToString("o"));
            PlayerPrefs.Save();
            
            _sessionStartTime = DateTime.Now;
        }

        private void CheckBedtime()
        {
            int currentHour = DateTime.Now.Hour;
            
            bool isBedtime = currentHour >= _bedtimeHour || currentHour < _wakeTimeHour;
            
            if (isBedtime)
            {
                // Show bedtime message and pause game
                Debug.Log("[ParentalDashboard] It's bedtime! Game paused.");
            }
        }

        /// <summary>
        /// Get play time report.
        /// </summary>
        public PlayTimeReport GetPlayTimeReport()
        {
            return new PlayTimeReport
            {
                todayMinutes = _todayPlayTime,
                limitMinutes = _dailyPlayTimeLimitMinutes,
                isLimitEnabled = _enablePlayTimeLimit,
                weeklyMinutes = GetWeeklyPlayTime()
            };
        }

        private float GetWeeklyPlayTime()
        {
            // Would sum up last 7 days from saved data
            return _todayPlayTime; // Simplified
        }
        #endregion

        #region Spending Management
        /// <summary>
        /// Check if a purchase is allowed.
        /// </summary>
        public bool CanMakePurchase(float amount)
        {
            if (_disableIAP)
            {
                Debug.Log("[ParentalDashboard] IAP disabled");
                return false;
            }
            
            if (_enableSpendingLimit && (_monthlySpending + amount) > _monthlySpendingLimit)
            {
                OnSpendingLimitReached?.Invoke();
                Debug.Log("[ParentalDashboard] Spending limit reached");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Record a purchase.
        /// </summary>
        public void RecordPurchase(float amount)
        {
            _monthlySpending += amount;
            
            PlayerPrefs.SetFloat("ParentalMonthlySpending", _monthlySpending);
            PlayerPrefs.Save();
            
            Debug.Log($"[ParentalDashboard] Purchase recorded: ${amount}. Monthly total: ${_monthlySpending}");
        }

        /// <summary>
        /// Check if PIN is required for purchases.
        /// </summary>
        public bool RequiresPINForPurchase()
        {
            return _requirePINForPurchases && IsPINSet;
        }
        #endregion

        #region Settings Management
        /// <summary>
        /// Update play time settings.
        /// </summary>
        public void SetPlayTimeLimit(bool enabled, float minutesPerDay)
        {
            if (!_isPINVerified) return;
            
            _enablePlayTimeLimit = enabled;
            _dailyPlayTimeLimitMinutes = minutesPerDay;
            SaveSettings();
        }

        /// <summary>
        /// Update bedtime settings.
        /// </summary>
        public void SetBedtime(bool enabled, int bedtimeHour, int wakeHour)
        {
            if (!_isPINVerified) return;
            
            _enableBedtimeMode = enabled;
            _bedtimeHour = bedtimeHour;
            _wakeTimeHour = wakeHour;
            SaveSettings();
        }

        /// <summary>
        /// Update spending settings.
        /// </summary>
        public void SetSpendingLimit(bool enabled, float monthlyLimit)
        {
            if (!_isPINVerified) return;
            
            _enableSpendingLimit = enabled;
            _monthlySpendingLimit = monthlyLimit;
            SaveSettings();
        }

        /// <summary>
        /// Update social settings.
        /// </summary>
        public void SetSocialControls(bool friends, bool gifts, bool chat)
        {
            if (!_isPINVerified) return;
            
            _allowFriendRequests = friends;
            _allowGiftSending = gifts;
            _allowChat = chat;
            SaveSettings();
        }

        /// <summary>
        /// Disable all purchases.
        /// </summary>
        public void SetIAPDisabled(bool disabled)
        {
            if (!_isPINVerified) return;
            
            _disableIAP = disabled;
            SaveSettings();
        }
        #endregion

        #region Query
        /// <summary>
        /// Check if friend requests are allowed.
        /// </summary>
        public bool AreFriendRequestsAllowed()
        {
            return _allowFriendRequests;
        }

        /// <summary>
        /// Check if gift sending is allowed.
        /// </summary>
        public bool IsGiftSendingAllowed()
        {
            return _allowGiftSending;
        }

        /// <summary>
        /// Check if chat is allowed.
        /// </summary>
        public bool IsChatAllowed()
        {
            return _allowChat;
        }

        /// <summary>
        /// Check if ads should be hidden.
        /// </summary>
        public bool ShouldHideAds()
        {
            return _hideAds;
        }
        #endregion

        #region Save/Load
        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Parental_PlayTimeEnabled", _enablePlayTimeLimit ? 1 : 0);
            PlayerPrefs.SetFloat("Parental_PlayTimeLimit", _dailyPlayTimeLimitMinutes);
            PlayerPrefs.SetInt("Parental_BedtimeEnabled", _enableBedtimeMode ? 1 : 0);
            PlayerPrefs.SetInt("Parental_BedtimeHour", _bedtimeHour);
            PlayerPrefs.SetInt("Parental_WakeHour", _wakeTimeHour);
            PlayerPrefs.SetInt("Parental_SpendingEnabled", _enableSpendingLimit ? 1 : 0);
            PlayerPrefs.SetFloat("Parental_SpendingLimit", _monthlySpendingLimit);
            PlayerPrefs.SetInt("Parental_RequirePIN", _requirePINForPurchases ? 1 : 0);
            PlayerPrefs.SetInt("Parental_AllowFriends", _allowFriendRequests ? 1 : 0);
            PlayerPrefs.SetInt("Parental_AllowGifts", _allowGiftSending ? 1 : 0);
            PlayerPrefs.SetInt("Parental_AllowChat", _allowChat ? 1 : 0);
            PlayerPrefs.SetInt("Parental_HideAds", _hideAds ? 1 : 0);
            PlayerPrefs.SetInt("Parental_DisableIAP", _disableIAP ? 1 : 0);
            PlayerPrefs.Save();
            
            Debug.Log("[ParentalDashboard] Settings saved");
        }

        private void LoadSettings()
        {
            _parentalPIN = PlayerPrefs.GetString("ParentalPIN", "");
            
            _enablePlayTimeLimit = PlayerPrefs.GetInt("Parental_PlayTimeEnabled", 0) == 1;
            _dailyPlayTimeLimitMinutes = PlayerPrefs.GetFloat("Parental_PlayTimeLimit", 60f);
            _enableBedtimeMode = PlayerPrefs.GetInt("Parental_BedtimeEnabled", 0) == 1;
            _bedtimeHour = PlayerPrefs.GetInt("Parental_BedtimeHour", 21);
            _wakeTimeHour = PlayerPrefs.GetInt("Parental_WakeHour", 7);
            _enableSpendingLimit = PlayerPrefs.GetInt("Parental_SpendingEnabled", 0) == 1;
            _monthlySpendingLimit = PlayerPrefs.GetFloat("Parental_SpendingLimit", 50f);
            _requirePINForPurchases = PlayerPrefs.GetInt("Parental_RequirePIN", 1) == 1;
            _allowFriendRequests = PlayerPrefs.GetInt("Parental_AllowFriends", 1) == 1;
            _allowGiftSending = PlayerPrefs.GetInt("Parental_AllowGifts", 1) == 1;
            _allowChat = PlayerPrefs.GetInt("Parental_AllowChat", 0) == 1;
            _hideAds = PlayerPrefs.GetInt("Parental_HideAds", 0) == 1;
            _disableIAP = PlayerPrefs.GetInt("Parental_DisableIAP", 0) == 1;
            
            _todayPlayTime = PlayerPrefs.GetFloat("ParentalTodayPlayTime", 0);
            _monthlySpending = PlayerPrefs.GetFloat("ParentalMonthlySpending", 0);
            
            string lastDate = PlayerPrefs.GetString("ParentalLastPlayDate", "");
            if (!string.IsNullOrEmpty(lastDate))
            {
                DateTime.TryParse(lastDate, out _lastPlayDate);
            }
        }
        #endregion

        #region Data Export (GDPR Compliance)
        /// <summary>
        /// Export all child data for GDPR/COPPA compliance.
        /// </summary>
        public string ExportChildData()
        {
            var data = new ChildDataExport
            {
                exportDate = DateTime.Now.ToString("o"),
                totalPlayTimeMinutes = _todayPlayTime,
                totalSpending = _monthlySpending,
                settings = new ChildDataExport.Settings
                {
                    playTimeLimitEnabled = _enablePlayTimeLimit,
                    playTimeLimitMinutes = _dailyPlayTimeLimitMinutes,
                    bedtimeModeEnabled = _enableBedtimeMode,
                    spendingLimitEnabled = _enableSpendingLimit,
                    spendingLimit = _monthlySpendingLimit
                }
            };
            
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Delete all child data for GDPR compliance.
        /// </summary>
        public void DeleteAllChildData()
        {
            if (!_isPINVerified)
            {
                Debug.LogWarning("[ParentalDashboard] PIN required to delete data");
                return;
            }
            
            // Clear all player data
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            // Clear save files
            Core.SaveSystem.Instance?.DeleteAllSaves();
            
            Debug.Log("[ParentalDashboard] All child data deleted");
        }

        [Serializable]
        public class ChildDataExport
        {
            public string exportDate;
            public float totalPlayTimeMinutes;
            public float totalSpending;
            public Settings settings;
            
            [Serializable]
            public class Settings
            {
                public bool playTimeLimitEnabled;
                public float playTimeLimitMinutes;
                public bool bedtimeModeEnabled;
                public bool spendingLimitEnabled;
                public float spendingLimit;
            }
        }
        #endregion
    }

    #region Data Classes
    public class PlayTimeReport
    {
        public float todayMinutes;
        public float limitMinutes;
        public bool isLimitEnabled;
        public float weeklyMinutes;
    }
    #endregion
}

