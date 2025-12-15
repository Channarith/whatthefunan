using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WhatTheFunan.Core;

namespace WhatTheFunan.Combat
{
    /// <summary>
    /// Automated Combat Mode - Auto-battle for younger players.
    /// Character fights automatically, player only selects targets and abilities.
    /// Best for younger kids (5-8).
    /// </summary>
    public class AutomatedCombat : MonoBehaviour, ICombatModeHandler
    {
        #region Events
        public static event System.Action<EnemyBase> OnAutoTargetSelected;
        public static event System.Action OnAutoAttackPerformed;
        public static event System.Action<int> OnAbilityUsed;
        #endregion

        #region Settings
        [Header("Auto-Combat Settings")]
        [SerializeField] private float _attackInterval = 1.5f;
        [SerializeField] private float _specialAbilityCooldown = 5f;
        [SerializeField] private bool _autoSelectTargets = true;
        [SerializeField] private bool _autoUseAbilities = true;
        
        [Header("AI Behavior")]
        [SerializeField] private float _targetSwitchDelay = 2f;
        [SerializeField] private float _dodgeChance = 0.3f;
        [SerializeField] private float _movementSpeed = 5f;
        #endregion

        #region State
        private bool _isActive;
        private bool _isFighting;
        private float _lastAttackTime;
        private float _lastAbilityTime;
        private List<EnemyBase> _enemies = new List<EnemyBase>();
        private EnemyBase _currentTarget;
        private int _selectedAbility = -1;
        
        // Animation
        private Animator _playerAnimator;
        private Transform _playerTransform;
        
        // Coroutine
        private Coroutine _combatLoopCoroutine;
        #endregion

        #region Animation Hashes
        private static readonly int AnimAutoAttack = Animator.StringToHash("AutoAttack");
        private static readonly int AnimAutoSpecial = Animator.StringToHash("AutoSpecial");
        private static readonly int AnimAutoDodge = Animator.StringToHash("AutoDodge");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            var player = FindObjectOfType<Characters.PlayerController>();
            if (player != null)
            {
                _playerAnimator = player.GetComponentInChildren<Animator>();
                _playerTransform = player.transform;
            }
        }
        #endregion

        #region ICombatModeHandler Implementation
        public void Activate()
        {
            _isActive = true;
            Debug.Log("[AutomatedCombat] Activated");
        }

        public void Deactivate()
        {
            _isActive = false;
            _isFighting = false;
            
            if (_combatLoopCoroutine != null)
            {
                StopCoroutine(_combatLoopCoroutine);
                _combatLoopCoroutine = null;
            }
            
            Debug.Log("[AutomatedCombat] Deactivated");
        }

        public void Update()
        {
            if (!_isActive || !_isFighting) return;
            
            // Clean up dead enemies
            _enemies.RemoveAll(e => e == null || !e.IsAlive);
            
            // Check for victory
            if (_enemies.Count == 0 && CombatController.Instance.IsInCombat)
            {
                CombatController.Instance.EndCombat(true);
            }
            
            // Auto-select new target if current died
            if (_autoSelectTargets && (_currentTarget == null || !_currentTarget.IsAlive))
            {
                AutoSelectTarget();
            }
        }

        public void StartCombat(EnemyBase[] enemies)
        {
            _enemies = new List<EnemyBase>(enemies);
            _lastAttackTime = 0;
            _lastAbilityTime = 0;
            
            // Select initial target
            AutoSelectTarget();
            
            // Start combat loop
            _isFighting = true;
            _combatLoopCoroutine = StartCoroutine(CombatLoop());
            
            Debug.Log($"[AutomatedCombat] Combat started with {enemies.Length} enemies");
        }

        public void EndCombat(bool victory)
        {
            _isFighting = false;
            
            if (_combatLoopCoroutine != null)
            {
                StopCoroutine(_combatLoopCoroutine);
                _combatLoopCoroutine = null;
            }
            
            _enemies.Clear();
            _currentTarget = null;
            
            Debug.Log($"[AutomatedCombat] Combat ended. Victory: {victory}");
        }

        public void OnAttackInput()
        {
            // In auto mode, tapping selects target or triggers immediate attack
            if (_currentTarget != null)
            {
                // Trigger immediate attack
                PerformAutoAttack();
            }
        }

        public void OnDodgeInput()
        {
            // Manual dodge override
            PerformAutoDodge();
        }

        public void OnCounterInput()
        {
            // Auto mode doesn't have manual counters
        }

        public void OnSpecialInput(int abilityIndex)
        {
            // Queue ability for next attack
            _selectedAbility = abilityIndex;
            
            // If cooldown is ready, use immediately
            if (Time.time - _lastAbilityTime >= _specialAbilityCooldown)
            {
                UseSpecialAbility(abilityIndex);
            }
        }

