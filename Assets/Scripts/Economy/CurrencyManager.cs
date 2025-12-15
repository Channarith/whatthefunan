using UnityEngine;
using System;

namespace WhatTheFunan.Economy
{
    /// <summary>
    /// Manages the dual currency system: Funan Coins (soft) and Dragon Gems (hard).
    /// Handles earning, spending, and currency persistence.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        #region Singleton
        private static CurrencyManager _instance;
        public static CurrencyManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<int, int> OnCoinsChanged; // oldValue, newValue
        public static event Action<int, int> OnGemsChanged;   // oldValue, newValue
        public static event Action<int> OnCoinsEarned;
        public static event Action<int> OnGemsEarned;
        public static event Action<int> OnCoinsSpent;
        public static event Action<int> OnGemsSpent;
        public static event Action OnInsufficientFunds;
        #endregion

        #region Currency Types
        public enum CurrencyType
        {
            FunanCoins,  // Soft currency - earned through gameplay
            DragonGems   // Hard currency - earned through IAP, achievements, rare drops
        }
        #endregion

        #region Currency State
        [Header("Starting Currency")]
        [SerializeField] private int _startingCoins = 100;
        [SerializeField] private int _startingGems = 10;
        
        private int _funanCoins;
        private int _dragonGems;
        
        public int FunanCoins => _funanCoins;
        public int DragonGems => _dragonGems;
        #endregion

        #region Constants
        private const string COINS_KEY = "FunanCoins";
        private const string GEMS_KEY = "DragonGems";
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
            
