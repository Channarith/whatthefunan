using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT BUILDER SYSTEM! 🔧🤖
    /// Build, customize, and program your fighting robots!
    /// Inspired by Khmer mythology characters!
    /// </summary>
    public class RobotBuilder : MonoBehaviour
    {
        public static RobotBuilder Instance { get; private set; }

        [Header("Build Configuration")]
        [SerializeField] private int _maxStatPoints = 300;
        [SerializeField] private int _maxAbilities = 8;
        [SerializeField] private int _startingPartSlots = 10;

        [Header("Current Build")]
        [SerializeField] private RobotData _currentBuild;

        [Header("Parts Database")]
        [SerializeField] private RobotPartsDatabase _partsDatabase;

        [Header("Templates")]
        [SerializeField] private List<RobotTemplate> _characterTemplates;

        // Events
        public event Action<RobotData> OnRobotCreated;
        public event Action<RobotData> OnRobotModified;
        public event Action<string> OnBuildError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeTemplates();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeTemplates()
        {
            _characterTemplates = new List<RobotTemplate>
            {
                // ============================================================
                // KHMER MYTHOLOGY ROBOT TEMPLATES! 🇰🇭
                // ============================================================

                new RobotTemplate
                {
                    templateId = "naga_bot",
                    templateName = "🐍 Naga Bot",
                    description = "Serpentine robot inspired by the Naga Prince! Water element master with incredible flexibility!",
                    characterInspiration = "naga_prince",
                    chassisType = RobotChassisType.Serpentine,
                    stats = new RobotCoreStats { power = 40, speed = 70, defense = 50, intelligence = 80, energy = 60, precision = 60 },
                    primaryElement = AbilityElement.Water,
                    primaryStyle = FightingStyle.Technical,
                    specialAbilities = new[] { "Seven Head Strike", "Tidal Wave", "Serpent Coil", "Water Shield" },
                    uniqueTrait = "Can split attacks into 7 simultaneous strikes!"
                },

                new RobotTemplate
                {
                    templateId = "champa_bot",
                    templateName = "🐘 Champa Bot",
                    description = "Heavy quadruped robot inspired by the wise elephant Champa! Tank class with devastating charge attacks!",
                    characterInspiration = "champa",
                    chassisType = RobotChassisType.Quadruped,
                    stats = new RobotCoreStats { power = 90, speed = 30, defense = 85, intelligence = 60, energy = 80, precision = 40 },
                    primaryElement = AbilityElement.Earth,
                    primaryStyle = FightingStyle.Tank,
                    specialAbilities = new[] { "Trunk Slam", "Earthquake Stomp", "Charge Rush", "Ancient Wisdom" },
                    uniqueTrait = "Immune to knockback! Cannot be moved!"
                },

                new RobotTemplate
                {
                    templateId = "kavi_bot",
                    templateName = "🐒 Kavi Bot",
                    description = "Agile humanoid inspired by the mischievous monkey Kavi! Speed demon with unpredictable attacks!",
                    characterInspiration = "kavi",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 45, speed = 95, defense = 30, intelligence = 70, energy = 50, precision = 70 },
                    primaryElement = AbilityElement.Wind,
                    primaryStyle = FightingStyle.Aggressive,
                    specialAbilities = new[] { "Banana Bomb", "Monkey Flip", "Armpit Spray", "Laugh Attack" },
                    uniqueTrait = "Double jump and wall climb! Hyper mobility!"
                },

                new RobotTemplate
                {
                    templateId = "mealea_bot",
                    templateName = "💃 Mealea Bot",
                    description = "Graceful flying robot inspired by the celestial dancer Mealea! Celestial magic and elegant attacks!",
                    characterInspiration = "mealea",
                    chassisType = RobotChassisType.Flying,
                    stats = new RobotCoreStats { power = 50, speed = 75, defense = 40, intelligence = 85, energy = 70, precision = 80 },
                    primaryElement = AbilityElement.Celestial,
                    primaryStyle = FightingStyle.Evasive,
                    specialAbilities = new[] { "Celestial Dance", "Feather Storm", "Grace Shield", "Apsara's Blessing" },
                    uniqueTrait = "Permanent flight! Cannot be grounded!"
                },

                new RobotTemplate
                {
                    templateId = "makara_bot",
                    templateName = "🔥 Makara Bot",
                    description = "Fierce hybrid robot inspired by the legendary sea dragon Makara! Fire and water dual element!",
                    characterInspiration = "makara",
                    chassisType = RobotChassisType.Hybrid,
                    stats = new RobotCoreStats { power = 85, speed = 55, defense = 60, intelligence = 50, energy = 75, precision = 55 },
                    primaryElement = AbilityElement.Fire,
                    primaryStyle = FightingStyle.Berserker,
                    specialAbilities = new[] { "Fire Breath", "Tidal Crash", "Dragon Rage", "Steam Explosion" },
                    uniqueTrait = "Attacks gain power when HP is low!"
                },

                new RobotTemplate
                {
                    templateId = "garuda_bot",
                    templateName = "🦅 Garuda Bot",
                    description = "Majestic aerial robot inspired by the divine bird Garuda! Wind element king of the skies!",
                    characterInspiration = "garuda",
                    chassisType = RobotChassisType.Flying,
                    stats = new RobotCoreStats { power = 70, speed = 90, defense = 45, intelligence = 65, energy = 60, precision = 75 },
                    primaryElement = AbilityElement.Wind,
                    primaryStyle = FightingStyle.Assassin,
                    specialAbilities = new[] { "Divine Dive", "Talon Strike", "Wing Blade", "Tactical Bird Poop" },
                    uniqueTrait = "Aerial attacks deal 50% more damage!"
                },

                new RobotTemplate
                {
                    templateId = "sena_bot",
                    templateName = "🦁 Sena Bot",
                    description = "Powerful humanoid inspired by the guardian lion Sena! Pure strength and royal majesty!",
                    characterInspiration = "sena",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 95, speed = 50, defense = 70, intelligence = 45, energy = 65, precision = 50 },
                    primaryElement = AbilityElement.Fire,
                    primaryStyle = FightingStyle.Aggressive,
                    specialAbilities = new[] { "Royal Roar", "Claw Fury", "Legendary Hairball", "Guardian's Wrath" },
                    uniqueTrait = "Roar stuns all enemies in range!"
                },

                new RobotTemplate
                {
                    templateId = "hanuman_bot",
                    templateName = "⚔️ Hanuman Bot",
                    description = "Legendary warrior robot inspired by the divine monkey warrior Hanuman! Ultimate combat prowess!",
                    characterInspiration = "hanuman",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 80, speed = 80, defense = 55, intelligence = 60, energy = 55, precision = 65 },
                    primaryElement = AbilityElement.Physical,
                    primaryStyle = FightingStyle.Technical,
                    specialAbilities = new[] { "Giant Growth", "Legendary Wedgie", "Sun Punch", "Immortal Body" },
                    uniqueTrait = "Can grow to double size for ultimate attacks!"
                },

                new RobotTemplate
                {
                    templateId = "reahu_bot",
                    templateName = "🌑 Reahu Bot",
                    description = "Shadowy robot inspired by the eclipse demon Reahu! Master of darkness and illusions!",
                    characterInspiration = "reahu",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 60, speed = 65, defense = 50, intelligence = 90, energy = 70, precision = 85 },
                    primaryElement = AbilityElement.Shadow,
                    primaryStyle = FightingStyle.Tactical,
                    specialAbilities = new[] { "Eclipse Shroud", "Void Burp", "Shadow Clone", "Devour Light" },
                    uniqueTrait = "Becomes invisible when standing still!"
                },

                new RobotTemplate
                {
                    templateId = "prohm_bot",
                    templateName = "🦕 Prohm Bot",
                    description = "Ancient hexapod robot inspired by the primordial guardian Prohm! Unstoppable prehistoric power!",
                    characterInspiration = "prohm",
                    chassisType = RobotChassisType.Hexapod,
                    stats = new RobotCoreStats { power = 75, speed = 25, defense = 95, intelligence = 70, energy = 85, precision = 45 },
                    primaryElement = AbilityElement.Earth,
                    primaryStyle = FightingStyle.Tank,
                    specialAbilities = new[] { "Fossil Fury", "Prehistoric Funk", "Ancient Armor", "Time Stomp" },
                    uniqueTrait = "Regenerates health over time!"
                },

                new RobotTemplate
                {
                    templateId = "yak_bot",
                    templateName = "👹 Yak Bot",
                    description = "Towering giant robot inspired by the temple guardian Yak! Overwhelming size and power!",
                    characterInspiration = "yak",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 100, speed = 20, defense = 80, intelligence = 30, energy = 90, precision = 30 },
                    primaryElement = AbilityElement.Earth,
                    primaryStyle = FightingStyle.Tank,
                    specialAbilities = new[] { "Temple Smash", "Guardian Grab", "Overwhelming Hug", "Stone Skin" },
                    uniqueTrait = "Deals damage just by walking into enemies!"
                },

                new RobotTemplate
                {
                    templateId = "kinnari_bot",
                    templateName = "🎵 Kinnari Bot",
                    description = "Musical flying robot inspired by the singing bird-woman Kinnari! Sound-based attacks!",
                    characterInspiration = "kinnari",
                    chassisType = RobotChassisType.Flying,
                    stats = new RobotCoreStats { power = 55, speed = 70, defense = 35, intelligence = 80, energy = 65, precision = 90 },
                    primaryElement = AbilityElement.Celestial,
                    primaryStyle = FightingStyle.Support,
                    specialAbilities = new[] { "Sonic Screech", "Healing Melody", "Opera Bird Screech", "Harmony Shield" },
                    uniqueTrait = "Songs buff allies and debuff enemies!"
                },

                new RobotTemplate
                {
                    templateId = "ream_eyso_bot",
                    templateName = "⚡ Ream Eyso Bot",
                    description = "Thunder god robot inspired by the lightning deity Ream Eyso! Electric devastation!",
                    characterInspiration = "ream_eyso",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 85, speed = 60, defense = 50, intelligence = 75, energy = 80, precision = 70 },
                    primaryElement = AbilityElement.Lightning,
                    primaryStyle = FightingStyle.Aggressive,
                    specialAbilities = new[] { "Thunder Axe", "Lightning Storm", "Thunder Fart", "Chain Lightning" },
                    uniqueTrait = "Critical hits chain to nearby enemies!"
                },

                new RobotTemplate
                {
                    templateId = "moni_mekhala_bot",
                    templateName = "💎 Moni Mekhala Bot",
                    description = "Lightning goddess robot inspired by Moni Mekhala! Crystal and electric powers!",
                    characterInspiration = "moni_mekhala",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 70, speed = 75, defense = 45, intelligence = 90, energy = 75, precision = 85 },
                    primaryElement = AbilityElement.Lightning,
                    primaryStyle = FightingStyle.Evasive,
                    specialAbilities = new[] { "Crystal Flash", "Electric Hair Day", "Diamond Shield", "Lightning Dance" },
                    uniqueTrait = "Reflects projectile attacks!"
                },

                new RobotTemplate
                {
                    templateId = "thorani_bot",
                    templateName = "🌍 Thorani Bot",
                    description = "Earth mother robot inspired by Preah Thorani! Nature and healing powers!",
                    characterInspiration = "preah_thorani",
                    chassisType = RobotChassisType.Humanoid,
                    stats = new RobotCoreStats { power = 50, speed = 45, defense = 70, intelligence = 85, energy = 90, precision = 60 },
                    primaryElement = AbilityElement.Nature,
                    primaryStyle = FightingStyle.Support,
                    specialAbilities = new[] { "Flood of Justice", "Earth Embrace", "Mom Lecture", "Nature's Wrath" },
                    uniqueTrait = "Heals allies when taking damage!"
                }
            };
        }

        #region Robot Building

        /// <summary>
        /// Create a new robot from scratch
        /// </summary>
        public RobotData CreateNewRobot(string name, string creatorId)
        {
            _currentBuild = new RobotData
            {
                robotName = name,
                creatorId = creatorId
            };

            Debug.Log($"🤖 New robot created: {name}");
            return _currentBuild;
        }

        /// <summary>
        /// Create a robot from a template
        /// </summary>
        public RobotData CreateFromTemplate(string templateId, string name, string creatorId)
        {
            var template = _characterTemplates.Find(t => t.templateId == templateId);
            if (template == null)
            {
                OnBuildError?.Invoke($"Template not found: {templateId}");
                return null;
            }

            _currentBuild = new RobotData
            {
                robotName = name,
                creatorId = creatorId,
                characterInspiration = template.characterInspiration,
                coreStats = template.stats,
                physicalConfig = new RobotPhysicalConfig { chassisType = template.chassisType }
            };

            // Set AI from template
            _currentBuild.aiConfig.primaryStyle = template.primaryStyle;

            // Set elemental affinity
            SetPrimaryElement(template.primaryElement);

            // Add special abilities
            foreach (var abilityName in template.specialAbilities)
            {
                AddAbilityByName(abilityName);
            }

            _currentBuild.coreStats.RecalculateDerived();

            Debug.Log($"🤖 Robot created from template: {name} ({template.templateName})");
            OnRobotCreated?.Invoke(_currentBuild);

            return _currentBuild;
        }

        /// <summary>
        /// Set robot stats (validates max points)
        /// </summary>
        public bool SetStats(int power, int speed, int defense, int intelligence, int energy, int precision)
        {
            int total = power + speed + defense + intelligence + energy + precision;
            if (total > _maxStatPoints)
            {
                OnBuildError?.Invoke($"Total stats ({total}) exceeds maximum ({_maxStatPoints})!");
                return false;
            }

            _currentBuild.coreStats.power = Mathf.Clamp(power, 0, 100);
            _currentBuild.coreStats.speed = Mathf.Clamp(speed, 0, 100);
            _currentBuild.coreStats.defense = Mathf.Clamp(defense, 0, 100);
            _currentBuild.coreStats.intelligence = Mathf.Clamp(intelligence, 0, 100);
            _currentBuild.coreStats.energy = Mathf.Clamp(energy, 0, 100);
            _currentBuild.coreStats.precision = Mathf.Clamp(precision, 0, 100);
            _currentBuild.coreStats.RecalculateDerived();

            OnRobotModified?.Invoke(_currentBuild);
            return true;
        }

        /// <summary>
        /// Set chassis type
        /// </summary>
        public void SetChassisType(RobotChassisType chassisType)
        {
            _currentBuild.physicalConfig.chassisType = chassisType;

            // Update default limb configuration
            switch (chassisType)
            {
                case RobotChassisType.Humanoid:
                    _currentBuild.physicalConfig.armCount = 2;
                    _currentBuild.physicalConfig.legCount = 2;
                    break;
                case RobotChassisType.Quadruped:
                    _currentBuild.physicalConfig.armCount = 0;
                    _currentBuild.physicalConfig.legCount = 4;
                    break;
                case RobotChassisType.Hexapod:
                    _currentBuild.physicalConfig.armCount = 2;
                    _currentBuild.physicalConfig.legCount = 6;
                    break;
                case RobotChassisType.Serpentine:
                    _currentBuild.physicalConfig.armCount = 0;
                    _currentBuild.physicalConfig.legCount = 0;
                    _currentBuild.physicalConfig.hasTail = true;
                    break;
                case RobotChassisType.Flying:
                    _currentBuild.physicalConfig.armCount = 2;
                    _currentBuild.physicalConfig.legCount = 2;
                    _currentBuild.physicalConfig.hasWings = true;
                    break;
            }

            OnRobotModified?.Invoke(_currentBuild);
        }

        /// <summary>
        /// Set fighting style
        /// </summary>
        public void SetFightingStyle(FightingStyle primary, FightingStyle secondary)
        {
            _currentBuild.aiConfig.primaryStyle = primary;
            _currentBuild.aiConfig.secondaryStyle = secondary;
            OnRobotModified?.Invoke(_currentBuild);
        }

        /// <summary>
        /// Set AI behavior parameters
        /// </summary>
        public void SetAIBehavior(int aggression, int caution, int adaptability)
        {
            _currentBuild.aiConfig.aggression = Mathf.Clamp(aggression, 0, 100);
            _currentBuild.aiConfig.caution = Mathf.Clamp(caution, 0, 100);
            _currentBuild.aiConfig.adaptability = Mathf.Clamp(adaptability, 0, 100);
            OnRobotModified?.Invoke(_currentBuild);
        }

        /// <summary>
        /// Set primary elemental affinity
        /// </summary>
        public void SetPrimaryElement(AbilityElement element)
        {
            // Reset all to base
            var affinity = _currentBuild.combatStats.elementalAffinity;
            affinity.water = affinity.fire = affinity.earth = affinity.wind = 8;
            affinity.lightning = affinity.nature = affinity.celestial = affinity.shadow = 8;

            // Boost primary element
            switch (element)
            {
                case AbilityElement.Water: affinity.water = 36; break;
                case AbilityElement.Fire: affinity.fire = 36; break;
                case AbilityElement.Earth: affinity.earth = 36; break;
                case AbilityElement.Wind: affinity.wind = 36; break;
                case AbilityElement.Lightning: affinity.lightning = 36; break;
                case AbilityElement.Nature: affinity.nature = 36; break;
                case AbilityElement.Celestial: affinity.celestial = 36; break;
                case AbilityElement.Shadow: affinity.shadow = 36; break;
            }

            OnRobotModified?.Invoke(_currentBuild);
        }

        /// <summary>
        /// Add an ability to the robot
        /// </summary>
        public bool AddAbility(RobotAbility ability)
        {
            if (_currentBuild.abilities.Count >= _maxAbilities)
            {
                OnBuildError?.Invoke($"Maximum abilities ({_maxAbilities}) reached!");
                return false;
            }

            _currentBuild.abilities.Add(ability);
            OnRobotModified?.Invoke(_currentBuild);
            return true;
        }

        /// <summary>
        /// Add ability by name (from database)
        /// </summary>
        public bool AddAbilityByName(string abilityName)
        {
            var ability = CreateAbilityFromName(abilityName);
            if (ability != null)
            {
                return AddAbility(ability);
            }
            return false;
        }

        /// <summary>
        /// Set robot visual customization
        /// </summary>
        public void SetVisuals(string primaryColor, string secondaryColor, string accentColor, string materialType)
        {
            _currentBuild.visuals.primaryColor = primaryColor;
            _currentBuild.visuals.secondaryColor = secondaryColor;
            _currentBuild.visuals.accentColor = accentColor;
            _currentBuild.visuals.materialType = materialType;
            OnRobotModified?.Invoke(_currentBuild);
        }

        /// <summary>
        /// Enable Khmer-inspired decorations
        /// </summary>
        public void SetKhmerDecorations(bool nagaCrest, bool ankorPatterns, bool celestialGlow)
        {
            _currentBuild.visuals.hasNagaCrest = nagaCrest;
            _currentBuild.visuals.hasAnkorWatPatterns = ankorPatterns;
            _currentBuild.visuals.hasCelestialGlow = celestialGlow;
            OnRobotModified?.Invoke(_currentBuild);
        }

        #endregion

        #region Ability Factory

        private RobotAbility CreateAbilityFromName(string abilityName)
        {
            // This would normally come from a database
            // Here's a simplified version with some abilities
            return abilityName switch
            {
                // Naga abilities
                "Seven Head Strike" => new RobotAbility
                {
                    abilityId = "seven_head_strike",
                    abilityName = "Seven Head Strike",
                    description = "Strike with 7 simultaneous attacks!",
                    type = AbilityType.Attack,
                    element = AbilityElement.Water,
                    baseDamage = 70,
                    energyCost = 30,
                    cooldown = 8f
                },
                "Tidal Wave" => new RobotAbility
                {
                    abilityId = "tidal_wave",
                    abilityName = "Tidal Wave",
                    description = "Summon a wave that pushes enemies back!",
                    type = AbilityType.Attack,
                    element = AbilityElement.Water,
                    baseDamage = 50,
                    energyCost = 25,
                    cooldown = 6f,
                    areaOfEffect = 5f
                },

                // Champa abilities
                "Trunk Slam" => new RobotAbility
                {
                    abilityId = "trunk_slam",
                    abilityName = "Trunk Slam",
                    description = "Devastating trunk slam attack!",
                    type = AbilityType.Attack,
                    element = AbilityElement.Physical,
                    baseDamage = 100,
                    energyCost = 20,
                    cooldown = 5f
                },
                "Earthquake Stomp" => new RobotAbility
                {
                    abilityId = "earthquake_stomp",
                    abilityName = "Earthquake Stomp",
                    description = "Stomp the ground, stunning nearby enemies!",
                    type = AbilityType.Attack,
                    element = AbilityElement.Earth,
                    baseDamage = 60,
                    energyCost = 35,
                    cooldown = 10f,
                    areaOfEffect = 8f
                },

                // Add more abilities as needed...
                _ => new RobotAbility
                {
                    abilityId = abilityName.ToLower().Replace(" ", "_"),
                    abilityName = abilityName,
                    description = $"The {abilityName} attack!",
                    type = AbilityType.Attack,
                    element = AbilityElement.Physical,
                    baseDamage = 50,
                    energyCost = 20,
                    cooldown = 5f
                }
            };
        }

        #endregion

        #region Save/Load

        public RobotData GetCurrentBuild() => _currentBuild;

        public void LoadBuild(RobotData data)
        {
            _currentBuild = data;
            Debug.Log($"🤖 Loaded robot: {data.robotName}");
        }

        public List<RobotTemplate> GetTemplates() => _characterTemplates;

        #endregion
    }

    /// <summary>
    /// Robot Template - Preset configurations based on Khmer characters
    /// </summary>
    [Serializable]
    public class RobotTemplate
    {
        public string templateId;
        public string templateName;
        public string description;
        public string characterInspiration;
        public RobotChassisType chassisType;
        public RobotCoreStats stats;
        public AbilityElement primaryElement;
        public FightingStyle primaryStyle;
        public string[] specialAbilities;
        public string uniqueTrait;
        public Sprite previewImage;
    }
}

