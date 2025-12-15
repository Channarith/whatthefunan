using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Gameplay
{
    /// <summary>
    /// Epic boss battle system with phases, mechanics, and rewards.
    /// Bosses are mythological creatures from Funan lore.
    /// </summary>
    public class BossBattle : MonoBehaviour
    {
        #region Events
        public static event Action<BossData> OnBossBattleStarted;
        public static event Action<BossData, int> OnBossPhaseChanged; // boss, phase
        public static event Action<float> OnBossHealthChanged; // health percent
        public static event Action<BossData, bool> OnBossBattleEnded; // boss, victory
        public static event Action<BossAttack> OnBossAttacking;
        public static event Action OnBossStaggered;
        #endregion

        #region Boss Data
        [Header("Boss Configuration")]
        [SerializeField] private BossData _bossData;
        
        private float _currentHealth;
        private int _currentPhase;
        private bool _isActive;
        private bool _isStaggered;
        private float _staggerTimer;
        
        public BossData Boss => _bossData;
        public float HealthPercent => _bossData.maxHealth > 0 ? _currentHealth / _bossData.maxHealth : 0;
        public int CurrentPhase => _currentPhase;
        public bool IsActive => _isActive;
        public bool IsStaggered => _isStaggered;
        #endregion

        #region Battle Settings
        [Header("Battle Settings")]
        [SerializeField] private float _staggerDuration = 3f;
        [SerializeField] private float _attackInterval = 3f;
        [SerializeField] private float _phaseTransitionDuration = 2f;
        
        private float _attackTimer;
        private int _attackIndex;
        private Coroutine _battleCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isActive) return;
            
            if (_isStaggered)
            {
                UpdateStagger();
            }
            else
            {
                UpdateAttackPattern();
            }
        }
        #endregion

        #region Battle Flow
        /// <summary>
        /// Start the boss battle.
        /// </summary>
        public void StartBattle()
        {
            if (_bossData == null)
            {
                Debug.LogError("[BossBattle] No boss data configured");
                return;
            }
            
            _currentHealth = _bossData.maxHealth;
            _currentPhase = 0;
            _isActive = true;
            _isStaggered = false;
            _attackTimer = _attackInterval;
            _attackIndex = 0;
            
            Core.GameManager.Instance?.EnterCombat();
            OnBossBattleStarted?.Invoke(_bossData);
            
            Debug.Log($"[BossBattle] Battle started with {_bossData.bossName}");
            
            // Start intro sequence
            _battleCoroutine = StartCoroutine(BossIntroSequence());
        }

        private IEnumerator BossIntroSequence()
        {
            // Boss entrance animation
            yield return new WaitForSeconds(2f);
            
            // Begin attack patterns
            _attackTimer = _attackInterval;
        }

        /// <summary>
        /// Deal damage to the boss.
        /// </summary>
        public void DealDamage(float damage)
        {
            if (!_isActive) return;
            
            // Bonus damage during stagger
            if (_isStaggered)
            {
                damage *= 2f;
            }
            
            _currentHealth -= damage;
            _currentHealth = Mathf.Max(0, _currentHealth);
            
            OnBossHealthChanged?.Invoke(HealthPercent);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            // Check phase transition
            CheckPhaseTransition();
            
            // Check defeat
            if (_currentHealth <= 0)
            {
                DefeatBoss();
            }
        }

        private void CheckPhaseTransition()
        {
            if (_bossData.phases.Count == 0) return;
            
            for (int i = _bossData.phases.Count - 1; i >= 0; i--)
            {
                var phase = _bossData.phases[i];
                if (HealthPercent <= phase.healthThreshold && i > _currentPhase)
                {
                    TransitionToPhase(i);
                    break;
                }
            }
        }

        private void TransitionToPhase(int phaseIndex)
        {
            _currentPhase = phaseIndex;
            _isStaggered = true;
            _staggerTimer = _phaseTransitionDuration;
            
            OnBossPhaseChanged?.Invoke(_bossData, _currentPhase);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Heavy);
            
            Debug.Log($"[BossBattle] Phase transition to phase {_currentPhase}");
        }

        private void DefeatBoss()
        {
            _isActive = false;
            
            if (_battleCoroutine != null)
            {
                StopCoroutine(_battleCoroutine);
            }
            
            OnBossBattleEnded?.Invoke(_bossData, true);
            Core.HapticManager.Instance?.OnBossDefeated();
            
            // Grant rewards
            GrantVictoryRewards();
            
            Core.GameManager.Instance?.ExitCombat();
            
            Debug.Log($"[BossBattle] {_bossData.bossName} defeated!");
        }

        /// <summary>
        /// End battle in defeat.
        /// </summary>
        public void PlayerDefeated()
        {
            _isActive = false;
            
            if (_battleCoroutine != null)
            {
                StopCoroutine(_battleCoroutine);
            }
            
            OnBossBattleEnded?.Invoke(_bossData, false);
            Core.GameManager.Instance?.ExitCombat();
            
            Debug.Log($"[BossBattle] Player defeated by {_bossData.bossName}");
        }
        #endregion

        #region Attack Patterns
        private void UpdateAttackPattern()
        {
            _attackTimer -= Time.deltaTime;
            
            if (_attackTimer <= 0)
            {
                ExecuteAttack();
                _attackTimer = GetAttackInterval();
            }
        }

        private void ExecuteAttack()
        {
            var phase = GetCurrentPhase();
            if (phase == null || phase.attacks.Count == 0) return;
            
            // Select attack (can be random or sequential)
            BossAttack attack;
            if (phase.randomAttacks)
            {
                attack = phase.attacks[UnityEngine.Random.Range(0, phase.attacks.Count)];
            }
            else
            {
                attack = phase.attacks[_attackIndex % phase.attacks.Count];
                _attackIndex++;
            }
            
            OnBossAttacking?.Invoke(attack);
            
            // Execute attack behavior
            StartCoroutine(ExecuteAttackCoroutine(attack));
            
            Debug.Log($"[BossBattle] Executing attack: {attack.attackName}");
        }

        private IEnumerator ExecuteAttackCoroutine(BossAttack attack)
        {
            // Warning phase
            yield return new WaitForSeconds(attack.warningDuration);
            
            // Deal damage to player (if not dodged)
            // PlayerCombat.Instance?.TakeDamage(attack.damage);
            
            // Recovery phase
            yield return new WaitForSeconds(attack.recoveryDuration);
        }

        private float GetAttackInterval()
        {
            var phase = GetCurrentPhase();
            if (phase != null && phase.attackSpeedMultiplier > 0)
            {
                return _attackInterval / phase.attackSpeedMultiplier;
            }
            return _attackInterval;
        }

        private BossPhase GetCurrentPhase()
        {
            if (_currentPhase < _bossData.phases.Count)
            {
                return _bossData.phases[_currentPhase];
            }
            return null;
        }
        #endregion

        #region Stagger
        private void UpdateStagger()
        {
            _staggerTimer -= Time.deltaTime;
            
            if (_staggerTimer <= 0)
            {
                _isStaggered = false;
                Debug.Log("[BossBattle] Boss recovered from stagger");
            }
        }

        /// <summary>
        /// Stagger the boss (opens for bonus damage).
        /// </summary>
        public void StaggerBoss()
        {
            if (_isStaggered) return;
            
            _isStaggered = true;
            _staggerTimer = _staggerDuration;
            
            OnBossStaggered?.Invoke();
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Heavy);
            
            Debug.Log("[BossBattle] Boss staggered!");
        }
        #endregion

        #region Rewards
        private void GrantVictoryRewards()
        {
            Economy.CurrencyManager.Instance?.AddCoins(_bossData.rewardCoins);
            Economy.CurrencyManager.Instance?.AddGems(_bossData.rewardGems);
            
            // Unlock boss in collection
            Collection.CollectionManager.Instance?.UnlockEntry($"boss_{_bossData.bossId}");
            
            // First-time rewards
            string firstTimeKey = $"Boss_FirstKill_{_bossData.bossId}";
            if (PlayerPrefs.GetInt(firstTimeKey, 0) == 0)
            {
                PlayerPrefs.SetInt(firstTimeKey, 1);
                PlayerPrefs.Save();
                
                // Bonus first-time rewards
                Economy.CurrencyManager.Instance?.AddGems(_bossData.firstTimeGems);
            }
        }
        #endregion
    }

    #region Boss Data Classes
    [Serializable]
    public class BossData
    {
        [Header("Identity")]
        public string bossId;
        public string bossName;
        [TextArea] public string description;
        public Sprite portrait;
        public GameObject prefab;
        
        [Header("Stats")]
        public float maxHealth = 1000f;
        public float baseDamage = 20f;
        
        [Header("Phases")]
        public List<BossPhase> phases = new List<BossPhase>();
        
        [Header("Rewards")]
        public int rewardCoins = 500;
        public int rewardGems = 10;
        public int firstTimeGems = 50;
        public string unlockItemId;
        
        [Header("Cultural/Educational")]
        [TextArea] public string loreText;
    }

    [Serializable]
    public class BossPhase
    {
        public string phaseName;
        [Range(0f, 1f)] public float healthThreshold; // Trigger at this health %
        public List<BossAttack> attacks = new List<BossAttack>();
        public float attackSpeedMultiplier = 1f;
        public bool randomAttacks = false;
        
        [Header("Visual Changes")]
        public Color phaseColor;
        public string animationTrigger;
    }

    [Serializable]
    public class BossAttack
    {
        public string attackId;
        public string attackName;
        public AttackType type;
        public float damage;
        public float warningDuration = 1f;
        public float recoveryDuration = 0.5f;
        
        [Header("Patterns")]
        public AttackPattern pattern;
        public int hitCount = 1;
        public float radius = 5f;
        public float speed = 10f;
        
        [Header("Visuals")]
        public GameObject warningIndicator;
        public GameObject effectPrefab;
    }

    public enum AttackType
    {
        Melee,
        Ranged,
        AOE,
        Charge,
        Summon
    }

    public enum AttackPattern
    {
        SingleTarget,
        Cone,
        Circle,
        Line,
        Spiral,
        Random
    }
    #endregion
}

