using UnityEngine;
using System.Collections.Generic;
using WhatTheFunan.Core;

namespace WhatTheFunan.Combat
{
    /// <summary>
    /// Free-Flow Combat Mode - Batman Arkham-style fluid combat.
    /// Tap to attack, swipe to dodge/counter, automatic target switching.
    /// Best for older kids (10+).
    /// </summary>
    public class FreeFlowCombat : MonoBehaviour, ICombatModeHandler
    {
        #region Settings
        [Header("Free-Flow Settings")]
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _lungeSpeed = 15f;
        [SerializeField] private float _attackCooldown = 0.3f;
        [SerializeField] private float _comboCooldown = 0.8f;
        [SerializeField] private float _dodgeDistance = 4f;
        [SerializeField] private float _dodgeSpeed = 20f;
        [SerializeField] private float _counterWindow = 0.5f;
        
        [Header("Combat Flow")]
        [SerializeField] private float _autoTargetRange = 8f;
        [SerializeField] private float _targetSwitchAngle = 45f;
        [SerializeField] private bool _autoTargetNearest = true;
        #endregion

        #region State
        private bool _isActive;
        private bool _isAttacking;
        private bool _isDodging;
        private bool _isCountering;
        private float _lastAttackTime;
        private int _comboStep;
        private List<EnemyBase> _enemies = new List<EnemyBase>();
        private EnemyBase _currentTarget;
        
        // Animation
        private Animator _playerAnimator;
        #endregion

        #region Animation Hashes
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimComboStep = Animator.StringToHash("ComboStep");
        private static readonly int AnimDodge = Animator.StringToHash("Dodge");
        private static readonly int AnimCounter = Animator.StringToHash("Counter");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get player animator reference
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
            Debug.Log("[FreeFlowCombat] Activated");
        }

        public void Deactivate()
        {
            _isActive = false;
            _isAttacking = false;
            _isDodging = false;
            _isCountering = false;
            _comboStep = 0;
            Debug.Log("[FreeFlowCombat] Deactivated");
        }

        public void Update()
        {
            if (!_isActive) return;
            
            // Auto-target if no target
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                AutoSelectTarget();
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
            _comboStep = 0;
            _lastAttackTime = 0;
            
            // Select initial target
            AutoSelectTarget();
            
            Debug.Log($"[FreeFlowCombat] Combat started with {enemies.Length} enemies");
        }

        public void EndCombat(bool victory)
        {
            _enemies.Clear();
            _currentTarget = null;
            _comboStep = 0;
            _isAttacking = false;
            _isDodging = false;
            
            Debug.Log($"[FreeFlowCombat] Combat ended. Victory: {victory}");
        }

        public void OnAttackInput()
        {
            if (_isAttacking || _isDodging || _isCountering) return;
            
            // Check attack cooldown
            if (Time.time - _lastAttackTime < _attackCooldown) return;
            
            PerformAttack();
        }

        public void OnDodgeInput()
        {
            if (_isDodging || _isAttacking) return;
            
            PerformDodge();
        }

        public void OnCounterInput()
        {
            if (_isCountering || _isAttacking) return;
            
            PerformCounter();
        }

        public void OnSpecialInput(int abilityIndex)
        {
            // TODO: Implement special abilities
            Debug.Log($"[FreeFlowCombat] Special ability {abilityIndex} requested");
        }

        public void SwitchTarget()
        {
            if (_enemies.Count <= 1) return;
            
            int currentIndex = _enemies.IndexOf(_currentTarget);
            int nextIndex = (currentIndex + 1) % _enemies.Count;
            
            _currentTarget = _enemies[nextIndex];
            CombatController.Instance.SetTarget(_currentTarget);
            
            Debug.Log($"[FreeFlowCombat] Target switched to: {_currentTarget.name}");
        }
        #endregion

        #region Combat Actions
        private void PerformAttack()
        {
            if (_currentTarget == null)
            {
                AutoSelectTarget();
                if (_currentTarget == null) return;
            }

            _isAttacking = true;
            _lastAttackTime = Time.time;
            
            // Advance combo
            _comboStep = (_comboStep % 3) + 1;
            
            // Calculate if we need to lunge to target
            float distanceToTarget = Vector3.Distance(
                transform.position, 
                _currentTarget.transform.position
            );

            if (distanceToTarget > _attackRange)
            {
                // Lunge towards target
                StartCoroutine(LungeToTarget());
            }
            else
            {
                // Attack immediately
                ExecuteAttack();
            }
        }