        public void SwitchTarget()
        {
            if (_enemies.Count <= 1) return;
            
            int currentIndex = _enemies.IndexOf(_currentTarget);
            int nextIndex = (currentIndex + 1) % _enemies.Count;
            
            SetTarget(_enemies[nextIndex]);
        }
        #endregion

        #region Combat Loop
        private IEnumerator CombatLoop()
        {
            while (_isFighting && _enemies.Count > 0)
            {
                // Wait for attack interval
                yield return new WaitForSeconds(_attackInterval);
                
                if (!_isFighting) break;
                
                // Check if we have a target
                if (_currentTarget == null || !_currentTarget.IsAlive)
                {
                    AutoSelectTarget();
                    if (_currentTarget == null)
                    {
                        continue;
                    }
                }
                
                // Random chance to dodge (simulates enemy attacking)
                if (Random.value < _dodgeChance)
                {
                    PerformAutoDodge();
                    yield return new WaitForSeconds(0.5f);
                }
                
                // Check if we should use ability
                if (_autoUseAbilities && 
                    _selectedAbility >= 0 && 
                    Time.time - _lastAbilityTime >= _specialAbilityCooldown)
                {
                    UseSpecialAbility(_selectedAbility);
                    _selectedAbility = -1;
                }
                else
                {
                    // Perform regular attack
                    PerformAutoAttack();
                }
            }
        }
        #endregion

        #region Combat Actions
        private void PerformAutoAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;
            
            _lastAttackTime = Time.time;
            
            // Play attack animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger(AnimAutoAttack);
            }
            
            // Face target
            if (_playerTransform != null)
            {
                Vector3 lookDir = _currentTarget.transform.position - _playerTransform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    _playerTransform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            
            // Calculate damage (auto attacks are consistent)
            float damage = 15f;
            bool isCritical = CombatController.Instance.RollCritical();
            
            // Register hit
            CombatController.Instance.RegisterHit(_currentTarget, damage, isCritical);
            
            OnAutoAttackPerformed?.Invoke();
            
            Debug.Log($"[AutomatedCombat] Auto attack on {_currentTarget.name} for {damage} damage");
        }

        private void PerformAutoDodge()
        {
            // Play dodge animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger(AnimAutoDodge);
            }
            
            CombatController.Instance.RegisterDodge();
            
            Debug.Log("[AutomatedCombat] Auto dodge performed");
        }

        private void UseSpecialAbility(int abilityIndex)
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) return;
            
            _lastAbilityTime = Time.time;
            
            // Play special ability animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetInteger("AbilityIndex", abilityIndex);
                _playerAnimator.SetTrigger(AnimAutoSpecial);
            }
            
            // Special abilities do more damage
            float damage = 30f + (abilityIndex * 10f);
            
            CombatController.Instance.RegisterHit(_currentTarget, damage, true);
            
            OnAbilityUsed?.Invoke(abilityIndex);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            
            Debug.Log($"[AutomatedCombat] Used ability {abilityIndex} for {damage} damage");
        }
        #endregion

        #region Target Selection
        private void AutoSelectTarget()
        {
            if (_enemies.Count == 0) return;
            
            // Find nearest alive enemy
            EnemyBase nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var enemy in _enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                
                float dist = _playerTransform != null 
                    ? Vector3.Distance(_playerTransform.position, enemy.transform.position)
                    : 0f;
                    
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }
            
            if (nearest != null)
            {
                SetTarget(nearest);
            }
        }

        private void SetTarget(EnemyBase enemy)
        {
            _currentTarget = enemy;
            CombatController.Instance.SetTarget(enemy);
            OnAutoTargetSelected?.Invoke(enemy);
            
            Debug.Log($"[AutomatedCombat] Target set to: {enemy.name}");
        }

        /// <summary>
        /// Manually select a target (from UI tap on enemy).
        /// </summary>
        public void SelectTarget(EnemyBase enemy)
        {
            if (enemy != null && enemy.IsAlive && _enemies.Contains(enemy))
            {
                SetTarget(enemy);
            }
        }
        #endregion

        #region UI Helpers
        /// <summary>
        /// Get the current target for UI display.
        /// </summary>
        public EnemyBase GetCurrentTarget()
        {
            return _currentTarget;
        }

        /// <summary>
        /// Get all enemies for UI display.
        /// </summary>
        public List<EnemyBase> GetAllEnemies()
        {
            return new List<EnemyBase>(_enemies);
        }

        /// <summary>
        /// Check if an ability is ready to use.
        /// </summary>
        public bool IsAbilityReady()
        {
            return Time.time - _lastAbilityTime >= _specialAbilityCooldown;
        }

        /// <summary>
        /// Get ability cooldown remaining.
        /// </summary>
        public float GetAbilityCooldownRemaining()
        {
            float remaining = _specialAbilityCooldown - (Time.time - _lastAbilityTime);
            return Mathf.Max(0, remaining);
        }
        #endregion
    }
}

