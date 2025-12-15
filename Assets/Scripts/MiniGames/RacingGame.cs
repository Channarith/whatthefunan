using UnityEngine;
using System;
using System.Collections.Generic;
using WhatTheFunan.Core;

namespace WhatTheFunan.MiniGames
{
    /// <summary>
    /// Mount racing mini-game featuring elephant racing and other Funan mounts.
    /// Simple endless runner / racing with obstacles and power-ups.
    /// </summary>
    public class RacingGame : MonoBehaviour
    {
        #region Events
        public static event Action OnRaceStarted;
        public static event Action<RaceResult> OnRaceEnded;
        public static event Action<int> OnLapCompleted;
        public static event Action<PowerUp> OnPowerUpCollected;
        public static event Action OnObstacleHit;
        #endregion

        #region State
        public enum RacingState
        {
            Idle,
            Countdown,
            Racing,
            Finished
        }

        [SerializeField] private RacingState _state = RacingState.Idle;
        public RacingState State => _state;
        public bool IsRacing => _state == RacingState.Racing;
        #endregion

        #region Track Data
        [Header("Tracks")]
        [SerializeField] private List<RaceTrack> _tracks = new List<RaceTrack>();
        
        private RaceTrack _currentTrack;
        public RaceTrack CurrentTrack => _currentTrack;
        #endregion

        #region Mount Data
        [Header("Mounts")]
        [SerializeField] private List<RaceMount> _mounts = new List<RaceMount>();
        
        private RaceMount _currentMount;
        public RaceMount CurrentMount => _currentMount;
        #endregion

        #region Race State
        private int _currentLap;
        private float _raceTime;
        private float _currentSpeed;
        private int _position;
        private List<float> _lapTimes = new List<float>();
        private float _lapStartTime;
        private float _boostRemaining;
        
        public int CurrentLap => _currentLap;
        public float RaceTime => _raceTime;
        public float CurrentSpeed => _currentSpeed;
        public int Position => _position;
        #endregion

        #region Settings
        [Header("Racing Settings")]
        [SerializeField] private float _baseSpeed = 10f;
        [SerializeField] private float _maxSpeed = 20f;
        [SerializeField] private float _acceleration = 5f;
        [SerializeField] private float _deceleration = 3f;
        [SerializeField] private float _boostMultiplier = 1.5f;
        [SerializeField] private float _obstacleSlowdown = 0.5f;
        [SerializeField] private int _totalLaps = 3;
        
        [Header("Controls")]
        [SerializeField] private float _laneWidth = 3f;
        [SerializeField] private float _laneSwitchSpeed = 10f;
        [SerializeField] private int _laneCount = 3;
        
        private int _currentLane;
        private float _targetLaneX;
        #endregion

        #region AI Racers
        [Header("AI")]
        [SerializeField] private int _aiRacerCount = 3;
        private List<AIRacer> _aiRacers = new List<AIRacer>();
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!IsRacing) return;
            