        private System.Collections.IEnumerator LungeToTarget()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = _currentTarget.transform.position;
            Vector3 direction = (targetPos - startPos).normalized;
            
            // Calculate lunge destination (stop at attack range)
            float lungeDistance = Vector3.Distance(startPos, targetPos) - _attackRange * 0.8f;
            Vector3 lungeDestination = startPos + direction * lungeDistance;
            
            // Face target
            transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
            
            // Animate lunge
            float elapsed = 0f;
            float lungeDuration = lungeDistance / _lungeSpeed;
            
            while (elapsed < lungeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lungeDuration;
                
                // Ease out for smooth landing
                t = 1f - Mathf.Pow(1f - t, 2f);
                
                transform.position = Vector3.Lerp(startPos, lungeDestination, t);
                yield return null;
            }
            
            ExecuteAttack();
        }

        private void ExecuteAttack()
        {
            if (_currentTarget == null)
            {
                _isAttacking = false;
                return;
            }

            // Play attack animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetInteger(AnimComboStep, _comboStep);
                _playerAnimator.SetTrigger(AnimAttack);
            }

            // Calculate damage
            float baseDamage = 10f + (_comboStep * 2f); // Combo increases damage
            bool isCritical = CombatController.Instance.RollCritical();
            
            // Register hit
            CombatController.Instance.RegisterHit(_currentTarget, baseDamage, isCritical);
            
            // Switch to next target if current is dead
            if (!_currentTarget.IsAlive)
            {
                SwitchTarget();
            }

            // Reset attacking state after animation time
            StartCoroutine(ResetAttackState());
        }

        private System.Collections.IEnumerator ResetAttackState()
        {
            yield return new WaitForSeconds(_attackCooldown);
            _isAttacking = false;
        }

        private void PerformDodge()
        {
            _isDodging = true;
            
            // Play dodge animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger(AnimDodge);
            }
            
            // Calculate dodge direction (away from current threat or input direction)
            Vector3 dodgeDirection = -transform.forward;
            
            // Perform dodge movement
            StartCoroutine(DodgeMovement(dodgeDirection));
        }

        private System.Collections.IEnumerator DodgeMovement(Vector3 direction)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + direction * _dodgeDistance;
            
            float elapsed = 0f;
            float duration = _dodgeDistance / _dodgeSpeed;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease in-out for smooth dodge
                t = t * t * (3f - 2f * t);
                
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            CombatController.Instance.RegisterDodge();
            _isDodging = false;
        }

        private void PerformCounter()
        {
            _isCountering = true;
            
            // Play counter animation
            if (_playerAnimator != null)
            {
                _playerAnimator.SetTrigger(AnimCounter);
            }
            
            // Counter deals bonus damage
            if (_currentTarget != null)
            {
                float counterDamage = 25f; // Counter attacks are powerful
                CombatController.Instance.RegisterHit(_currentTarget, counterDamage, true);
                CombatController.Instance.RegisterCounter();
            }
            
            StartCoroutine(ResetCounterState());
        }

        private System.Collections.IEnumerator ResetCounterState()
        {
            yield return new WaitForSeconds(0.5f);
            _isCountering = false;
        }
        #endregion

        #region Target Selection
        private void AutoSelectTarget()
        {
            if (_enemies.Count == 0) return;
            
            if (_autoTargetNearest)
            {
                // Find nearest enemy
                float nearestDist = float.MaxValue;
                EnemyBase nearest = null;
                
                foreach (var enemy in _enemies)
                {
                    if (enemy == null || !enemy.IsAlive) continue;
                    
                    float dist = Vector3.Distance(transform.position, enemy.transform.position);
                    if (dist < nearestDist && dist < _autoTargetRange)
                    {
                        nearestDist = dist;
                        nearest = enemy;
                    }
                }
                
                if (nearest != null)
                {
                    _currentTarget = nearest;
                    CombatController.Instance.SetTarget(_currentTarget);
                }
            }
            else
            {
                // Just pick the first alive enemy
                foreach (var enemy in _enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        _currentTarget = enemy;
                        CombatController.Instance.SetTarget(_currentTarget);
                        break;
                    }
                }
            }
        }
        #endregion
    }
}

