using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT BATTLE SYSTEM! ⚔️🤖
    /// Epic robot vs robot combat!
    /// Real-time battles with strategy and skill!
    /// </summary>
    public class RobotBattleSystem : MonoBehaviour
    {
        public static RobotBattleSystem Instance { get; private set; }

        [Header("Battle State")]
        [SerializeField] private BattleState _currentState;
        [SerializeField] private RobotBattler _robot1;
        [SerializeField] private RobotBattler _robot2;

        [Header("Battle Settings")]
        [SerializeField] private float _battleTimeLimit = 180f; // 3 minutes
        [SerializeField] private float _roundTimeLimit = 60f;
        [SerializeField] private int _roundsToWin = 2;
        [SerializeField] private float _arenaRadius = 20f;

        [Header("Current Match")]
        [SerializeField] private int _robot1Wins;
        [SerializeField] private int _robot2Wins;
        [SerializeField] private int _currentRound;
        [SerializeField] private float _roundTimer;

        // Events
        public event Action<RobotBattler, RobotBattler> OnBattleStarted;
        public event Action<RobotBattler> OnRoundWon;
        public event Action<RobotBattler, BattleResult> OnBattleEnded;
        public event Action<RobotBattler, int> OnDamageDealt;
        public event Action<RobotBattler, RobotAbility> OnAbilityUsed;
        public event Action<float> OnTimerUpdate;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Battle Setup

        /// <summary>
        /// Start a battle between two robots!
        /// </summary>
        public void StartBattle(RobotData robot1Data, RobotData robot2Data)
        {
            // Create battlers
            _robot1 = new RobotBattler(robot1Data);
            _robot2 = new RobotBattler(robot2Data);

            // Reset match state
            _robot1Wins = 0;
            _robot2Wins = 0;
            _currentRound = 1;

            _currentState = BattleState.Starting;

            Debug.Log($"⚔️ BATTLE START!");
            Debug.Log($"   {robot1Data.robotName} VS {robot2Data.robotName}");

            OnBattleStarted?.Invoke(_robot1, _robot2);

            StartCoroutine(BattleSequence());
        }

        private IEnumerator BattleSequence()
        {
            // Pre-battle intro
            yield return new WaitForSeconds(2f);

            while (_robot1Wins < _roundsToWin && _robot2Wins < _roundsToWin)
            {
                yield return StartCoroutine(RunRound());
                _currentRound++;
                yield return new WaitForSeconds(2f); // Between rounds
            }

            // Battle ended
            EndBattle();
        }

        private IEnumerator RunRound()
        {
            Debug.Log($"🔔 Round {_currentRound} START!");

            // Reset health for new round
            _robot1.ResetForRound();
            _robot2.ResetForRound();

            _currentState = BattleState.Fighting;
            _roundTimer = _roundTimeLimit;

            // Main battle loop
            while (_currentState == BattleState.Fighting)
            {
                _roundTimer -= Time.deltaTime;
                OnTimerUpdate?.Invoke(_roundTimer);

                // Process AI decisions
                ProcessRobotAI(_robot1, _robot2);
                ProcessRobotAI(_robot2, _robot1);

                // Check win conditions
                if (_robot1.CurrentHP <= 0)
                {
                    _robot2Wins++;
                    OnRoundWon?.Invoke(_robot2);
                    Debug.Log($"🏆 Round {_currentRound}: {_robot2.Data.robotName} wins!");
                    _currentState = BattleState.RoundEnd;
                }
                else if (_robot2.CurrentHP <= 0)
                {
                    _robot1Wins++;
                    OnRoundWon?.Invoke(_robot1);
                    Debug.Log($"🏆 Round {_currentRound}: {_robot1.Data.robotName} wins!");
                    _currentState = BattleState.RoundEnd;
                }
                else if (_roundTimer <= 0)
                {
                    // Time out - winner is whoever has more HP
                    if (_robot1.CurrentHP > _robot2.CurrentHP)
                    {
                        _robot1Wins++;
                        OnRoundWon?.Invoke(_robot1);
                    }
                    else if (_robot2.CurrentHP > _robot1.CurrentHP)
                    {
                        _robot2Wins++;
                        OnRoundWon?.Invoke(_robot2);
                    }
                    // Else draw - no winner for this round
                    _currentState = BattleState.RoundEnd;
                }

                yield return null;
            }
        }

        private void EndBattle()
        {
            _currentState = BattleState.Ended;

            RobotBattler winner = _robot1Wins > _robot2Wins ? _robot1 : _robot2;
            RobotBattler loser = _robot1Wins > _robot2Wins ? _robot2 : _robot1;

            var result = new BattleResult
            {
                winner = winner,
                loser = loser,
                winnerRoundsWon = Mathf.Max(_robot1Wins, _robot2Wins),
                loserRoundsWon = Mathf.Min(_robot1Wins, _robot2Wins),
                totalRounds = _currentRound - 1,
                totalDamageDealt = winner.TotalDamageDealt + loser.TotalDamageDealt,
                battleDuration = _battleTimeLimit - _roundTimer
            };

            // Update battle histories
            UpdateBattleHistory(winner, loser, result);

            Debug.Log($"🎊 BATTLE ENDED!");
            Debug.Log($"   Winner: {winner.Data.robotName} ({result.winnerRoundsWon}-{result.loserRoundsWon})");

            OnBattleEnded?.Invoke(winner, result);
        }

        private void UpdateBattleHistory(RobotBattler winner, RobotBattler loser, BattleResult result)
        {
            // Winner stats
            winner.Data.battleHistory.totalBattles++;
            winner.Data.battleHistory.wins++;
            winner.Data.battleHistory.currentWinStreak++;
            winner.Data.battleHistory.longestWinStreak = Mathf.Max(
                winner.Data.battleHistory.longestWinStreak,
                winner.Data.battleHistory.currentWinStreak
            );
            winner.Data.battleHistory.totalDamageDealt += winner.TotalDamageDealt;
            winner.Data.battleHistory.totalDamageTaken += winner.TotalDamageTaken;
            winner.Data.battleHistory.rankingPoints += 25;

            // Loser stats
            loser.Data.battleHistory.totalBattles++;
            loser.Data.battleHistory.losses++;
            loser.Data.battleHistory.currentWinStreak = 0;
            loser.Data.battleHistory.totalDamageDealt += loser.TotalDamageDealt;
            loser.Data.battleHistory.totalDamageTaken += loser.TotalDamageTaken;
            loser.Data.battleHistory.rankingPoints = Mathf.Max(0, loser.Data.battleHistory.rankingPoints - 10);

            // Update ranks
            UpdateRankTier(winner.Data.battleHistory);
            UpdateRankTier(loser.Data.battleHistory);
        }

        private void UpdateRankTier(RobotBattleHistory history)
        {
            history.rankTier = history.rankingPoints switch
            {
                < 100 => "Bronze",
                < 300 => "Silver",
                < 600 => "Gold",
                < 1000 => "Platinum",
                < 1500 => "Diamond",
                < 2000 => "Champion",
                _ => "Legend"
            };
        }

        #endregion

        #region AI Processing

        private void ProcessRobotAI(RobotBattler robot, RobotBattler opponent)
        {
            if (robot.IsStunned || robot.ActionCooldown > 0)
            {
                robot.ActionCooldown -= Time.deltaTime;
                return;
            }

            // Get AI decision
            var decision = MakeAIDecision(robot, opponent);

            // Execute decision
            ExecuteDecision(robot, opponent, decision);
        }

        private AIDecision MakeAIDecision(RobotBattler robot, RobotBattler opponent)
        {
            var ai = robot.Data.aiConfig;
            var decision = new AIDecision();

            // Calculate situation factors
            float healthRatio = (float)robot.CurrentHP / robot.MaxHP;
            float opponentHealthRatio = (float)opponent.CurrentHP / opponent.MaxHP;
            float distance = Vector3.Distance(robot.Position, opponent.Position);

            // Adjust behavior based on fighting style
            float aggressionModifier = ai.primaryStyle switch
            {
                FightingStyle.Aggressive => 1.5f,
                FightingStyle.Berserker => 2.0f - healthRatio, // More aggressive when low HP
                FightingStyle.Defensive => 0.5f,
                FightingStyle.Tank => 0.7f,
                FightingStyle.Assassin => opponentHealthRatio < 0.3f ? 2.0f : 0.8f,
                FightingStyle.Evasive => 0.6f,
                _ => 1.0f
            };

            float effectiveAggression = (ai.aggression / 100f) * aggressionModifier;
            float effectiveCaution = (ai.caution / 100f) * (1f / aggressionModifier);

            // Decision: Attack, Defend, or Move
            float attackScore = effectiveAggression * (1f - effectiveCaution * (1f - healthRatio));
            float defendScore = effectiveCaution * (1f - healthRatio);
            float moveScore = 0.3f;

            // Adjust based on distance
            if (distance > ai.preferences.preferredDistance)
            {
                moveScore += 0.3f;
                decision.moveDirection = (opponent.Position - robot.Position).normalized;
            }
            else if (distance < ai.preferences.preferredDistance * 0.5f)
            {
                moveScore += 0.2f;
                decision.moveDirection = (robot.Position - opponent.Position).normalized;
            }

            // Choose action
            float total = attackScore + defendScore + moveScore;
            float roll = UnityEngine.Random.value * total;

            if (roll < attackScore)
            {
                decision.action = AIAction.Attack;
                decision.abilityIndex = ChooseAbility(robot, opponent);
            }
            else if (roll < attackScore + defendScore)
            {
                decision.action = AIAction.Defend;
            }
            else
            {
                decision.action = AIAction.Move;
            }

            return decision;
        }

        private int ChooseAbility(RobotBattler robot, RobotBattler opponent)
        {
            // Simple ability selection - choose available ability
            var abilities = robot.Data.abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (robot.CurrentEnergy >= abilities[i].energyCost &&
                    robot.AbilityCooldowns[i] <= 0)
                {
                    return i;
                }
            }
            return -1; // Basic attack
        }

        private void ExecuteDecision(RobotBattler robot, RobotBattler opponent, AIDecision decision)
        {
            switch (decision.action)
            {
                case AIAction.Attack:
                    ExecuteAttack(robot, opponent, decision.abilityIndex);
                    break;
                case AIAction.Defend:
                    robot.IsBlocking = true;
                    robot.ActionCooldown = 0.5f;
                    break;
                case AIAction.Move:
                    robot.Position += decision.moveDirection * robot.Data.coreStats.speed * 0.1f * Time.deltaTime;
                    robot.ActionCooldown = 0.1f;
                    break;
            }
        }

        private void ExecuteAttack(RobotBattler attacker, RobotBattler defender, int abilityIndex)
        {
            int damage;
            string attackName;

            if (abilityIndex >= 0 && abilityIndex < attacker.Data.abilities.Count)
            {
                var ability = attacker.Data.abilities[abilityIndex];
                damage = CalculateAbilityDamage(attacker, defender, ability);
                attackName = ability.abilityName;
                attacker.CurrentEnergy -= ability.energyCost;
                attacker.AbilityCooldowns[abilityIndex] = ability.cooldown;
                OnAbilityUsed?.Invoke(attacker, ability);
            }
            else
            {
                // Basic attack
                damage = CalculateBasicDamage(attacker, defender);
                attackName = "Basic Attack";
            }

            // Apply damage
            if (defender.IsBlocking)
            {
                damage = (int)(damage * 0.3f); // Blocked
                defender.IsBlocking = false;
            }

            // Evasion check
            if (UnityEngine.Random.value * 100 < defender.Data.combatStats.evasionChance)
            {
                damage = 0;
                Debug.Log($"   {defender.Data.robotName} EVADED!");
            }

            // Critical hit
            if (UnityEngine.Random.value * 100 < attacker.Data.combatStats.criticalChance)
            {
                damage = (int)(damage * attacker.Data.combatStats.criticalDamage / 100f);
                Debug.Log($"   💥 CRITICAL HIT!");
            }

            defender.CurrentHP -= damage;
            attacker.TotalDamageDealt += damage;
            defender.TotalDamageTaken += damage;

            attacker.ActionCooldown = 1f / (attacker.Data.coreStats.speed / 50f);

            Debug.Log($"⚔️ {attacker.Data.robotName} uses {attackName}!");
            Debug.Log($"   {defender.Data.robotName} takes {damage} damage! ({defender.CurrentHP}/{defender.MaxHP} HP)");

            OnDamageDealt?.Invoke(defender, damage);
        }

        private int CalculateBasicDamage(RobotBattler attacker, RobotBattler defender)
        {
            float baseDamage = attacker.Data.coreStats.power * 0.5f;
            float defense = defender.Data.combatStats.physicalArmor / 100f;
            return Mathf.Max(1, (int)(baseDamage * (1f - defense)));
        }

        private int CalculateAbilityDamage(RobotBattler attacker, RobotBattler defender, RobotAbility ability)
        {
            float baseDamage = ability.baseDamage;

            // Apply power scaling
            baseDamage *= (1f + attacker.Data.coreStats.power / 200f);

            // Apply elemental bonus/weakness
            float elementalMultiplier = GetElementalMultiplier(ability.element, defender);
            baseDamage *= elementalMultiplier;

            // Apply defense
            float defense = ability.element switch
            {
                AbilityElement.Physical => defender.Data.combatStats.physicalArmor / 100f,
                _ => defender.Data.combatStats.energyShielding / 100f
            };

            return Mathf.Max(1, (int)(baseDamage * (1f - defense)));
        }

        private float GetElementalMultiplier(AbilityElement attackElement, RobotBattler defender)
        {
            // Elemental advantages/weaknesses
            return attackElement switch
            {
                AbilityElement.Water => defender.Data.combatStats.elementalAffinity.fire > 20 ? 1.5f : 1f,
                AbilityElement.Fire => defender.Data.combatStats.elementalAffinity.nature > 20 ? 1.5f : 1f,
                AbilityElement.Earth => defender.Data.combatStats.elementalAffinity.lightning > 20 ? 1.5f : 1f,
                AbilityElement.Wind => defender.Data.combatStats.elementalAffinity.earth > 20 ? 1.5f : 1f,
                AbilityElement.Lightning => defender.Data.combatStats.elementalAffinity.water > 20 ? 1.5f : 1f,
                AbilityElement.Nature => defender.Data.combatStats.elementalAffinity.shadow > 20 ? 1.5f : 1f,
                AbilityElement.Celestial => defender.Data.combatStats.elementalAffinity.shadow > 20 ? 1.5f : 1f,
                AbilityElement.Shadow => defender.Data.combatStats.elementalAffinity.celestial > 20 ? 1.5f : 1f,
                _ => 1f
            };
        }

        #endregion

        // Public accessors
        public BattleState GetBattleState() => _currentState;
        public RobotBattler GetRobot1() => _robot1;
        public RobotBattler GetRobot2() => _robot2;
        public float GetRoundTimer() => _roundTimer;
        public int GetRobot1Wins() => _robot1Wins;
        public int GetRobot2Wins() => _robot2Wins;
    }

    #region Battle Classes

    public enum BattleState
    {
        Idle,
        Starting,
        Fighting,
        RoundEnd,
        Ended
    }

    public enum AIAction
    {
        Attack,
        Defend,
        Move,
        UseItem
    }

    public class AIDecision
    {
        public AIAction action;
        public int abilityIndex;
        public Vector3 moveDirection;
    }

    /// <summary>
    /// Runtime battle state for a robot
    /// </summary>
    [Serializable]
    public class RobotBattler
    {
        public RobotData Data { get; private set; }

        // Battle state
        public int CurrentHP;
        public int MaxHP;
        public int CurrentEnergy;
        public int MaxEnergy;
        public Vector3 Position;
        public Quaternion Rotation;

        // Status
        public bool IsStunned;
        public bool IsBlocking;
        public float ActionCooldown;
        public float[] AbilityCooldowns;

        // Stats tracking
        public int TotalDamageDealt;
        public int TotalDamageTaken;
        public int AbilitiesUsed;

        public RobotBattler(RobotData data)
        {
            Data = data;
            MaxHP = data.coreStats.healthPoints;
            MaxEnergy = data.coreStats.energyCapacity;
            AbilityCooldowns = new float[data.abilities.Count];
            ResetForRound();
        }

        public void ResetForRound()
        {
            CurrentHP = MaxHP;
            CurrentEnergy = MaxEnergy;
            IsStunned = false;
            IsBlocking = false;
            ActionCooldown = 0;

            for (int i = 0; i < AbilityCooldowns.Length; i++)
            {
                AbilityCooldowns[i] = 0;
            }
        }
    }

    [Serializable]
    public class BattleResult
    {
        public RobotBattler winner;
        public RobotBattler loser;
        public int winnerRoundsWon;
        public int loserRoundsWon;
        public int totalRounds;
        public int totalDamageDealt;
        public float battleDuration;
    }

    #endregion
}

