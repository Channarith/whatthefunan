using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using WhatTheFunan.Core;
using WhatTheFunan.Economy;

namespace WhatTheFunan.MiniGames
{
    /// <summary>
    /// Fishing mini-game set in the Mekong-style rivers of Funan.
    /// Cast line, wait for bite, reel in with timing mechanics.
    /// </summary>
    public class FishingGame : MonoBehaviour
    {
        #region Events
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;
        public static event Action OnLineCast;
        public static event Action OnFishBite;
        public static event Action<Fish> OnFishCaught;
        public static event Action OnFishEscaped;
        public static event Action<float> OnTensionChanged;
        #endregion

        #region Game State
        public enum FishingState
        {
            Idle,
            Casting,
            Waiting,
            Hooked,
            Reeling,
            Caught,
            Escaped
        }

        [SerializeField] private FishingState _state = FishingState.Idle;
        public FishingState State => _state;
        public bool IsPlaying => _state != FishingState.Idle;
        #endregion

        #region Fish Database
        [Header("Fish Database")]
        [SerializeField] private List<Fish> _fishDatabase = new List<Fish>();
        
        private Fish _currentFish;
        public Fish CurrentFish => _currentFish;
        #endregion

        #region Settings
        [Header("Casting")]
        [SerializeField] private float _minCastPower = 0.3f;
        [SerializeField] private float _maxCastPower = 1f;
        [SerializeField] private float _castChargeSpeed = 1f;
        
        [Header("Waiting")]
        [SerializeField] private float _minWaitTime = 2f;
        [SerializeField] private float _maxWaitTime = 10f;
        [SerializeField] private float _biteWindow = 1f;
        
        [Header("Reeling")]
        [SerializeField] private float _reelingSpeed = 0.5f;
        [SerializeField] private float _tensionDecayRate = 0.3f;
        [SerializeField] private float _tensionBuildRate = 0.5f;
        [SerializeField] private float _maxTension = 1f;
        [SerializeField] private float _snapThreshold = 0.9f;
        
        [Header("Rewards")]
        [SerializeField] private int _baseCoinsPerFish = 10;
        #endregion

        #region Runtime State
        private float _castPower;
        private float _currentTension;
        private float _fishProgress; // 0 = hooked, 1 = caught
        private float _waitTimer;
        private bool _fishOnLine;
        private Coroutine _gameCoroutine;
        
        // Caught fish this session
        private List<Fish> _caughtFish = new List<Fish>();
        public IReadOnlyList<Fish> CaughtFish => _caughtFish;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!IsPlaying) return;
            
