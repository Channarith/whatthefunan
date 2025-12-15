using UnityEngine;
using System;
using System.Collections;
using WhatTheFunan.Core;
using WhatTheFunan.Characters;

namespace WhatTheFunan.Combat
{
    /// <summary>
    /// Main combat controller that manages combat state and delegates to combat modes.
    /// Supports three combat modes: Free-Flow, Paired Animation, and Automated.
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        #region Singleton
        private static CombatController _instance;
        public static CombatController Instance => _instance;
        #endregion

        #region Events
        public static event Action OnCombatStarted;
        public static event Action OnCombatEnded;
        public static event Action<CombatMode> OnCombatModeChanged;
        public static event Action<int> OnComboChanged;
        public static event Action OnHitLanded;
        public static event Action OnHitReceived;
        public static event Action OnCriticalHit;
        public static event Action OnDodge;
        public static event Action OnCounter;
        #endregion

        #region Combat Modes
        public enum CombatMode
        {
            FreeFlow,           // Batman Arkham-style fluid combat
            PairedAnimation,    // Cinematic QTE-based combat
            Automated           // Auto-battle for younger players
        }

        [Header("Combat Mode")]
        [SerializeField] private CombatMode _currentMode = CombatMode.FreeFlow;
        public CombatMode CurrentMode
        {
            get => _currentMode;
            private set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnCombatModeChanged?.Invoke(_currentMode);
                    SaveCombatModePreference();
                }
            }
        }
        #endregion

        #region Combat Mode Handlers
        [Header("Mode Handlers")]
        [SerializeField] private FreeFlowCombat _freeFlowHandler;
        [SerializeField] private PairedAnimationCombat _pairedAnimHandler;
        [SerializeField] private AutomatedCombat _automatedHandler;
        
        private ICombatModeHandler _currentHandler;
        #endregion

        #region Combat State
        public enum CombatState
        {
            Inactive,
            Engaging,
            InCombat,
            Finishing,
            Victory,
            Defeat
        }

        [SerializeField] private CombatState _combatState = CombatState.Inactive;
        public CombatState CurrentCombatState => _combatState;
        public bool IsInCombat => _combatState != CombatState.Inactive;
        #endregion

        #region Combo System
        [Header("Combo")]
        [SerializeField] private float _comboTimeout = 2f;
        
        private int _currentCombo = 0;
        private float _lastHitTime;
        
        public int CurrentCombo => _currentCombo;
        #endregion

        #region Combat Stats
        [Header("Combat Stats")]
        [SerializeField] private float _criticalHitChance = 0.1f;
        [SerializeField] private float _criticalHitMultiplier = 2f;
        [SerializeField] private float _dodgeWindow = 0.3f;
        [SerializeField] private float _counterWindow = 0.5f;
        #endregion

        #region Target Management
        private Transform _currentTarget;
        private EnemyBase _currentEnemy;
        
        public Transform CurrentTarget => _currentTarget;
        public EnemyBase CurrentEnemy => _currentEnemy;
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
            
            LoadCombatModePreference();
        }

        private void Start()
        {
            // Set initial combat mode handler
            SetCombatModeHandler(_currentMode);
        }

        private void Update()
        {
            if (!IsInCombat) return;

            // Update combo timeout
            if (_currentCombo > 0 && Time.time - _lastHitTime > _comboTimeout)
            {
                ResetCombo();
            }

            // Update current handler
            _currentHandler?.Update();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region Combat Mode Management
        /// <summary>
        /// Set the combat mode.
        /// </summary>
        public void SetCombatMode(CombatMode mode)
        {
            CurrentMode = mode;
            SetCombatModeHandler(mode);
        }

        /// <summary>
        /// Cycle to the next combat mode.
        /// </summary>
        public void CycleCombatMode()
        {
            int nextMode = ((int)_currentMode + 1) % 3;
            SetCombatMode((CombatMode)nextMode);
        }

        private void SetCombatModeHandler(CombatMode mode)
        {
            // Deactivate current handler
            _currentHandler?.Deactivate();

            // Set new handler
            switch (mode)
            {
                case CombatMode.FreeFlow:
                    _currentHandler = _freeFlowHandler;
                    break;
                case CombatMode.PairedAnimation:
                    _currentHandler = _pairedAnimHandler;
                    break;
                case CombatMode.Automated:
                    _currentHandler = _automatedHandler;
                    break;
            }

            // Activate new handler
            _currentHandler?.Activate();
            
            Debug.Log($"[CombatController] Combat mode set to: {mode}");
        }

        private void SaveCombatModePreference()
        {
            PlayerPrefs.SetInt("CombatMode", (int)_currentMode);
            PlayerPrefs.Save();
        }

        private void LoadCombatModePreference()
        {
            if (PlayerPrefs.HasKey("CombatMode"))
            {
                _currentMode = (CombatMode)PlayerPrefs.GetInt("CombatMode");
            }
        }
        #endregion

        #region Combat Flow
        /// <summary>
        /// Start combat with enemies.
        /// </summary>
        public void StartCombat(EnemyBase[] enemies)
        {
            if (IsInCombat) return;

            _combatState = CombatState.Engaging;
            
            // Notify game manager
            GameManager.Instance?.EnterCombat();
            
            // Set first target
            if (enemies.Length > 0)
            {
                SetTarget(enemies[0]);
            }

            // Initialize combat mode handler
            _currentHandler?.StartCombat(enemies);

            _combatState = CombatState.InCombat;
            OnCombatStarted?.Invoke();
            
            // Haptic feedback
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            
            Debug.Log($"[CombatController] Combat started with {enemies.Length} enemies");
        }

        /// <summary>
        /// End combat.
        /// </summary>
        public void EndCombat(bool victory)
        {
            if (!IsInCombat) return;

            _combatState = victory ? CombatState.Victory : CombatState.Defeat;
            
            // Notify handler
            _currentHandler?.EndCombat(victory);
            
            // Reset state
            ResetCombo();
            ClearTarget();
            
            _combatState = CombatState.Inactive;
            
            // Notify game manager
            GameManager.Instance?.ExitCombat();
            
            OnCombatEnded?.Invoke();
            
            // Haptic feedback
            if (victory)
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            }
            else
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Error);
            }
            
            Debug.Log($"[CombatController] Combat ended. Victory: {victory}");
        }
        #endregion

        #region Target Management
        /// <summary>
        /// Set the current combat target.
        /// </summary>
        public void SetTarget(EnemyBase enemy)
        {
            _currentEnemy = enemy;
            _currentTarget = enemy?.transform;
        }

        /// <summary>
        /// Clear the current target.
        /// </summary>
        public void ClearTarget()
        {
            _currentEnemy = null;
            _currentTarget = null;
        }

        /// <summary>
        /// Switch to the next available target.
        /// </summary>
        public void SwitchTarget()
        {
            _currentHandler?.SwitchTarget();
        }
        #endregion

        #region Combat Actions
        /// <summary>
        /// Request an attack (from input).
        /// </summary>
        public void RequestAttack()
        {
            if (!IsInCombat) return;
            _currentHandler?.OnAttackInput();
        }

        /// <summary>
        /// Request a dodge (from input).
        /// </summary>
        public void RequestDodge()
        {
            if (!IsInCombat) return;
            _currentHandler?.OnDodgeInput();
        }

        /// <summary>
        /// Request a counter (from input).
        /// </summary>
        public void RequestCounter()
        {
            if (!IsInCombat) return;
            _currentHandler?.OnCounterInput();
        }

        /// <summary>
        /// Request a special ability.
        /// </summary>
        public void RequestSpecial(int abilityIndex)
        {
            if (!IsInCombat) return;
            _currentHandler?.OnSpecialInput(abilityIndex);
        }
        #endregion

        #region Damage and Hits
        /// <summary>
        /// Register a hit on an enemy.
        /// </summary>
        public void RegisterHit(EnemyBase enemy, float damage, bool isCritical = false)
        {
            // Apply damage
            float finalDamage = damage;
            
            if (isCritical)
            {
                finalDamage *= _criticalHitMultiplier;
                OnCriticalHit?.Invoke();
                HapticManager.Instance?.OnCriticalHit();
            }
            else
            {
                OnHitLanded?.Invoke();
                HapticManager.Instance?.OnCombatHit();
            }

            enemy?.TakeDamage(finalDamage);
            
            // Update combo
            IncrementCombo();
            _lastHitTime = Time.time;
        }

        /// <summary>
        /// Register damage received by the player.
        /// </summary>
        public void RegisterDamageReceived(float damage)
        {
            OnHitReceived?.Invoke();
            HapticManager.Instance?.OnCombatHitReceived();
            
            // Break combo on hit
            ResetCombo();
            
            // TODO: Apply damage to player health
        }

        /// <summary>
        /// Register a successful dodge.
        /// </summary>
        public void RegisterDodge()
        {
            OnDodge?.Invoke();
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
        }

        /// <summary>
        /// Register a successful counter.
        /// </summary>
        public void RegisterCounter()
        {
            OnCounter?.Invoke();
            IncrementCombo();
            IncrementCombo(); // Double combo for counters
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
        }

        /// <summary>
        /// Check if an attack should be a critical hit.
        /// </summary>
        public bool RollCritical()
        {
            return UnityEngine.Random.value <= _criticalHitChance;
        }
        #endregion

        #region Combo System
        private void IncrementCombo()
        {
            _currentCombo++;
            OnComboChanged?.Invoke(_currentCombo);
        }

        private void ResetCombo()
        {
            if (_currentCombo > 0)
            {
                _currentCombo = 0;
                OnComboChanged?.Invoke(_currentCombo);
            }
        }
        #endregion
    }

    #region Interfaces
    /// <summary>
    /// Interface for combat mode handlers.
    /// </summary>
    public interface ICombatModeHandler
    {
        void Activate();
        void Deactivate();
        void Update();
        void StartCombat(EnemyBase[] enemies);
        void EndCombat(bool victory);
        void OnAttackInput();
        void OnDodgeInput();
        void OnCounterInput();
        void OnSpecialInput(int abilityIndex);
        void SwitchTarget();
    }
    #endregion

    #region Enemy Base (Placeholder)
    /// <summary>
    /// Base class for enemies. Will be expanded later.
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected float _maxHealth = 100f;
        [SerializeField] protected float _currentHealth;
        
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0;

        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public virtual void TakeDamage(float damage)
        {
            _currentHealth -= damage;
            
            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                Die();
            }
        }

        protected virtual void Die()
        {
            Debug.Log($"[Enemy] {gameObject.name} died");
            // TODO: Play death animation, drop loot, etc.
        }
    }
    #endregion
}