            _raceTime += Time.deltaTime;
            UpdateMovement();
            UpdateBoost();
            UpdateAIRacers();
            UpdatePosition();
        }
        #endregion

        #region Game Flow
        /// <summary>
        /// Start a race.
        /// </summary>
        public void StartRace(string trackId, string mountId)
        {
            _currentTrack = _tracks.Find(t => t.trackId == trackId);
            _currentMount = _mounts.Find(m => m.mountId == mountId);
            
            if (_currentTrack == null || _currentMount == null)
            {
                Debug.LogError("[RacingGame] Invalid track or mount");
                return;
            }
            
            InitializeRace();
            StartCoroutine(CountdownRoutine());
        }

        private void InitializeRace()
        {
            _currentLap = 0;
            _raceTime = 0;
            _currentSpeed = 0;
            _position = _aiRacerCount + 1;
            _lapTimes.Clear();
            _boostRemaining = 0;
            _currentLane = _laneCount / 2;
            _targetLaneX = 0;
            
            // Initialize AI racers
            _aiRacers.Clear();
            for (int i = 0; i < _aiRacerCount; i++)
            {
                _aiRacers.Add(new AIRacer
                {
                    name = $"Racer {i + 1}",
                    progress = 0,
                    speed = _baseSpeed * UnityEngine.Random.Range(0.8f, 1.2f)
                });
            }
            
            GameManager.Instance?.EnterMiniGame();
            _state = RacingState.Countdown;
        }

        private System.Collections.IEnumerator CountdownRoutine()
        {
            // 3... 2... 1... GO!
            for (int i = 3; i > 0; i--)
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
                yield return new WaitForSeconds(1f);
            }
            
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Heavy);
            _state = RacingState.Racing;
            _lapStartTime = _raceTime;
            
            OnRaceStarted?.Invoke();
            Debug.Log("[RacingGame] Race started!");
        }

        private void FinishRace()
        {
            _state = RacingState.Finished;
            
            var result = new RaceResult
            {
                trackId = _currentTrack.trackId,
                mountId = _currentMount.mountId,
                totalTime = _raceTime,
                lapTimes = new List<float>(_lapTimes),
                finalPosition = _position,
                totalRacers = _aiRacerCount + 1
            };
            
            OnRaceEnded?.Invoke(result);
            GrantRewards(result);
            
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            Debug.Log($"[RacingGame] Race finished! Position: {_position}, Time: {_raceTime:F2}s");
            
            GameManager.Instance?.ExitMiniGame();
        }
        #endregion

        #region Movement
        private void UpdateMovement()
        {
            // Accelerate to max speed
            float mountSpeedBonus = _currentMount.speedBonus;
            float targetSpeed = (_baseSpeed + mountSpeedBonus) * (_boostRemaining > 0 ? _boostMultiplier : 1f);
            targetSpeed = Mathf.Min(targetSpeed, _maxSpeed);
            
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
            
            // Move towards target lane
            // (In actual implementation, this would move a transform)
        }

        private void UpdateBoost()
        {
            if (_boostRemaining > 0)
            {
                _boostRemaining -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Switch to left lane.
        /// </summary>
        public void MoveLeft()
        {
            if (!IsRacing) return;
            if (_currentLane > 0)
            {
                _currentLane--;
                _targetLaneX = (_currentLane - _laneCount / 2) * _laneWidth;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }
        }

        /// <summary>
        /// Switch to right lane.
        /// </summary>
        public void MoveRight()
        {
            if (!IsRacing) return;
            if (_currentLane < _laneCount - 1)
            {
                _currentLane++;
                _targetLaneX = (_currentLane - _laneCount / 2) * _laneWidth;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }
        }

        /// <summary>
        /// Jump over obstacle.
        /// </summary>
        public void Jump()
        {
            if (!IsRacing) return;
            // Trigger jump animation
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
        }

        /// <summary>
        /// Use boost if available.
        /// </summary>
        public void UseBoost()
        {
            if (!IsRacing) return;
            if (_boostRemaining <= 0 && _currentMount.hasBoost)
            {
                _boostRemaining = 3f;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Heavy);
            }
        }
        #endregion

        #region Collision
        /// <summary>
        /// Called when hitting an obstacle.
        /// </summary>
        public void OnHitObstacle()
        {
            _currentSpeed *= _obstacleSlowdown;
            OnObstacleHit?.Invoke();
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Error);
        }

        /// <summary>
        /// Called when collecting a power-up.
        /// </summary>
        public void OnCollectPowerUp(PowerUp powerUp)
        {
            switch (powerUp.type)
            {
                case PowerUpType.Boost:
                    _boostRemaining += powerUp.duration;
                    break;
                case PowerUpType.Coin:
                    Economy.CurrencyManager.Instance?.AddCoins(powerUp.value);
                    break;
            }
            
            OnPowerUpCollected?.Invoke(powerUp);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
        }

        /// <summary>
        /// Called when crossing the finish line.
        /// </summary>
        public void OnCrossFinishLine()
        {
            _currentLap++;
            _lapTimes.Add(_raceTime - _lapStartTime);
            _lapStartTime = _raceTime;
            
            OnLapCompleted?.Invoke(_currentLap);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            
            if (_currentLap >= _totalLaps)
            {
                FinishRace();
            }
        }
        #endregion

        #region AI
        private void UpdateAIRacers()
        {
            foreach (var ai in _aiRacers)
            {
                // Vary AI speed slightly
                ai.speed += UnityEngine.Random.Range(-0.1f, 0.1f);
                ai.speed = Mathf.Clamp(ai.speed, _baseSpeed * 0.7f, _baseSpeed * 1.3f);
                ai.progress += ai.speed * Time.deltaTime;
            }
        }

        private void UpdatePosition()
        {
            float playerProgress = _raceTime * _currentSpeed; // Simplified
            
            int pos = 1;
            foreach (var ai in _aiRacers)
            {
                if (ai.progress > playerProgress)
                {
                    pos++;
                }
            }
            
            _position = pos;
        }
        #endregion

        #region Rewards
        private void GrantRewards(RaceResult result)
        {
            int baseReward = _currentTrack.baseReward;
            int positionBonus = (_aiRacerCount + 1 - result.finalPosition) * 10;
            
            int totalCoins = baseReward + positionBonus;
            Economy.CurrencyManager.Instance?.AddCoins(totalCoins);
            
            // Unlock mount cosmetics for 1st place
            if (result.finalPosition == 1)
            {
                // CollectionManager.Instance?.UnlockMountSkin(_currentMount.mountId);
            }
        }
        #endregion
    }

    #region Racing Data Classes
    [Serializable]
    public class RaceTrack
    {
        public string trackId;
        public string trackName;
        public Sprite preview;
        public float length;
        public int difficulty;
        public int baseReward;
    }

    [Serializable]
    public class RaceMount
    {
        public string mountId;
        public string mountName;
        public Sprite icon;
        public float speedBonus;
        public float acceleration;
        public bool hasBoost;
    }

    [Serializable]
    public class PowerUp
    {
        public PowerUpType type;
        public float duration;
        public int value;
    }

    public enum PowerUpType
    {
        Boost,
        Coin,
        Shield,
        Magnet
    }

    public class AIRacer
    {
        public string name;
        public float progress;
        public float speed;
    }

    public class RaceResult
    {
        public string trackId;
        public string mountId;
        public float totalTime;
        public List<float> lapTimes;
        public int finalPosition;
        public int totalRacers;
    }
    #endregion
}