            switch (_state)
            {
                case FishingState.Reeling:
                    UpdateReeling();
                    break;
            }
        }
        #endregion

        #region Game Flow
        /// <summary>
        /// Start a fishing session.
        /// </summary>
        public void StartFishing()
        {
            if (IsPlaying) return;
            
            _caughtFish.Clear();
            _state = FishingState.Idle;
            
            GameManager.Instance?.EnterMiniGame();
            OnGameStarted?.Invoke();
            
            Debug.Log("[FishingGame] Fishing session started");
        }

        /// <summary>
        /// End the fishing session.
        /// </summary>
        public void EndFishing()
        {
            if (_gameCoroutine != null)
            {
                StopCoroutine(_gameCoroutine);
                _gameCoroutine = null;
            }
            
            _state = FishingState.Idle;
            _currentFish = null;
            
            // Grant rewards
            GrantRewards();
            
            GameManager.Instance?.ExitMiniGame();
            OnGameEnded?.Invoke();
            
            Debug.Log($"[FishingGame] Session ended. Caught {_caughtFish.Count} fish");
        }

        /// <summary>
        /// Start charging the cast.
        /// </summary>
        public void StartCast()
        {
            if (_state != FishingState.Idle) return;
            
            _state = FishingState.Casting;
            _castPower = _minCastPower;
            
            _gameCoroutine = StartCoroutine(CastChargeRoutine());
        }

        /// <summary>
        /// Release the cast.
        /// </summary>
        public void ReleaseCast()
        {
            if (_state != FishingState.Casting) return;
            
            if (_gameCoroutine != null)
            {
                StopCoroutine(_gameCoroutine);
            }
            
            OnLineCast?.Invoke();
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            
            _gameCoroutine = StartCoroutine(WaitForBiteRoutine());
        }

        /// <summary>
        /// Hook the fish when it bites.
        /// </summary>
        public void HookFish()
        {
            if (_state != FishingState.Hooked) return;
            
            _state = FishingState.Reeling;
            _fishProgress = 0f;
            _currentTension = 0.3f;
            
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Heavy);
            
            Debug.Log($"[FishingGame] Hooked: {_currentFish.fishName}");
        }

        /// <summary>
        /// Reel input (called while player holds reel button).
        /// </summary>
        public void Reel()
        {
            if (_state != FishingState.Reeling) return;
            
            // Increase tension while reeling
            _currentTension += _tensionBuildRate * Time.deltaTime;
            _currentTension = Mathf.Clamp(_currentTension, 0, _maxTension);
            
            // Progress if tension is in safe zone
            if (_currentTension < _snapThreshold)
            {
                float speedModifier = 1f - (_currentFish.difficulty * 0.5f);
                _fishProgress += _reelingSpeed * speedModifier * Time.deltaTime;
            }
            
            OnTensionChanged?.Invoke(_currentTension / _maxTension);
            
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
        }

        /// <summary>
        /// Stop reeling (called when player releases reel button).
        /// </summary>
        public void StopReeling()
        {
            // Tension decays when not reeling
        }
        #endregion

        #region Game Routines
        private IEnumerator CastChargeRoutine()
        {
            while (_state == FishingState.Casting)
            {
                _castPower += _castChargeSpeed * Time.deltaTime;
                
                // Oscillate between min and max
                if (_castPower > _maxCastPower)
                {
                    _castPower = _maxCastPower - (_castPower - _maxCastPower);
                }
                else if (_castPower < _minCastPower)
                {
                    _castPower = _minCastPower + (_minCastPower - _castPower);
                }
                
                yield return null;
            }
        }

        private IEnumerator WaitForBiteRoutine()
        {
            _state = FishingState.Waiting;
            
            // Select fish based on cast power
            _currentFish = SelectRandomFish(_castPower);
            
            // Calculate wait time based on fish rarity
            float waitTime = UnityEngine.Random.Range(_minWaitTime, _maxWaitTime);
            waitTime *= (1f + _currentFish.difficulty * 0.5f);
            
            _waitTimer = waitTime;
            
            while (_waitTimer > 0)
            {
                _waitTimer -= Time.deltaTime;
                yield return null;
            }
            
            // Fish bites!
            _state = FishingState.Hooked;
            _fishOnLine = true;
            OnFishBite?.Invoke();
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Heavy);
            
            // Player must hook within window
            float biteTimer = _biteWindow;
            while (biteTimer > 0 && _state == FishingState.Hooked)
            {
                biteTimer -= Time.deltaTime;
                yield return null;
            }
            
            // Missed the hook
            if (_state == FishingState.Hooked)
            {
                FishEscaped("Missed the bite!");
            }
        }

        private void UpdateReeling()
        {
            // Decay tension when not actively reeling
            _currentTension -= _tensionDecayRate * Time.deltaTime;
            _currentTension = Mathf.Clamp(_currentTension, 0, _maxTension);
            
            // Fish fights back
            float fightStrength = _currentFish.difficulty * UnityEngine.Random.Range(0.3f, 0.7f);
            _fishProgress -= fightStrength * Time.deltaTime * 0.1f;
            _fishProgress = Mathf.Clamp01(_fishProgress);
            
            OnTensionChanged?.Invoke(_currentTension / _maxTension);
            
            // Check for snap
            if (_currentTension >= _maxTension)
            {
                FishEscaped("Line snapped!");
            }
            // Check for catch
            else if (_fishProgress >= 1f)
            {
                CatchFish();
            }
        }

        private void CatchFish()
        {
            _state = FishingState.Caught;
            _caughtFish.Add(_currentFish);
            
            OnFishCaught?.Invoke(_currentFish);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            
            Debug.Log($"[FishingGame] Caught: {_currentFish.fishName}!");
            
            // Reset for next cast
            StartCoroutine(ResetAfterDelay(2f));
        }

        private void FishEscaped(string reason)
        {
            _state = FishingState.Escaped;
            _fishOnLine = false;
            
            OnFishEscaped?.Invoke();
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Error);
            
            Debug.Log($"[FishingGame] Fish escaped: {reason}");
            
            // Reset for next cast
            StartCoroutine(ResetAfterDelay(1.5f));
        }

        private IEnumerator ResetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _state = FishingState.Idle;
            _currentFish = null;
            _fishProgress = 0;
            _currentTension = 0;
        }
        #endregion

        #region Fish Selection
        private Fish SelectRandomFish(float castPower)
        {
            // Better cast power = better chance at rare fish
            List<Fish> eligibleFish = new List<Fish>();
            
            foreach (var fish in _fishDatabase)
            {
                float rarityThreshold = 1f - fish.rarity * 0.2f;
                if (castPower >= rarityThreshold * 0.5f)
                {
                    eligibleFish.Add(fish);
                }
            }
            
            if (eligibleFish.Count == 0)
            {
                eligibleFish.Add(_fishDatabase[0]); // Default fish
            }
            
            // Weight by inverse rarity
            float totalWeight = 0;
            foreach (var fish in eligibleFish)
            {
                totalWeight += 1f / (fish.rarity + 1);
            }
            
            float roll = UnityEngine.Random.Range(0, totalWeight);
            float currentWeight = 0;
            
            foreach (var fish in eligibleFish)
            {
                currentWeight += 1f / (fish.rarity + 1);
                if (roll <= currentWeight)
                {
                    return fish;
                }
            }
            
            return eligibleFish[0];
        }
        #endregion

        #region Rewards
        private void GrantRewards()
        {
            int totalCoins = 0;
            
            foreach (var fish in _caughtFish)
            {
                int fishValue = Mathf.RoundToInt(_baseCoinsPerFish * (1 + fish.rarity * 0.5f));
                totalCoins += fishValue;
                
                // Add to fish log collection
                // CollectionManager.Instance?.UnlockFish(fish.fishId);
            }
            
            if (totalCoins > 0)
            {
                CurrencyManager.Instance?.AddCoins(totalCoins);
            }
        }
        #endregion
    }

    #region Fish Data
    [Serializable]
    public class Fish
    {
        public string fishId;
        public string fishName;
        [TextArea] public string description;
        public Sprite icon;
        public Sprite model;
        
        [Range(0, 4)] public int rarity; // 0 = common, 4 = legendary
        [Range(0.1f, 1f)] public float difficulty;
        
        public int baseValue;
        public bool isSpecial;
    }
    #endregion
}