            LoadCurrency();
        }

        private void OnDestroy()
        {
            SaveCurrency();
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Currency Operations
        /// <summary>
        /// Add currency to the player's wallet.
        /// </summary>
        public void AddCurrency(CurrencyType type, int amount)
        {
            if (amount <= 0) return;
            
            switch (type)
            {
                case CurrencyType.FunanCoins:
                    int oldCoins = _funanCoins;
                    _funanCoins += amount;
                    OnCoinsChanged?.Invoke(oldCoins, _funanCoins);
                    OnCoinsEarned?.Invoke(amount);
                    Debug.Log($"[CurrencyManager] Earned {amount} Funan Coins. Total: {_funanCoins}");
                    break;
                    
                case CurrencyType.DragonGems:
                    int oldGems = _dragonGems;
                    _dragonGems += amount;
                    OnGemsChanged?.Invoke(oldGems, _dragonGems);
                    OnGemsEarned?.Invoke(amount);
                    Debug.Log($"[CurrencyManager] Earned {amount} Dragon Gems. Total: {_dragonGems}");
                    break;
            }
            
            SaveCurrency();
        }

        /// <summary>
        /// Spend currency from the player's wallet.
        /// Returns true if successful, false if insufficient funds.
        /// </summary>
        public bool SpendCurrency(CurrencyType type, int amount)
        {
            if (amount <= 0) return true;
            
            if (!HasEnough(type, amount))
            {
                OnInsufficientFunds?.Invoke();
                Debug.LogWarning($"[CurrencyManager] Insufficient {type} to spend {amount}");
                return false;
            }
            
            switch (type)
            {
                case CurrencyType.FunanCoins:
                    int oldCoins = _funanCoins;
                    _funanCoins -= amount;
                    OnCoinsChanged?.Invoke(oldCoins, _funanCoins);
                    OnCoinsSpent?.Invoke(amount);
                    Debug.Log($"[CurrencyManager] Spent {amount} Funan Coins. Remaining: {_funanCoins}");
                    break;
                    
                case CurrencyType.DragonGems:
                    int oldGems = _dragonGems;
                    _dragonGems -= amount;
                    OnGemsChanged?.Invoke(oldGems, _dragonGems);
                    OnGemsSpent?.Invoke(amount);
                    Debug.Log($"[CurrencyManager] Spent {amount} Dragon Gems. Remaining: {_dragonGems}");
                    break;
            }
            
            SaveCurrency();
            return true;
        }

        /// <summary>
        /// Check if the player has enough currency.
        /// </summary>
        public bool HasEnough(CurrencyType type, int amount)
        {
            return type switch
            {
                CurrencyType.FunanCoins => _funanCoins >= amount,
                CurrencyType.DragonGems => _dragonGems >= amount,
                _ => false
            };
        }

        /// <summary>
        /// Get the current amount of a currency type.
        /// </summary>
        public int GetAmount(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.FunanCoins => _funanCoins,
                CurrencyType.DragonGems => _dragonGems,
                _ => 0
            };
        }
        #endregion

        #region Convenience Methods
        /// <summary>
        /// Add Funan Coins (soft currency).
        /// </summary>
        public void AddCoins(int amount) => AddCurrency(CurrencyType.FunanCoins, amount);

        /// <summary>
        /// Add Dragon Gems (hard currency).
        /// </summary>
        public void AddGems(int amount) => AddCurrency(CurrencyType.DragonGems, amount);

        /// <summary>
        /// Spend Funan Coins.
        /// </summary>
        public bool SpendCoins(int amount) => SpendCurrency(CurrencyType.FunanCoins, amount);

        /// <summary>
        /// Spend Dragon Gems.
        /// </summary>
        public bool SpendGems(int amount) => SpendCurrency(CurrencyType.DragonGems, amount);

        /// <summary>
        /// Check if player can afford coins.
        /// </summary>
        public bool CanAffordCoins(int amount) => HasEnough(CurrencyType.FunanCoins, amount);

        /// <summary>
        /// Check if player can afford gems.
        /// </summary>
        public bool CanAffordGems(int amount) => HasEnough(CurrencyType.DragonGems, amount);
        #endregion

        #region Persistence
        private void SaveCurrency()
        {
            PlayerPrefs.SetInt(COINS_KEY, _funanCoins);
            PlayerPrefs.SetInt(GEMS_KEY, _dragonGems);
            PlayerPrefs.Save();
        }

        private void LoadCurrency()
        {
            if (PlayerPrefs.HasKey(COINS_KEY))
            {
                _funanCoins = PlayerPrefs.GetInt(COINS_KEY);
                _dragonGems = PlayerPrefs.GetInt(GEMS_KEY);
            }
            else
            {
                // New player - give starting currency
                _funanCoins = _startingCoins;
                _dragonGems = _startingGems;
                SaveCurrency();
            }
            
            Debug.Log($"[CurrencyManager] Loaded: {_funanCoins} Coins, {_dragonGems} Gems");
        }

        /// <summary>
        /// Reset currency to starting values (for new game).
        /// </summary>
        public void ResetCurrency()
        {
            int oldCoins = _funanCoins;
            int oldGems = _dragonGems;
            
            _funanCoins = _startingCoins;
            _dragonGems = _startingGems;
            
            OnCoinsChanged?.Invoke(oldCoins, _funanCoins);
            OnGemsChanged?.Invoke(oldGems, _dragonGems);
            
            SaveCurrency();
            Debug.Log("[CurrencyManager] Currency reset to starting values");
        }
        #endregion

        #region Reward Helpers
        /// <summary>
        /// Give quest reward.
        /// </summary>
        public void GiveQuestReward(int coins, int gems = 0)
        {
            if (coins > 0) AddCoins(coins);
            if (gems > 0) AddGems(gems);
        }

        /// <summary>
        /// Give combat reward based on enemies defeated.
        /// </summary>
        public void GiveCombatReward(int enemyCount, int baseCoins = 10)
        {
            int reward = baseCoins * enemyCount;
            AddCoins(reward);
        }

        /// <summary>
        /// Give daily reward.
        /// </summary>
        public void GiveDailyReward(int day)
        {
            // Escalating daily rewards
            int coins = 50 + (day * 10);
            int gems = day >= 7 ? 5 : 0; // Gems on day 7
            
            AddCoins(coins);
            if (gems > 0) AddGems(gems);
        }

        /// <summary>
        /// Give ad reward (watched rewarded ad).
        /// </summary>
        public void GiveAdReward()
        {
            AddGems(5); // Standard rewarded ad gives 5 gems
        }
        #endregion

        #region Formatting
        /// <summary>
        /// Format coins for display (e.g., "1,234" or "1.2K").
        /// </summary>
        public string FormatCoins(bool shortFormat = false)
        {
            if (shortFormat && _funanCoins >= 1000)
            {
                if (_funanCoins >= 1000000)
                    return (_funanCoins / 1000000f).ToString("0.#") + "M";
                return (_funanCoins / 1000f).ToString("0.#") + "K";
            }
            return _funanCoins.ToString("N0");
        }

        /// <summary>
        /// Format gems for display.
        /// </summary>
        public string FormatGems(bool shortFormat = false)
        {
            if (shortFormat && _dragonGems >= 1000)
            {
                return (_dragonGems / 1000f).ToString("0.#") + "K";
            }
            return _dragonGems.ToString("N0");
        }
        #endregion
    }
}

