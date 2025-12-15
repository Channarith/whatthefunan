using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WhatTheFunan.Core;

namespace WhatTheFunan.Combat
{
    /// <summary>
    /// Paired Animation Combat Mode - Cinematic QTE-based combat.
    /// Timing-based button prompts for attacks and counters.
    /// Best for all ages.
    /// </summary>
    public class PairedAnimationCombat : MonoBehaviour, ICombatModeHandler
    {
        #region Events
        public static event System.Action<QTEPrompt> OnQTEPromptShown;
        public static event System.Action<bool> OnQTEResult;
        public static event System.Action OnQTESequenceComplete;
        #endregion

        #region QTE Types
        public enum QTEType
        {
            Tap,            // Single tap
            DoubleTap,      // Quick double tap
            Hold,           // Hold for duration
            Swipe,          // Swipe direction
            Mash            // Rapid tapping
        }

        [System.Serializable]
        public class QTEPrompt
        {
            public QTEType type;
            public float duration;
            public Vector2 swipeDirection; // For swipe type
            public int requiredTaps;       // For mash type
        }
        #endregion

        #region Settings
        [Header("QTE Settings")]
        [SerializeField] private float _qteSuccessWindow = 0.5f;
        [SerializeField] private float _qteFailPenalty = 0.5f;
        [SerializeField] private float _timeBetweenPrompts = 0.3f;
        
        [Header("Combat Flow")]
        [SerializeField] private int _promptsPerSequence = 3;
        [SerializeField] private float _sequenceDamageMultiplier = 1.5f;
        
        [Header("Visual")]
        [SerializeField] private float _cinematicSlowMo = 0.5f;
        #endregion

        #region State
        private bool _isActive;
        private bool _inSequence;
        private bool _waitingForInput;
        private QTEPrompt _currentPrompt;
        private float _promptStartTime;
        private int _successfulPrompts;
        private int _totalPrompts;
        private List<EnemyBase> _enemies = new List<EnemyBase>();
        private EnemyBase _currentTarget;
        
        // For mash type
        private int _currentTaps;
        
        // Animation
        private Animator _playerAnimator;
        #endregion

        #region Animation Hashes
        private static readonly int AnimPairedAttack = Animator.StringToHash("PairedAttack");
        private static readonly int AnimPairedSuccess = Animator.StringToHash("PairedSuccess");
        private static readonly int AnimPairedFail = Animator.StringToHash("PairedFail");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            var player = FindObjectOfType<Characters.PlayerController>();
            if (player != null)
            {
                _playerAnimator = player.GetComponentInChildren<Animator>();
            }
        }
        #endregion

        #region ICombatModeHandler Implementation
        public void Activate()
        {
            _isActive = true;
            Debug.Log("[PairedAnimationCombat] Activated");
        }

        public void Deactivate()
        {
            _isActive = false;
            _inSequence = false;
            _waitingForInput = false;
            Time.timeScale = 1f;
            Debug.Log("[PairedAnimationCombat] Deactivated");
        }

        public void Update()
        {
            if (!_isActive) return;
            
            // Check QTE timeout
            if (_waitingForInput && _currentPrompt != null)
            {
                float elapsed = Time.unscaledTime - _promptStartTime;
                if (elapsed > _currentPrompt.duration)
                {
                    OnQTEFailed();
                }
            }
            
            // Clean up dead enemies
            _enemies.RemoveAll(e => e == null || !e.IsAlive);
            
            // Check for victory
            if (_enemies.Count == 0 && CombatController.Instance.IsInCombat)
            {
                CombatController.Instance.EndCombat(true);
            }
        }

        public void StartCombat(EnemyBase[] enemies)
        {
            _enemies = new List<EnemyBase>(enemies);
            _successfulPrompts = 0;
            _totalPrompts = 0;
            
            // Select initial target
            if (_enemies.Count > 0)
            {
                _currentTarget = _enemies[0];
                CombatController.Instance.SetTarget(_currentTarget);
            }
            
            Debug.Log($"[PairedAnimationCombat] Combat started with {enemies.Length} enemies");
        }

        public void EndCombat(bool victory)
        {
            _enemies.Clear();
            _currentTarget = null;
            _inSequence = false;
            _waitingForInput = false;
            Time.timeScale = 1f;
            
            Debug.Log($"[PairedAnimationCombat] Combat ended. Victory: {victory}");
        }

        public void OnAttackInput()
        {
            if (_inSequence)
            {
                // Handle QTE input
                ProcessQTEInput(QTEType.Tap);
            }
            else
            {
                // Start attack sequence
                StartAttackSequence();
            }
        }

        public void OnDodgeInput()
        {
            if (_waitingForInput && _currentPrompt?.type == QTEType.Swipe)
            {
                // Process as swipe input
                ProcessQTEInput(QTEType.Swipe);
            }
        }

        public void OnCounterInput()
        {
            if (_waitingForInput)
            {
                ProcessQTEInput(QTEType.Tap);
            }
        }

        public void OnSpecialInput(int abilityIndex)
        {
            // TODO: Implement special abilities
            Debug.Log($"[PairedAnimationCombat] Special ability {abilityIndex} requested");
        }

