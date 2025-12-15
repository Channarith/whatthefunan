using UnityEngine;
using System;
using System.Collections;

namespace WhatTheFunan.Monetization
{
    /// <summary>
    /// Manages ad display using Unity Ads and AdMob.
    /// Supports rewarded ads and interstitial ads.
    /// COPPA compliant - child-friendly ad networks only.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        #region Singleton
        private static AdManager _instance;
        public static AdManager Instance => _instance;
        #endregion

        #region Events
        public static event Action OnRewardedAdLoaded;
        public static event Action OnRewardedAdStarted;
        public static event Action<int> OnRewardedAdCompleted; // gems earned
        public static event Action OnRewardedAdFailed;
        public static event Action OnInterstitialShown;
        public static event Action OnAdFreePurchased;
        #endregion

        #region Settings
        [Header("Ad Unit IDs")]
        [SerializeField] private string _rewardedAdUnitId = "rewardedVideo";
        [SerializeField] private string _interstitialAdUnitId = "video";
        
        [Header("Rewards")]
        [SerializeField] private int _rewardedAdGems = 5;
        
        [Header("Interstitial Settings")]
        [SerializeField] private float _minTimeBetweenInterstitials = 600f; // 10 minutes
        [SerializeField] private int _actionsBeforeInterstitial = 5;
        
        [Header("Testing")]
        [SerializeField] private bool _testMode = true;
        #endregion

        #region State
        private bool _isInitialized;
        private bool _rewardedAdReady;
        private bool _interstitialReady;
        private float _lastInterstitialTime;
        private int _actionsSinceLastInterstitial;
        private bool _isAdFree;
        
        public bool IsInitialized => _isInitialized;
        public bool RewardedAdReady => _rewardedAdReady && !_isAdFree;
        public bool IsAdFree => _isAdFree;
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
            
            LoadAdFreeStatus();
        }

        private void Start()
        {
            InitializeAds();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Initialization
        private void InitializeAds()
        {
            if (_isAdFree)
            {
                Debug.Log("[AdManager] Ad-free mode active, skipping initialization");
                return;
            }
            
            // TODO: Initialize Unity Ads
            // Advertisement.Initialize(_gameId, _testMode, this);
            
            // TODO: Initialize AdMob for mediation
            // MobileAds.Initialize(initStatus => { });
            
            _isInitialized = true;
            
            // Pre-load ads
            LoadRewardedAd();
            LoadInterstitial();
            
            Debug.Log("[AdManager] Ads initialized (mock mode)");
        }
        #endregion

        #region Rewarded Ads
        /// <summary>
        /// Load a rewarded ad.
        /// </summary>
        public void LoadRewardedAd()
        {
            if (_isAdFree) return;
            
            // TODO: Load Unity Ads rewarded video
            // Advertisement.Load(_rewardedAdUnitId, this);
            
            // Simulate ad loading
            StartCoroutine(SimulateAdLoad(true));
        }

        /// <summary>
        /// Show a rewarded ad.
        /// </summary>
        public void ShowRewardedAd()
        {
            if (_isAdFree)
            {
                Debug.Log("[AdManager] Ad-free mode, granting reward directly");
                GrantReward();
                return;
            }
            
            if (!_rewardedAdReady)
            {
                Debug.LogWarning("[AdManager] Rewarded ad not ready");
                OnRewardedAdFailed?.Invoke();
                return;
            }
            
            OnRewardedAdStarted?.Invoke();
            
            if (_testMode)
            {
                // Simulate watching ad
                StartCoroutine(SimulateRewardedAd());
            }
            else
            {
                // TODO: Show Unity Ads
                // Advertisement.Show(_rewardedAdUnitId, this);
            }
        }

        private IEnumerator SimulateRewardedAd()
        {
            Debug.Log("[AdManager] TEST MODE: Simulating rewarded ad...");
            yield return new WaitForSeconds(2f);
            OnRewardedAdComplete();
        }

        private void OnRewardedAdComplete()
        {
            _rewardedAdReady = false;
            GrantReward();
            
            // Reload for next time
            LoadRewardedAd();
        }

        private void GrantReward()
        {
            Economy.CurrencyManager.Instance?.AddGems(_rewardedAdGems);
            OnRewardedAdCompleted?.Invoke(_rewardedAdGems);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[AdManager] Granted {_rewardedAdGems} gems for watching ad");
        }
        #endregion

        #region Interstitial Ads
        /// <summary>
        /// Load an interstitial ad.
        /// </summary>
        public void LoadInterstitial()
        {
            if (_isAdFree) return;
            
            // TODO: Load interstitial
            // Advertisement.Load(_interstitialAdUnitId, this);
            
            // Simulate loading
            StartCoroutine(SimulateAdLoad(false));
        }

        /// <summary>
        /// Show an interstitial ad if conditions are met.
        /// </summary>
        public bool TryShowInterstitial()
        {
            if (_isAdFree) return false;
            
            _actionsSinceLastInterstitial++;
            
            // Check conditions
            if (_actionsSinceLastInterstitial < _actionsBeforeInterstitial)
            {
                return false;
            }
            
            if (Time.time - _lastInterstitialTime < _minTimeBetweenInterstitials)
            {
                return false;
            }
            
            if (!_interstitialReady)
            {
                return false;
            }
            
            ShowInterstitial();
            return true;
        }

        private void ShowInterstitial()
        {
            _actionsSinceLastInterstitial = 0;
            _lastInterstitialTime = Time.time;
            _interstitialReady = false;
            
            OnInterstitialShown?.Invoke();
            
            if (_testMode)
            {
                Debug.Log("[AdManager] TEST MODE: Interstitial would show here");
            }
            else
            {
                // TODO: Show Unity Ads interstitial
                // Advertisement.Show(_interstitialAdUnitId, this);
            }
            
            // Reload for next time
            LoadInterstitial();
        }

        /// <summary>
        /// Record an action that counts toward interstitial display.
        /// Call this after level completion, returning to menu, etc.
        /// </summary>
        public void RecordAction()
        {
            _actionsSinceLastInterstitial++;
        }
        #endregion

        #region Ad-Free
        /// <summary>
        /// Activate ad-free mode (after purchase).
        /// </summary>
        public void ActivateAdFree()
        {
            _isAdFree = true;
            PlayerPrefs.SetInt("AdFree", 1);
            PlayerPrefs.Save();
            
            OnAdFreePurchased?.Invoke();
            Debug.Log("[AdManager] Ad-free mode activated");
        }

        private void LoadAdFreeStatus()
        {
            _isAdFree = PlayerPrefs.GetInt("AdFree", 0) == 1;
            
            // Also check subscription
            if (IAPManager.Instance?.HasActiveSubscription() == true)
            {
                _isAdFree = true;
            }
        }
        #endregion

        #region Simulation
        private IEnumerator SimulateAdLoad(bool isRewarded)
        {
            yield return new WaitForSeconds(1f);
            
            if (isRewarded)
            {
                _rewardedAdReady = true;
                OnRewardedAdLoaded?.Invoke();
                Debug.Log("[AdManager] Rewarded ad loaded");
            }
            else
            {
                _interstitialReady = true;
                Debug.Log("[AdManager] Interstitial ad loaded");
            }
        }
        #endregion

        #region Query
        /// <summary>
        /// Get the reward amount for watching an ad.
        /// </summary>
        public int GetRewardAmount()
        {
            return _rewardedAdGems;
        }

        /// <summary>
        /// Check if ads are available (not ad-free and initialized).
        /// </summary>
        public bool AreAdsAvailable()
        {
            return !_isAdFree && _isInitialized;
        }
        #endregion
    }
}

