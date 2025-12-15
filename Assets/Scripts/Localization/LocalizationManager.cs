using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Localization
{
    /// <summary>
    /// Manages game localization for multiple languages.
    /// Supports English, Khmer, Thai, Lao, and more.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        #region Singleton
        private static LocalizationManager _instance;
        public static LocalizationManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<SystemLanguage> OnLanguageChanged;
        #endregion

        #region Languages
        [Header("Supported Languages")]
        [SerializeField] private List<LanguageData> _supportedLanguages = new List<LanguageData>();
        
        [Header("Localization Data")]
        [SerializeField] private List<LocalizationTable> _tables = new List<LocalizationTable>();
        
        private Dictionary<string, LocalizationTable> _tableLookup = new Dictionary<string, LocalizationTable>();
        private Dictionary<string, Dictionary<string, string>> _strings = new Dictionary<string, Dictionary<string, string>>();
        
        private SystemLanguage _currentLanguage = SystemLanguage.English;
        public SystemLanguage CurrentLanguage => _currentLanguage;
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
            
            InitializeTables();
            LoadLanguagePreference();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeTables()
        {
            _tableLookup.Clear();
            foreach (var table in _tables)
            {
                _tableLookup[table.tableId] = table;
            }
        }
        #endregion

        #region Language Selection
        /// <summary>
        /// Set the current language.
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            if (!IsLanguageSupported(language))
            {
                Debug.LogWarning($"[LocalizationManager] Language not supported: {language}");
                language = SystemLanguage.English;
            }
            
            _currentLanguage = language;
            SaveLanguagePreference();
            
            // Reload all localized strings
            LoadStringsForLanguage(language);
            
            OnLanguageChanged?.Invoke(language);
            Debug.Log($"[LocalizationManager] Language set to: {language}");
        }

        /// <summary>
        /// Set language by code (e.g., "en", "km", "th").
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            SystemLanguage language = GetLanguageFromCode(languageCode);
            SetLanguage(language);
        }

        /// <summary>
        /// Check if a language is supported.
        /// </summary>
        public bool IsLanguageSupported(SystemLanguage language)
        {
            foreach (var lang in _supportedLanguages)
            {
                if (lang.language == language)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get list of supported languages.
        /// </summary>
        public List<LanguageData> GetSupportedLanguages()
        {
            return new List<LanguageData>(_supportedLanguages);
        }

        private SystemLanguage GetLanguageFromCode(string code)
        {
            switch (code.ToLower())
            {
                case "en": return SystemLanguage.English;
                case "km": return SystemLanguage.Unknown; // Khmer - use custom
                case "th": return SystemLanguage.Thai;
                case "lo": return SystemLanguage.Unknown; // Lao - use custom
                case "zh": return SystemLanguage.Chinese;
                case "ja": return SystemLanguage.Japanese;
                case "ko": return SystemLanguage.Korean;
                case "vi": return SystemLanguage.Vietnamese;
                case "fr": return SystemLanguage.French;
                case "de": return SystemLanguage.German;
                case "es": return SystemLanguage.Spanish;
                case "pt": return SystemLanguage.Portuguese;
                default: return SystemLanguage.English;
            }
        }

        private void LoadStringsForLanguage(SystemLanguage language)
        {
            _strings.Clear();
            
            string langCode = GetLanguageCode(language);
            
            foreach (var table in _tables)
            {
                var langDict = new Dictionary<string, string>();
                
                foreach (var entry in table.entries)
                {
                    string value = GetLocalizedValue(entry, langCode);
                    langDict[entry.key] = value;
                }
                
                _strings[table.tableId] = langDict;
            }
        }

        private string GetLocalizedValue(LocalizationEntry entry, string langCode)
        {
            foreach (var translation in entry.translations)
            {
                if (translation.languageCode == langCode)
                {
                    return translation.value;
                }
            }
            
            // Fallback to English
            foreach (var translation in entry.translations)
            {
                if (translation.languageCode == "en")
                {
                    return translation.value;
                }
            }
            
            return entry.key; // Return key as fallback
        }
        #endregion

        #region Get Strings
        /// <summary>
        /// Get a localized string by key.
        /// </summary>
        public string Get(string key, string tableId = "general")
        {
            if (_strings.TryGetValue(tableId, out var table))
            {
                if (table.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
            
            Debug.LogWarning($"[LocalizationManager] Key not found: {tableId}/{key}");
            return $"[{key}]";
        }

        /// <summary>
        /// Get a localized string with format parameters.
        /// </summary>
        public string GetFormat(string key, string tableId = "general", params object[] args)
        {
            string format = Get(key, tableId);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        /// <summary>
        /// Shorthand for Get().
        /// </summary>
        public static string L(string key, string tableId = "general")
        {
            return Instance?.Get(key, tableId) ?? $"[{key}]";
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Get language code for a SystemLanguage.
        /// </summary>
        public string GetLanguageCode(SystemLanguage language)
        {
            foreach (var lang in _supportedLanguages)
            {
                if (lang.language == language)
                {
                    return lang.code;
                }
            }
            return "en";
        }

        /// <summary>
        /// Get display name for current language (in that language).
        /// </summary>
        public string GetCurrentLanguageName()
        {
            foreach (var lang in _supportedLanguages)
            {
                if (lang.language == _currentLanguage)
                {
                    return lang.nativeName;
                }
            }
            return "English";
        }

        /// <summary>
        /// Detect device language.
        /// </summary>
        public SystemLanguage DetectDeviceLanguage()
        {
            SystemLanguage deviceLang = Application.systemLanguage;
            
            if (IsLanguageSupported(deviceLang))
            {
                return deviceLang;
            }
            
            return SystemLanguage.English;
        }
        #endregion

        #region Save/Load
        private void SaveLanguagePreference()
        {
            PlayerPrefs.SetString("Language", GetLanguageCode(_currentLanguage));
            PlayerPrefs.Save();
        }

        private void LoadLanguagePreference()
        {
            string savedCode = PlayerPrefs.GetString("Language", "");
            
            if (!string.IsNullOrEmpty(savedCode))
            {
                SetLanguage(savedCode);
            }
            else
            {
                // Use device language
                SetLanguage(DetectDeviceLanguage());
            }
        }
        #endregion
    }

    #region Localization Data Classes
    [Serializable]
    public class LanguageData
    {
        public SystemLanguage language;
        public string code;         // e.g., "en", "km", "th"
        public string englishName;  // e.g., "Khmer"
        public string nativeName;   // e.g., "ភាសាខ្មែរ"
        public Sprite flag;
        public bool isRTL;          // Right-to-left
        public Font customFont;     // For Khmer, Thai, etc.
    }

    [Serializable]
    public class LocalizationTable
    {
        public string tableId;      // e.g., "general", "ui", "quests", "items"
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }

    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public List<Translation> translations = new List<Translation>();
    }

    [Serializable]
    public class Translation
    {
        public string languageCode;
        [TextArea] public string value;
    }
    #endregion

    #region Sample Strings
    /*
    GENERAL TABLE:
    - "welcome" -> "Welcome to Funan!" / "សូមស្វាគមន៍មកកាន់ហ្វូណាន!" / "ยินดีต้อนรับสู่ฟูนัน!"
    - "play" -> "Play" / "លេង" / "เล่น"
    - "settings" -> "Settings" / "ការកំណត់" / "การตั้งค่า"
    - "exit" -> "Exit" / "ចាកចេញ" / "ออก"
    
    UI TABLE:
    - "coins" -> "Coins" / "កាក់" / "เหรียญ"
    - "gems" -> "Gems" / "គ្រាប់ពេជ្រ" / "อัญมณี"
    - "level" -> "Level" / "កម្រិត" / "ระดับ"
    
    QUESTS TABLE:
    - "quest_intro_1" -> "Welcome to the Kingdom of Funan..."
    - "quest_objective_defeat" -> "Defeat {0} enemies"
    - "quest_objective_collect" -> "Collect {0} items"
    
    NOTE: Khmer and Thai fonts require special font assets.
    */
    #endregion
}