        public void SwitchTarget()
        {
            if (_enemies.Count <= 1) return;
            
            int currentIndex = _enemies.IndexOf(_currentTarget);
            int nextIndex = (currentIndex + 1) % _enemies.Count;
            
            _currentTarget = _enemies[nextIndex];
            CombatController.Instance.SetTarget(_currentTarget);
            
            Debug.Log($"[PairedAnimationCombat] Target switched to: {_currentTarget.name}");
        }
        #endregion

        #region Attack Sequence
        private void StartAttackSequence()
        {
            if (_currentTarget == null)
            {
                // Auto-select target
                foreach (var enemy in _enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        _currentTarget = enemy;
                        break;
                    }
                }
                
                if (_currentTarget == null) return;
            }

            StartCoroutine(AttackSequenceCoroutine());
        }

        private IEnumerator AttackSequenceCoroutine()
        {
            _inSequence = true;
            _successfulPrompts = 0;
            _totalPrompts = 0;
            
            // Play attack initiation animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger(AnimPairedAttack);
            }
            
            // Cinematic slow-mo
            Time.timeScale = _cinematicSlowMo;
            
            // Generate and run QTE sequence
            for (int i = 0; i < _promptsPerSequence; i++)
            {
                _totalPrompts++;
                
                // Generate random prompt type
                QTEPrompt prompt = GenerateRandomPrompt();
                _currentPrompt = prompt;
                
                // Show prompt to player
                OnQTEPromptShown?.Invoke(prompt);
                
                // Wait for input
                _waitingForInput = true;
                _promptStartTime = Time.unscaledTime;
                _currentTaps = 0;
                
                // Wait until input received or timeout
                while (_waitingForInput)
                {
                    yield return null;
                }
                
                yield return new WaitForSecondsRealtime(_timeBetweenPrompts);
            }
            
            // Calculate results
            float successRate = (float)_successfulPrompts / _totalPrompts;
            float baseDamage = 20f;
            float finalDamage = baseDamage * (1f + successRate * _sequenceDamageMultiplier);
            
            // Apply damage
            if (_currentTarget != null && _currentTarget.IsAlive)
            {
                bool isCritical = successRate >= 1f; // Perfect = critical
                CombatController.Instance.RegisterHit(_currentTarget, finalDamage, isCritical);
            }
            
            // End sequence
            Time.timeScale = 1f;
            _inSequence = false;
            
            OnQTESequenceComplete?.Invoke();
            
            // Switch target if current is dead
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                SwitchTarget();
            }
        }

        private QTEPrompt GenerateRandomPrompt()
        {
            QTEPrompt prompt = new QTEPrompt();
            
            // Randomly select QTE type (weighted towards simpler types for kids)
            float roll = Random.value;
            if (roll < 0.5f)
            {
                prompt.type = QTEType.Tap;
                prompt.duration = _qteSuccessWindow;
            }
            else if (roll < 0.75f)
            {
                prompt.type = QTEType.DoubleTap;
                prompt.duration = _qteSuccessWindow * 1.5f;
            }
            else if (roll < 0.9f)
            {
                prompt.type = QTEType.Hold;
                prompt.duration = 0.5f;
            }
            else
            {
                prompt.type = QTEType.Mash;
                prompt.duration = 1f;
                prompt.requiredTaps = 5;
            }
            
            return prompt;
        }
        #endregion

        #region QTE Processing
        private void ProcessQTEInput(QTEType inputType)
        {
            if (!_waitingForInput || _currentPrompt == null) return;
            
            bool success = false;
            
            switch (_currentPrompt.type)
            {
                case QTEType.Tap:
                    success = (inputType == QTEType.Tap);
                    if (success) CompletePrompt(true);
                    break;
                    
                case QTEType.DoubleTap:
                    if (inputType == QTEType.Tap)
                    {
                        _currentTaps++;
                        if (_currentTaps >= 2)
                        {
                            CompletePrompt(true);
                        }
                    }
                    break;
                    
                case QTEType.Hold:
                    // Hold is handled differently - check if still holding
                    success = true;
                    break;
                    
                case QTEType.Mash:
                    if (inputType == QTEType.Tap)
                    {
                        _currentTaps++;
                        if (_currentTaps >= _currentPrompt.requiredTaps)
                        {
                            CompletePrompt(true);
                        }
                    }
                    break;
                    
                case QTEType.Swipe:
                    success = (inputType == QTEType.Swipe);
                    if (success) CompletePrompt(true);
                    break;
            }
        }

        private void CompletePrompt(bool success)
        {
            _waitingForInput = false;
            
            if (success)
            {
                _successfulPrompts++;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
                
                if (_playerAnimator != null)
                {
                    _playerAnimator.SetTrigger(AnimPairedSuccess);
                }
            }
            else
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Warning);
                
                if (_playerAnimator != null)
                {
                    _playerAnimator.SetTrigger(AnimPairedFail);
                }
            }
            
            OnQTEResult?.Invoke(success);
        }

        private void OnQTEFailed()
        {
            CompletePrompt(false);
        }
        #endregion
    }
}

