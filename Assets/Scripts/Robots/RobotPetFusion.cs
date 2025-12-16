using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT-PET FUSION SYSTEM! 🤖🐾
    /// Mix and match robots with pets for combined abilities!
    /// Unlock unique synergies and fusion powers!
    /// </summary>
    public class RobotPetFusion : MonoBehaviour
    {
        public static RobotPetFusion Instance { get; private set; }

        [Header("Current Fusion")]
        [SerializeField] private FusedUnit _currentFusion;

        [Header("Synergy Database")]
        [SerializeField] private List<SynergyBonus> _synergyDatabase;

        [Header("Fusion Settings")]
        [SerializeField] private int _maxAbilitiesFromRobot = 4;
        [SerializeField] private int _maxAbilitiesFromPet = 4;
        [SerializeField] private int _maxTotalAbilities = 6;
        [SerializeField] private float _fusionStatBonus = 0.1f; // 10% bonus for fusion

        // Events
        public event Action<FusedUnit> OnFusionCreated;
        public event Action<SynergyBonus> OnSynergyActivated;
        public event Action<FusionAbility> OnFusionAbilityUnlocked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeSynergyDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSynergyDatabase()
        {
            _synergyDatabase = new List<SynergyBonus>
            {
                // ============================================================
                // PERFECT SYNERGIES (Same character robot + pet) ⭐⭐⭐
                // ============================================================
                new SynergyBonus
                {
                    synergyId = "naga_perfect",
                    synergyName = "🐍 Perfect Naga Bond",
                    description = "Naga Bot + Naga Pet = Ultimate serpent power!",
                    robotType = "naga_bot",
                    petType = "naga_serpent",
                    synergyLevel = SynergyLevel.Perfect,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 20f }, { "speed", 15f }, { "precision", 25f }
                    },
                    unlocksFusionAbility = "Tsunami Serpent Strike",
                    fusionAbilityDescription = "All 7 heads unleash water fury simultaneously!"
                },

                new SynergyBonus
                {
                    synergyId = "champa_perfect",
                    synergyName = "🐘 Perfect Champa Bond",
                    description = "Champa Bot + Champa Pet = Unstoppable pachyderm!",
                    robotType = "champa_bot",
                    petType = "champa_elephant",
                    synergyLevel = SynergyLevel.Perfect,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 30f }, { "defense", 25f }, { "energy", 15f }
                    },
                    unlocksFusionAbility = "Elephant Stampede",
                    fusionAbilityDescription = "Create a herd of spirit elephants that trample everything!"
                },

                new SynergyBonus
                {
                    synergyId = "garuda_perfect",
                    synergyName = "🦅 Perfect Garuda Bond",
                    description = "Garuda Bot + Garuda Pet = Divine sky domination!",
                    robotType = "garuda_bot",
                    petType = "garuda_bird",
                    synergyLevel = SynergyLevel.Perfect,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "speed", 30f }, { "precision", 20f }, { "evasion", 25f }
                    },
                    unlocksFusionAbility = "Divine Wind Storm",
                    fusionAbilityDescription = "Summon a tornado of divine feathers that slice enemies!"
                },

                new SynergyBonus
                {
                    synergyId = "sena_perfect",
                    synergyName = "🦁 Perfect Sena Bond",
                    description = "Sena Bot + Sena Pet = Royal lion supremacy!",
                    robotType = "sena_bot",
                    petType = "sena_lion",
                    synergyLevel = SynergyLevel.Perfect,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 35f }, { "defense", 15f }, { "intimidation", 50f }
                    },
                    unlocksFusionAbility = "King's Roar of Judgment",
                    fusionAbilityDescription = "A roar so powerful it damages AND stuns ALL enemies!"
                },

                new SynergyBonus
                {
                    synergyId = "makara_perfect",
                    synergyName = "🔥 Perfect Makara Bond",
                    description = "Makara Bot + Makara Pet = Dragon chaos unleashed!",
                    robotType = "makara_bot",
                    petType = "makara_dragon",
                    synergyLevel = SynergyLevel.Perfect,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 25f }, { "fire_affinity", 30f }, { "water_affinity", 30f }
                    },
                    unlocksFusionAbility = "Steam Apocalypse",
                    fusionAbilityDescription = "Fire + Water = Devastating steam explosion!"
                },

                // ============================================================
                // GREAT SYNERGIES (Complementary elements) ⭐⭐
                // ============================================================
                new SynergyBonus
                {
                    synergyId = "water_wind",
                    synergyName = "🌊💨 Storm Synergy",
                    description = "Water robot + Wind pet = Devastating storms!",
                    robotElement = AbilityElement.Water,
                    petElement = "Wind",
                    synergyLevel = SynergyLevel.Great,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "speed", 15f }, { "precision", 10f }
                    },
                    unlocksFusionAbility = "Hurricane Surge",
                    fusionAbilityDescription = "Wind-powered water attacks with increased range!"
                },

                new SynergyBonus
                {
                    synergyId = "fire_earth",
                    synergyName = "🔥🌍 Volcanic Synergy",
                    description = "Fire robot + Earth pet = Volcanic power!",
                    robotElement = AbilityElement.Fire,
                    petElement = "Earth",
                    synergyLevel = SynergyLevel.Great,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 20f }, { "defense", 10f }
                    },
                    unlocksFusionAbility = "Magma Eruption",
                    fusionAbilityDescription = "Earth shields that explode with fire damage!"
                },

                new SynergyBonus
                {
                    synergyId = "lightning_water",
                    synergyName = "⚡💧 Electro Synergy",
                    description = "Lightning robot + Water pet = Shocking currents!",
                    robotElement = AbilityElement.Lightning,
                    petElement = "Water",
                    synergyLevel = SynergyLevel.Great,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 15f }, { "criticalChance", 20f }
                    },
                    unlocksFusionAbility = "Electrocution Wave",
                    fusionAbilityDescription = "Water conducts lightning to hit multiple enemies!"
                },

                new SynergyBonus
                {
                    synergyId = "celestial_nature",
                    synergyName = "✨🌿 Divine Nature Synergy",
                    description = "Celestial robot + Nature pet = Sacred growth!",
                    robotElement = AbilityElement.Celestial,
                    petElement = "Nature",
                    synergyLevel = SynergyLevel.Great,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "intelligence", 15f }, { "energy", 20f }, { "healing", 25f }
                    },
                    unlocksFusionAbility = "Sacred Garden",
                    fusionAbilityDescription = "Summon healing flowers that also damage shadows!"
                },

                new SynergyBonus
                {
                    synergyId = "shadow_lightning",
                    synergyName = "🌑⚡ Dark Storm Synergy",
                    description = "Shadow robot + Lightning pet = Electric darkness!",
                    robotElement = AbilityElement.Shadow,
                    petElement = "Lightning",
                    synergyLevel = SynergyLevel.Great,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "speed", 10f }, { "criticalDamage", 30f }
                    },
                    unlocksFusionAbility = "Black Lightning",
                    fusionAbilityDescription = "Invisible lightning strikes from the shadows!"
                },

                // ============================================================
                // GOOD SYNERGIES (Compatible pairs) ⭐
                // ============================================================
                new SynergyBonus
                {
                    synergyId = "tank_healer",
                    synergyName = "🛡️💚 Guardian Synergy",
                    description = "Tank robot + Healing pet = Unkillable combo!",
                    robotStyle = FightingStyle.Tank,
                    petRole = "Healer",
                    synergyLevel = SynergyLevel.Good,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "defense", 15f }, { "regeneration", 10f }
                    },
                    unlocksFusionAbility = "Eternal Guardian",
                    fusionAbilityDescription = "Gradually heal while blocking damage!"
                },

                new SynergyBonus
                {
                    synergyId = "assassin_scout",
                    synergyName = "🗡️👁️ Hunter Synergy",
                    description = "Assassin robot + Scout pet = Perfect ambush!",
                    robotStyle = FightingStyle.Assassin,
                    petRole = "Scout",
                    synergyLevel = SynergyLevel.Good,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "criticalChance", 15f }, { "speed", 10f }
                    },
                    unlocksFusionAbility = "Perfect Assassination",
                    fusionAbilityDescription = "Pet marks target for guaranteed critical hit!"
                },

                new SynergyBonus
                {
                    synergyId = "support_buffer",
                    synergyName = "💚✨ Amplifier Synergy",
                    description = "Support robot + Buffer pet = Double the buffs!",
                    robotStyle = FightingStyle.Support,
                    petRole = "Buffer",
                    synergyLevel = SynergyLevel.Good,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "buffStrength", 25f }, { "buffDuration", 20f }
                    },
                    unlocksFusionAbility = "Ultimate Empowerment",
                    fusionAbilityDescription = "All buffs are doubled and last twice as long!"
                },

                // ============================================================
                // SPECIAL MYTHOLOGICAL SYNERGIES 🇰🇭
                // ============================================================
                new SynergyBonus
                {
                    synergyId = "naga_garuda_rivalry",
                    synergyName = "🐍🦅 Ancient Rivals",
                    description = "Naga + Garuda together? Chaos power!",
                    robotType = "naga_bot",
                    petType = "garuda_bird",
                    synergyLevel = SynergyLevel.Legendary,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "power", 25f }, { "speed", 25f }, { "chaosBonus", 50f }
                    },
                    unlocksFusionAbility = "Chaos of the Eternals",
                    fusionAbilityDescription = "Ancient rivals combine for unpredictable devastating attacks!"
                },

                new SynergyBonus
                {
                    synergyId = "mealea_kinnari_dance",
                    synergyName = "💃🎵 Celestial Dance",
                    description = "Mealea Bot + Kinnari Pet = Divine performance!",
                    robotType = "mealea_bot",
                    petType = "kinnari_bird",
                    synergyLevel = SynergyLevel.Legendary,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "celestialAffinity", 40f }, { "evasion", 30f }, { "charm", 50f }
                    },
                    unlocksFusionAbility = "Apsara's Grand Performance",
                    fusionAbilityDescription = "Mesmerize all enemies while dealing celestial damage!"
                },

                new SynergyBonus
                {
                    synergyId = "reahu_moni_eclipse",
                    synergyName = "🌑💎 Eclipse Dance",
                    description = "Reahu + Moni Mekhala = Eclipse event!",
                    robotType = "reahu_bot",
                    petType = "moni_mekhala_spirit",
                    synergyLevel = SynergyLevel.Legendary,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "shadowAffinity", 30f }, { "lightningAffinity", 30f }
                    },
                    unlocksFusionAbility = "Total Eclipse",
                    fusionAbilityDescription = "Darkness covers the battlefield - only your attacks can see!"
                },

                new SynergyBonus
                {
                    synergyId = "hanuman_kavi_monkey",
                    synergyName = "🐒⚔️ Monkey Mayhem",
                    description = "Hanuman Bot + Kavi Pet = Monkey army!",
                    robotType = "hanuman_bot",
                    petType = "kavi_monkey",
                    synergyLevel = SynergyLevel.Legendary,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "speed", 20f }, { "trickery", 40f }, { "comboBonus", 30f }
                    },
                    unlocksFusionAbility = "Infinite Monkey Clones",
                    fusionAbilityDescription = "Summon an army of trickster monkeys!"
                },

                new SynergyBonus
                {
                    synergyId = "thorani_prohm_ancient",
                    synergyName = "🌍🦕 Ancient Earth",
                    description = "Thorani Bot + Prohm Pet = Primordial power!",
                    robotType = "thorani_bot",
                    petType = "prohm_ancient",
                    synergyLevel = SynergyLevel.Legendary,
                    statBonuses = new Dictionary<string, float>
                    {
                        { "defense", 35f }, { "earthAffinity", 40f }, { "regeneration", 25f }
                    },
                    unlocksFusionAbility = "Gaia's Judgement",
                    fusionAbilityDescription = "The earth itself rises to crush your enemies!"
                }
            };
        }

        #region Fusion Creation

        /// <summary>
        /// Create a fusion between robot and pet!
        /// </summary>
        public FusedUnit CreateFusion(RobotData robot, PetData pet, List<string> selectedRobotAbilities, List<string> selectedPetAbilities)
        {
            if (robot == null || pet == null)
            {
                Debug.LogError("Cannot create fusion without both robot and pet!");
                return null;
            }

            var fusion = new FusedUnit
            {
                fusionId = Guid.NewGuid().ToString(),
                fusionName = $"{robot.robotName} + {pet.petName}",
                robot = robot,
                pet = pet,
                createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Calculate combined stats
            CalculateFusedStats(fusion);

            // Select abilities (up to max)
            fusion.selectedAbilities = SelectFusionAbilities(robot, pet, selectedRobotAbilities, selectedPetAbilities);

            // Check for synergies
            fusion.activeSynergies = FindActiveSynergies(robot, pet);

            // Apply synergy bonuses
            ApplySynergyBonuses(fusion);

            // Unlock fusion abilities
            UnlockFusionAbilities(fusion);

            _currentFusion = fusion;

            Debug.Log($"🤖🐾 Fusion created: {fusion.fusionName}");
            Debug.Log($"   Active synergies: {fusion.activeSynergies.Count}");
            Debug.Log($"   Total abilities: {fusion.selectedAbilities.Count}");

            OnFusionCreated?.Invoke(fusion);

            return fusion;
        }

        /// <summary>
        /// Quick fusion with auto-selected abilities
        /// </summary>
        public FusedUnit QuickFusion(RobotData robot, PetData pet)
        {
            // Auto-select best abilities
            var robotAbilities = robot.abilities
                .OrderByDescending(a => a.baseDamage)
                .Take(_maxAbilitiesFromRobot)
                .Select(a => a.abilityId)
                .ToList();

            var petAbilities = pet.abilities
                .OrderByDescending(a => a.power)
                .Take(_maxAbilitiesFromPet)
                .Select(a => a.abilityId)
                .ToList();

            return CreateFusion(robot, pet, robotAbilities, petAbilities);
        }

        #endregion

        #region Stats Calculation

        private void CalculateFusedStats(FusedUnit fusion)
        {
            var robotStats = fusion.robot.coreStats;
            var petStats = fusion.pet.stats;

            // Weighted average (robot 60%, pet 40%) + fusion bonus
            fusion.fusedStats = new FusedStats
            {
                power = CombineStat(robotStats.power, petStats.power, 0.6f, 0.4f),
                speed = CombineStat(robotStats.speed, petStats.speed, 0.6f, 0.4f),
                defense = CombineStat(robotStats.defense, petStats.defense, 0.6f, 0.4f),
                intelligence = CombineStat(robotStats.intelligence, petStats.intelligence, 0.5f, 0.5f),
                energy = CombineStat(robotStats.energy, petStats.energy, 0.5f, 0.5f),
                precision = CombineStat(robotStats.precision, petStats.precision, 0.5f, 0.5f)
            };

            // Apply fusion bonus
            fusion.fusedStats.power = (int)(fusion.fusedStats.power * (1 + _fusionStatBonus));
            fusion.fusedStats.speed = (int)(fusion.fusedStats.speed * (1 + _fusionStatBonus));
            fusion.fusedStats.defense = (int)(fusion.fusedStats.defense * (1 + _fusionStatBonus));
            fusion.fusedStats.intelligence = (int)(fusion.fusedStats.intelligence * (1 + _fusionStatBonus));
            fusion.fusedStats.energy = (int)(fusion.fusedStats.energy * (1 + _fusionStatBonus));
            fusion.fusedStats.precision = (int)(fusion.fusedStats.precision * (1 + _fusionStatBonus));

            // Clamp to max 100
            fusion.fusedStats.power = Mathf.Min(100, fusion.fusedStats.power);
            fusion.fusedStats.speed = Mathf.Min(100, fusion.fusedStats.speed);
            fusion.fusedStats.defense = Mathf.Min(100, fusion.fusedStats.defense);
            fusion.fusedStats.intelligence = Mathf.Min(100, fusion.fusedStats.intelligence);
            fusion.fusedStats.energy = Mathf.Min(100, fusion.fusedStats.energy);
            fusion.fusedStats.precision = Mathf.Min(100, fusion.fusedStats.precision);
        }

        private int CombineStat(int robotStat, int petStat, float robotWeight, float petWeight)
        {
            return Mathf.RoundToInt(robotStat * robotWeight + petStat * petWeight);
        }

        #endregion

        #region Ability Selection

        private List<FusedAbility> SelectFusionAbilities(
            RobotData robot, 
            PetData pet,
            List<string> selectedRobotAbilities,
            List<string> selectedPetAbilities)
        {
            var fusedAbilities = new List<FusedAbility>();

            // Add selected robot abilities
            foreach (var abilityId in selectedRobotAbilities.Take(_maxAbilitiesFromRobot))
            {
                var ability = robot.abilities.FirstOrDefault(a => a.abilityId == abilityId);
                if (ability != null)
                {
                    fusedAbilities.Add(new FusedAbility
                    {
                        abilityId = ability.abilityId,
                        abilityName = ability.abilityName,
                        source = AbilitySource.Robot,
                        damage = ability.baseDamage,
                        energyCost = ability.energyCost,
                        cooldown = ability.cooldown,
                        element = ability.element,
                        description = ability.description
                    });
                }
            }

            // Add selected pet abilities
            foreach (var abilityId in selectedPetAbilities.Take(_maxAbilitiesFromPet))
            {
                var ability = pet.abilities.FirstOrDefault(a => a.abilityId == abilityId);
                if (ability != null)
                {
                    fusedAbilities.Add(new FusedAbility
                    {
                        abilityId = ability.abilityId,
                        abilityName = ability.abilityName,
                        source = AbilitySource.Pet,
                        damage = ability.power,
                        energyCost = ability.energyCost,
                        cooldown = ability.cooldown,
                        element = ability.element,
                        description = ability.description
                    });
                }
            }

            // Limit total abilities
            return fusedAbilities.Take(_maxTotalAbilities).ToList();
        }

        #endregion

        #region Synergy Detection

        private List<SynergyBonus> FindActiveSynergies(RobotData robot, PetData pet)
        {
            var activeSynergies = new List<SynergyBonus>();

            foreach (var synergy in _synergyDatabase)
            {
                if (CheckSynergyMatch(synergy, robot, pet))
                {
                    activeSynergies.Add(synergy);
                    Debug.Log($"✨ Synergy activated: {synergy.synergyName}");
                    OnSynergyActivated?.Invoke(synergy);
                }
            }

            return activeSynergies;
        }

        private bool CheckSynergyMatch(SynergyBonus synergy, RobotData robot, PetData pet)
        {
            // Check specific robot + pet type match
            if (!string.IsNullOrEmpty(synergy.robotType) && !string.IsNullOrEmpty(synergy.petType))
            {
                return robot.characterInspiration == synergy.robotType && 
                       pet.petType == synergy.petType;
            }

            // Check element match
            if (synergy.robotElement != AbilityElement.Physical && !string.IsNullOrEmpty(synergy.petElement))
            {
                var robotElement = robot.combatStats.elementalAffinity.GetDominantElement();
                return robotElement == synergy.robotElement.ToString() && 
                       pet.element == synergy.petElement;
            }

            // Check fighting style + pet role match
            if (synergy.robotStyle != FightingStyle.Balanced && !string.IsNullOrEmpty(synergy.petRole))
            {
                return robot.aiConfig.primaryStyle == synergy.robotStyle && 
                       pet.role == synergy.petRole;
            }

            return false;
        }

        private void ApplySynergyBonuses(FusedUnit fusion)
        {
            foreach (var synergy in fusion.activeSynergies)
            {
                foreach (var bonus in synergy.statBonuses)
                {
                    switch (bonus.Key.ToLower())
                    {
                        case "power":
                            fusion.fusedStats.power += (int)bonus.Value;
                            break;
                        case "speed":
                            fusion.fusedStats.speed += (int)bonus.Value;
                            break;
                        case "defense":
                            fusion.fusedStats.defense += (int)bonus.Value;
                            break;
                        case "intelligence":
                            fusion.fusedStats.intelligence += (int)bonus.Value;
                            break;
                        case "energy":
                            fusion.fusedStats.energy += (int)bonus.Value;
                            break;
                        case "precision":
                            fusion.fusedStats.precision += (int)bonus.Value;
                            break;
                        case "criticalchance":
                            fusion.fusedStats.criticalChance += (int)bonus.Value;
                            break;
                        case "criticaldamage":
                            fusion.fusedStats.criticalDamage += (int)bonus.Value;
                            break;
                        case "evasion":
                            fusion.fusedStats.evasion += (int)bonus.Value;
                            break;
                    }
                }

                // Cap stats at 100
                fusion.fusedStats.power = Mathf.Min(100, fusion.fusedStats.power);
                fusion.fusedStats.speed = Mathf.Min(100, fusion.fusedStats.speed);
                fusion.fusedStats.defense = Mathf.Min(100, fusion.fusedStats.defense);
                fusion.fusedStats.intelligence = Mathf.Min(100, fusion.fusedStats.intelligence);
                fusion.fusedStats.energy = Mathf.Min(100, fusion.fusedStats.energy);
                fusion.fusedStats.precision = Mathf.Min(100, fusion.fusedStats.precision);
            }
        }

        private void UnlockFusionAbilities(FusedUnit fusion)
        {
            fusion.fusionAbilities = new List<FusionAbility>();

            foreach (var synergy in fusion.activeSynergies)
            {
                if (!string.IsNullOrEmpty(synergy.unlocksFusionAbility))
                {
                    var fusionAbility = new FusionAbility
                    {
                        abilityId = $"fusion_{synergy.synergyId}",
                        abilityName = synergy.unlocksFusionAbility,
                        description = synergy.fusionAbilityDescription,
                        synergyRequired = synergy.synergyName,
                        damage = CalculateFusionAbilityDamage(fusion, synergy),
                        energyCost = 50, // High cost for powerful fusion abilities
                        cooldown = 30f,  // Long cooldown
                        isFusionAbility = true
                    };

                    fusion.fusionAbilities.Add(fusionAbility);

                    Debug.Log($"🌟 Fusion ability unlocked: {fusionAbility.abilityName}");
                    OnFusionAbilityUnlocked?.Invoke(fusionAbility);
                }
            }
        }

        private int CalculateFusionAbilityDamage(FusedUnit fusion, SynergyBonus synergy)
        {
            int baseDamage = (fusion.fusedStats.power + fusion.fusedStats.intelligence) / 2;

            // Bonus based on synergy level
            float multiplier = synergy.synergyLevel switch
            {
                SynergyLevel.Perfect => 3.0f,
                SynergyLevel.Legendary => 2.5f,
                SynergyLevel.Great => 2.0f,
                SynergyLevel.Good => 1.5f,
                _ => 1.0f
            };

            return (int)(baseDamage * multiplier);
        }

        #endregion

        #region Fusion Management

        public FusedUnit GetCurrentFusion() => _currentFusion;

        public void ClearFusion()
        {
            _currentFusion = null;
            Debug.Log("🔓 Fusion cleared");
        }

        /// <summary>
        /// Get all possible synergies for a robot-pet pair
        /// </summary>
        public List<SynergyBonus> GetPossibleSynergies(RobotData robot, PetData pet)
        {
            return _synergyDatabase.Where(s => CheckSynergyMatch(s, robot, pet)).ToList();
        }

        /// <summary>
        /// Get recommended pets for a robot
        /// </summary>
        public List<string> GetRecommendedPets(RobotData robot)
        {
            var recommendations = new List<string>();

            foreach (var synergy in _synergyDatabase)
            {
                if (!string.IsNullOrEmpty(synergy.robotType) && 
                    robot.characterInspiration == synergy.robotType)
                {
                    if (!recommendations.Contains(synergy.petType))
                        recommendations.Add(synergy.petType);
                }
            }

            return recommendations;
        }

        /// <summary>
        /// Get recommended robots for a pet
        /// </summary>
        public List<string> GetRecommendedRobots(PetData pet)
        {
            var recommendations = new List<string>();

            foreach (var synergy in _synergyDatabase)
            {
                if (!string.IsNullOrEmpty(synergy.petType) && 
                    pet.petType == synergy.petType)
                {
                    if (!recommendations.Contains(synergy.robotType))
                        recommendations.Add(synergy.robotType);
                }
            }

            return recommendations;
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class FusedUnit
    {
        public string fusionId;
        public string fusionName;
        public RobotData robot;
        public PetData pet;
        public FusedStats fusedStats;
        public List<FusedAbility> selectedAbilities;
        public List<FusionAbility> fusionAbilities;
        public List<SynergyBonus> activeSynergies;
        public long createdTimestamp;
    }

    [Serializable]
    public class FusedStats
    {
        public int power;
        public int speed;
        public int defense;
        public int intelligence;
        public int energy;
        public int precision;
        public int criticalChance;
        public int criticalDamage;
        public int evasion;
    }

    [Serializable]
    public class FusedAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public AbilitySource source;
        public int damage;
        public int energyCost;
        public float cooldown;
        public AbilityElement element;
    }

    [Serializable]
    public class FusionAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public string synergyRequired;
        public int damage;
        public int energyCost;
        public float cooldown;
        public bool isFusionAbility;
    }

    [Serializable]
    public enum AbilitySource
    {
        Robot,
        Pet,
        Fusion
    }

    [Serializable]
    public enum SynergyLevel
    {
        None = 0,
        Good = 1,       // ⭐
        Great = 2,      // ⭐⭐
        Perfect = 3,    // ⭐⭐⭐
        Legendary = 4   // 🌟🌟🌟🌟
    }

    [Serializable]
    public class SynergyBonus
    {
        public string synergyId;
        public string synergyName;
        public string description;
        public SynergyLevel synergyLevel;

        // Match criteria (any can match)
        public string robotType;
        public string petType;
        public AbilityElement robotElement;
        public string petElement;
        public FightingStyle robotStyle;
        public string petRole;

        // Bonuses
        public Dictionary<string, float> statBonuses;

        // Fusion ability unlock
        public string unlocksFusionAbility;
        public string fusionAbilityDescription;
    }

    #endregion

    #region Pet Data Structure

    /// <summary>
    /// Pet data structure for fusion
    /// </summary>
    [Serializable]
    public class PetData
    {
        public string petId;
        public string petName;
        public string petType;          // e.g., "naga_serpent", "champa_elephant"
        public string element;          // Water, Fire, Earth, etc.
        public string role;             // Healer, Scout, Buffer, etc.
        public PetStats stats;
        public List<PetAbility> abilities;
        public string description;

        public PetData()
        {
            petId = Guid.NewGuid().ToString();
            stats = new PetStats();
            abilities = new List<PetAbility>();
        }
    }

    [Serializable]
    public class PetStats
    {
        public int power;
        public int speed;
        public int defense;
        public int intelligence;
        public int energy;
        public int precision;

        public PetStats()
        {
            power = speed = defense = intelligence = energy = precision = 50;
        }
    }

    [Serializable]
    public class PetAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public int power;
        public int energyCost;
        public float cooldown;
        public AbilityElement element;
    }

    #endregion
}

