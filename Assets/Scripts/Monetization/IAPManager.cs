using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Monetization
{
    /// <summary>
    /// Manages In-App Purchases using Unity IAP.
    /// Supports consumables (gems), non-consumables (characters, DLC), and subscriptions.
    /// Includes parental gate for child protection.
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        #region Singleton
        private static IAPManager _instance;
        public static IAPManager Instance => _instance;
        #endregion

        #region Events
        public static event Action<IAPProduct> OnPurchaseStarted;
        public static event Action<IAPProduct> OnPurchaseComplete;
        public static event Action<IAPProduct, string> OnPurchaseFailed;
        public static event Action OnPurchasesRestored;
        public static event Action OnParentalGateRequired;
        #endregion

        #region Products
        [Header("Products")]
        [SerializeField] private List<IAPProduct> _products = new List<IAPProduct>();
        
        private Dictionary<string, IAPProduct> _productLookup = new Dictionary<string, IAPProduct>();
        public IReadOnlyList<IAPProduct> Products => _products;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private bool _requireParentalGate = true;
        [SerializeField] private bool _testMode = true; // Disable for production
        #endregion

        #region State
        private bool _isInitialized;
        private bool _isPurchasing;
        private IAPProduct _pendingProduct;
        
        public bool IsInitialized => _isInitialized;
        public bool IsPurchasing => _isPurchasing;
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
            
            InitializeProducts();
        }

        private void Start()
        {
            InitializeIAP();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeProducts()
        {
            _productLookup.Clear();
            foreach (var product in _products)
            {
                _productLookup[product.productId] = product;
            }
        }
        #endregion

        #region IAP Initialization
        private void InitializeIAP()
        {
            // TODO: Implement Unity IAP initialization
            // var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            // foreach (var product in _products) {
            //     builder.AddProduct(product.productId, product.productType);
            // }
            // UnityPurchasing.Initialize(this, builder);
            
            _isInitialized = true;
            Debug.Log("[IAPManager] IAP initialized (mock mode)");
        }
        #endregion

        #region Purchase Flow
        /// <summary>
        /// Initiate a purchase.
        /// </summary>
        public void Purchase(string productId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[IAPManager] IAP not initialized");
                return;
            }
            
            if (_isPurchasing)
            {
                Debug.LogWarning("[IAPManager] Purchase already in progress");
                return;
            }
            
            if (!_productLookup.TryGetValue(productId, out IAPProduct product))
            {
                Debug.LogError($"[IAPManager] Product not found: {productId}");
                return;
            }
            
            _pendingProduct = product;
            
            // Check parental gate
            if (_requireParentalGate)
            {
                OnParentalGateRequired?.Invoke();
                // Wait for parental gate result
            }
            else
            {
                ProcessPurchase(product);
            }
        }

        /// <summary>
        /// Called when parental gate is passed.
        /// </summary>
        public void OnParentalGatePassed()
        {
            if (_pendingProduct != null)
            {
                ProcessPurchase(_pendingProduct);
            }
        }

        /// <summary>
        /// Called when parental gate is failed/cancelled.
        /// </summary>
        public void OnParentalGateFailed()
        {
            if (_pendingProduct != null)
            {
                OnPurchaseFailed?.Invoke(_pendingProduct, "Parental gate not passed");
                _pendingProduct = null;
            }
        }

        private void ProcessPurchase(IAPProduct product)
        {
            _isPurchasing = true;
            OnPurchaseStarted?.Invoke(product);
            
            if (_testMode)
            {
                // Simulate successful purchase in test mode
                Debug.Log($"[IAPManager] TEST MODE: Simulating purchase of {product.productId}");
                SimulatePurchaseSuccess(product);
            }
            else
            {
                // TODO: Real Unity IAP purchase
                // _storeController.InitiatePurchase(product.productId);
            }
        }

        private void SimulatePurchaseSuccess(IAPProduct product)
        {
            // Simulate network delay
            StartCoroutine(SimulatePurchaseCoroutine(product));
        }

        private System.Collections.IEnumerator SimulatePurchaseCoroutine(IAPProduct product)
        {
            yield return new WaitForSeconds(1f);
            OnPurchaseSuccess(product);
        }

        private void OnPurchaseSuccess(IAPProduct product)
        {
            _isPurchasing = false;
            _pendingProduct = null;
            
            // Grant rewards
            GrantProductRewards(product);
            
            // Track analytics
            // AnalyticsManager.Instance?.TrackPurchase(product);
            
            OnPurchaseComplete?.Invoke(product);
            Core.HapticManager.Instance?.OnPurchaseComplete();
            
            Debug.Log($"[IAPManager] Purchase complete: {product.productId}");
        }

        private void OnPurchaseFailure(IAPProduct product, string error)
        {
            _isPurchasing = false;
            _pendingProduct = null;
            
            OnPurchaseFailed?.Invoke(product, error);
            
            Debug.LogWarning($"[IAPManager] Purchase failed: {product.productId} - {error}");
        }

        private void GrantProductRewards(IAPProduct product)
        {
            switch (product.type)
            {
                case IAPProductType.Consumable:
                    // Grant gems
                    if (product.gemAmount > 0)
                    {
                        Economy.CurrencyManager.Instance?.AddGems(product.gemAmount);
                    }
                    break;
                    
                case IAPProductType.NonConsumable:
                    // Unlock content
                    if (!string.IsNullOrEmpty(product.unlockId))
                    {
                        UnlockContent(product.unlockId);
                    }
                    break;
                    
                case IAPProductType.Subscription:
                    // Activate subscription
                    ActivateSubscription(product);
                    break;
            }
            
            // Bonus rewards
            if (product.bonusCoins > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(product.bonusCoins);
            }
        }

        private void UnlockContent(string unlockId)
        {
            // Parse unlock ID and unlock appropriate content
            // e.g., "character_neak", "chapter_3", "skin_elephant_gold"
            
            var unlocked = PlayerPrefs.GetString("UnlockedContent", "");
            if (!unlocked.Contains(unlockId))
            {
                unlocked += unlockId + ",";
                PlayerPrefs.SetString("UnlockedContent", unlocked);
                PlayerPrefs.Save();
            }
            
            Debug.Log($"[IAPManager] Unlocked content: {unlockId}");
        }

        private void ActivateSubscription(IAPProduct product)
        {
            // Set subscription active
            PlayerPrefs.SetString("ActiveSubscription", product.productId);
            PlayerPrefs.SetString("SubscriptionExpiry", DateTime.Now.AddDays(30).ToString());
            PlayerPrefs.Save();
            
            Debug.Log($"[IAPManager] Subscription activated: {product.productId}");
        }
        #endregion

        #region Restore Purchases
        /// <summary>
        /// Restore previous purchases (for iOS).
        /// </summary>
        public void RestorePurchases()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[IAPManager] IAP not initialized");
                return;
            }
            
            // TODO: Implement restore
            // #if UNITY_IOS
            // var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
            // apple.RestoreTransactions(OnRestoreComplete);
            // #endif
            
            Debug.Log("[IAPManager] Restoring purchases...");
            OnPurchasesRestored?.Invoke();
        }
        #endregion

        #region Query
        /// <summary>
        /// Get a product by ID.
        /// </summary>
        public IAPProduct GetProduct(string productId)
        {
            return _productLookup.GetValueOrDefault(productId, null);
        }

        /// <summary>
        /// Check if content is unlocked.
        /// </summary>
        public bool IsContentUnlocked(string unlockId)
        {
            var unlocked = PlayerPrefs.GetString("UnlockedContent", "");
            return unlocked.Contains(unlockId);
        }

        /// <summary>
        /// Check if player has active subscription.
        /// </summary>
        public bool HasActiveSubscription()
        {
            var expiry = PlayerPrefs.GetString("SubscriptionExpiry", "");
            if (string.IsNullOrEmpty(expiry)) return false;
            
            if (DateTime.TryParse(expiry, out DateTime expiryDate))
            {
                return DateTime.Now < expiryDate;
            }
            return false;
        }

        /// <summary>
        /// Get products by category.
        /// </summary>
        public List<IAPProduct> GetProductsByCategory(string category)
        {
            return _products.FindAll(p => p.category == category);
        }
        #endregion
    }

    #region IAP Data Classes
    public enum IAPProductType
    {
        Consumable,     // Gems, can buy multiple times
        NonConsumable,  // Characters, DLC, buy once
        Subscription    // Season pass, monthly
    }

    [Serializable]
    public class IAPProduct
    {
        [Header("Identity")]
        public string productId;        // e.g., "com.game.gems_100"
        public string displayName;
        public string description;
        public Sprite icon;
        public string category;         // "gems", "characters", "dlc", "subscription"
        
        [Header("Pricing")]
        public string priceString;      // "$0.99" (fetched from store)
        public float priceValue;        // For sorting
        
        [Header("Type")]
        public IAPProductType type;
        
        [Header("Rewards")]
        public int gemAmount;           // For consumables
        public int bonusCoins;          // Bonus with purchase
        public string unlockId;         // For non-consumables
        
        [Header("Display")]
        public bool isBestValue;
        public bool isMostPopular;
        public float bonusPercent;      // "20% bonus!"
    }
    #endregion
}

