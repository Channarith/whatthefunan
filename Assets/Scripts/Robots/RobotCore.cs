using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT CORE DATA STRUCTURES! 🤖
    /// Designed for both in-game AND real-world robot integration!
    /// All data is serializable for Bluetooth/WiFi transfer to physical robots!
    /// </summary>
    
    #region Robot Data (Exportable to Real Robots)

    /// <summary>
    /// The main robot data container - designed to be exported to real robots!
    /// Uses simple data types for universal compatibility.
    /// </summary>
    [Serializable]
    public class RobotData
    {
        // ============================================================
        // IDENTIFICATION (Used by both game and real robots)
        // ============================================================
        public string robotId;           // Unique identifier (UUID)
        public string robotName;         // Display name
        public string creatorId;         // Player who built it
        public long createdTimestamp;    // Unix timestamp
        public int version;              // Data version for compatibility
        public string characterInspiration; // Khmer character this robot is based on

        // ============================================================
        // CORE STATS (0-100 scale for universal compatibility)
        // ============================================================
        public RobotCoreStats coreStats;

        // ============================================================
        // COMBAT ATTRIBUTES
        // ============================================================
        public RobotCombatStats combatStats;

        // ============================================================
        // AI & STRATEGY
        // ============================================================
        public RobotAIConfig aiConfig;

        // ============================================================
        // PHYSICAL CONFIGURATION (For real robots)
        // ============================================================
        public RobotPhysicalConfig physicalConfig;

        // ============================================================
        // ABILITIES & MOVES
        // ============================================================
        public List<RobotAbility> abilities;

        // ============================================================
        // VISUAL CUSTOMIZATION
        // ============================================================
        public RobotVisuals visuals;

        // ============================================================
        // BATTLE HISTORY & LEARNING
        // ============================================================
        public RobotBattleHistory battleHistory;

        public RobotData()
        {
            robotId = Guid.NewGuid().ToString();
            createdTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            version = 1;
            coreStats = new RobotCoreStats();
            combatStats = new RobotCombatStats();
            aiConfig = new RobotAIConfig();
            physicalConfig = new RobotPhysicalConfig();
            abilities = new List<RobotAbility>();
            visuals = new RobotVisuals();
            battleHistory = new RobotBattleHistory();
        }
    }

    /// <summary>
    /// Core stats - the foundation of every robot
    /// All values 0-100 for easy real-world motor/servo mapping
    /// </summary>
    [Serializable]
    public class RobotCoreStats
    {
        // Primary Stats
        public int power;           // 0-100: Physical strength, motor power
        public int speed;           // 0-100: Movement speed, servo response time
        public int defense;         // 0-100: Damage resistance, armor
        public int intelligence;    // 0-100: AI complexity, decision making
        public int energy;          // 0-100: Battery/power capacity
        public int precision;       // 0-100: Attack accuracy, sensor precision

        // Derived Stats (calculated)
        public int healthPoints;    // Calculated from defense + power
        public int energyCapacity;  // Calculated from energy stat
        public int reactionTime;    // Calculated from speed + intelligence (ms)

        public RobotCoreStats()
        {
            power = 50;
            speed = 50;
            defense = 50;
            intelligence = 50;
            energy = 50;
            precision = 50;
            RecalculateDerived();
        }

        public void RecalculateDerived()
        {
            healthPoints = (defense * 10) + (power * 5);
            energyCapacity = energy * 10;
            reactionTime = Mathf.Max(50, 500 - (speed * 3) - (intelligence * 2)); // 50-500ms
        }

        public int GetTotalStatPoints()
        {
            return power + speed + defense + intelligence + energy + precision;
        }
    }

    /// <summary>
    /// Combat-specific stats and configurations
    /// </summary>
    [Serializable]
    public class RobotCombatStats
    {
        // Attack Types (specialization percentages, total = 100)
        public int meleeAffinity;       // Close combat effectiveness
        public int rangedAffinity;      // Ranged attack effectiveness
        public int magicAffinity;       // Special/magic attack effectiveness

        // Defense Types
        public int physicalArmor;       // % damage reduction from physical
        public int energyShielding;     // % damage reduction from energy/magic
        public int evasionChance;       // % chance to dodge attacks

        // Special Combat Modifiers
        public int criticalChance;      // % chance for critical hit
        public int criticalDamage;      // % bonus damage on critical
        public int counterChance;       // % chance to counter after block
        public int comboBonus;          // % bonus damage for combo attacks

        // Elemental Affinities (Khmer mythological elements)
        public ElementalAffinity elementalAffinity;

        public RobotCombatStats()
        {
            meleeAffinity = 34;
            rangedAffinity = 33;
            magicAffinity = 33;
            physicalArmor = 20;
            energyShielding = 20;
            evasionChance = 10;
            criticalChance = 5;
            criticalDamage = 150;
            counterChance = 10;
            comboBonus = 10;
            elementalAffinity = new ElementalAffinity();
        }
    }

    /// <summary>
    /// Elemental affinities based on Khmer mythology
    /// </summary>
    [Serializable]
    public class ElementalAffinity
    {
        public int water;       // Naga element - fluidity, adaptability
        public int fire;        // Makara element - power, destruction
        public int earth;       // Prohm element - stability, defense
        public int wind;        // Garuda element - speed, evasion
        public int lightning;   // Ream Eyso element - burst damage
        public int nature;      // Forest spirits - healing, growth
        public int celestial;   // Apsara element - magic, grace
        public int shadow;      // Reahu element - stealth, trickery

        public ElementalAffinity()
        {
            water = fire = earth = wind = 12;
            lightning = nature = celestial = shadow = 13;
        }

        public string GetDominantElement()
        {
            int max = Mathf.Max(water, fire, earth, wind, lightning, nature, celestial, shadow);
            if (max == water) return "Water";
            if (max == fire) return "Fire";
            if (max == earth) return "Earth";
            if (max == wind) return "Wind";
            if (max == lightning) return "Lightning";
            if (max == nature) return "Nature";
            if (max == celestial) return "Celestial";
            return "Shadow";
        }
    }

    /// <summary>
    /// AI Configuration - The brain of the robot
    /// This is what makes robots intelligent and can be transferred to real robots!
    /// </summary>
    [Serializable]
    public class RobotAIConfig
    {
        // Fighting Style
        public FightingStyle primaryStyle;
        public FightingStyle secondaryStyle;

        // Behavior Parameters (0-100)
        public int aggression;          // How offensive vs defensive
        public int caution;             // Risk assessment threshold
        public int adaptability;        // How quickly it changes tactics
        public int patternRecognition;  // Ability to predict opponent moves
        public int memoryRetention;     // How long it remembers opponent patterns

        // Decision Making
        public DecisionWeights decisionWeights;

        // Combat Preferences
        public CombatPreferences preferences;

        // Learning Configuration
        public LearningConfig learningConfig;

        public RobotAIConfig()
        {
            primaryStyle = FightingStyle.Balanced;
            secondaryStyle = FightingStyle.Defensive;
            aggression = 50;
            caution = 50;
            adaptability = 50;
            patternRecognition = 50;
            memoryRetention = 50;
            decisionWeights = new DecisionWeights();
            preferences = new CombatPreferences();
            learningConfig = new LearningConfig();
        }
    }

    [Serializable]
    public enum FightingStyle
    {
        Aggressive,     // Rush down, high damage, low defense
        Defensive,      // Wait and counter, high defense
        Balanced,       // Mix of offense and defense
        Technical,      // Combo-focused, precision attacks
        Berserker,      // High risk, high reward
        Tactical,       // Exploit weaknesses, adapt
        Evasive,        // Dodge and weave, hit and run
        Tank,           // Absorb damage, slow but powerful
        Assassin,       // Quick kills, low HP
        Support         // Buff self, debuff enemy
    }

    [Serializable]
    public class DecisionWeights
    {
        // What the AI considers when making decisions
        public float healthWeight;          // How much current HP affects decisions
        public float energyWeight;          // How much current energy affects decisions
        public float distanceWeight;        // How much distance affects decisions
        public float threatWeight;          // How much enemy threat affects decisions
        public float opportunityWeight;     // How much openings affect decisions
        public float timingWeight;          // How much timing affects decisions

        public DecisionWeights()
        {
            healthWeight = 1.0f;
            energyWeight = 1.0f;
            distanceWeight = 1.0f;
            threatWeight = 1.0f;
            opportunityWeight = 1.0f;
            timingWeight = 1.0f;
        }
    }

    [Serializable]
    public class CombatPreferences
    {
        public float preferredDistance;     // Optimal combat distance (0=melee, 100=far)
        public float comboChaining;         // Tendency to chain combos (0-1)
        public float specialMoveUsage;      // How often to use special moves (0-1)
        public float blockFrequency;        // How often to block vs dodge (0-1)
        public float feintUsage;            // How often to feint/fake (0-1)
        public float grabUsage;             // How often to use grabs (0-1)

        public CombatPreferences()
        {
            preferredDistance = 30f;
            comboChaining = 0.5f;
            specialMoveUsage = 0.3f;
            blockFrequency = 0.5f;
            feintUsage = 0.2f;
            grabUsage = 0.2f;
        }
    }

    [Serializable]
    public class LearningConfig
    {
        public bool enableLearning;         // Does the robot learn from battles?
        public float learningRate;          // How fast it adapts (0-1)
        public int maxPatternMemory;        // How many patterns to remember
        public bool persistLearning;        // Save learning between sessions

        public LearningConfig()
        {
            enableLearning = true;
            learningRate = 0.1f;
            maxPatternMemory = 100;
            persistLearning = true;
        }
    }

    /// <summary>
    /// Physical Configuration - For real robot hardware mapping!
    /// </summary>
    [Serializable]
    public class RobotPhysicalConfig
    {
        // Robot Type
        public RobotChassisType chassisType;
        public RobotSizeClass sizeClass;

        // Limb Configuration
        public int armCount;            // Number of arms (0-4)
        public int legCount;            // Number of legs (0-6, 0=wheels/tracks)
        public bool hasWings;
        public bool hasTail;

        // Motor/Servo Configuration (for real robots)
        public List<ServoConfig> servos;
        public List<MotorConfig> motors;

        // Sensor Configuration (for real robots)
        public List<SensorConfig> sensors;

        // Weapon Mounts
        public List<WeaponMount> weaponMounts;

        // Physical Dimensions (mm for real robot compatibility)
        public float heightMM;
        public float widthMM;
        public float depthMM;
        public float weightGrams;

        public RobotPhysicalConfig()
        {
            chassisType = RobotChassisType.Humanoid;
            sizeClass = RobotSizeClass.Medium;
            armCount = 2;
            legCount = 2;
            hasWings = false;
            hasTail = false;
            servos = new List<ServoConfig>();
            motors = new List<MotorConfig>();
            sensors = new List<SensorConfig>();
            weaponMounts = new List<WeaponMount>();
            heightMM = 300f;
            widthMM = 200f;
            depthMM = 150f;
            weightGrams = 1000f;
        }
    }

    [Serializable]
    public enum RobotChassisType
    {
        Humanoid,       // 2 arms, 2 legs (human-like)
        Quadruped,      // 4 legs (animal-like)
        Hexapod,        // 6 legs (insect-like)
        Wheeled,        // Wheels for mobility
        Tracked,        // Tank tracks
        Serpentine,     // Snake-like (Naga inspired!)
        Flying,         // Wings/propellers (Garuda inspired!)
        Hybrid          // Mix of types
    }

    [Serializable]
    public enum RobotSizeClass
    {
        Tiny,           // < 100mm
        Small,          // 100-200mm
        Medium,         // 200-400mm
        Large,          // 400-600mm
        Giant           // > 600mm
    }

    [Serializable]
    public class ServoConfig
    {
        public string servoId;
        public string jointName;        // e.g., "left_shoulder", "right_elbow"
        public int minAngle;            // Minimum rotation (degrees)
        public int maxAngle;            // Maximum rotation (degrees)
        public int defaultAngle;        // Default/rest position
        public int speed;               // Rotation speed (degrees/second)
        public int torque;              // Torque rating
    }

    [Serializable]
    public class MotorConfig
    {
        public string motorId;
        public string motorName;
        public int maxRPM;
        public int power;               // Power rating
        public bool reversible;
    }

    [Serializable]
    public class SensorConfig
    {
        public string sensorId;
        public SensorType sensorType;
        public string mountPosition;    // Where on robot
        public float range;             // Detection range
        public float accuracy;          // Accuracy percentage
    }

    [Serializable]
    public enum SensorType
    {
        Infrared,           // Proximity detection
        Ultrasonic,         // Distance measurement
        Camera,             // Vision
        Accelerometer,      // Motion detection
        Gyroscope,          // Orientation
        Touch,              // Contact sensors
        Microphone,         // Sound detection
        LiDAR               // 3D mapping
    }

    [Serializable]
    public class WeaponMount
    {
        public string mountId;
        public string mountPosition;    // "left_arm", "right_arm", "back", etc.
        public WeaponType weaponType;
        public string weaponName;
        public int damage;
        public int range;
        public float cooldown;
    }

    [Serializable]
    public enum WeaponType
    {
        // Melee
        Fist,
        Sword,
        Hammer,
        Spear,
        Claws,

        // Ranged
        Launcher,
        Cannon,
        BeamEmitter,

        // Special
        Shield,
        Grabber,
        Flamethrower,
        ElectricDischarge,
        SonicBlaster
    }

    /// <summary>
    /// Robot Abilities - Special moves and attacks
    /// </summary>
    [Serializable]
    public class RobotAbility
    {
        public string abilityId;
        public string abilityName;
        public string description;
        public AbilityType type;
        public AbilityElement element;

        // Costs
        public int energyCost;
        public float cooldown;

        // Effects
        public int baseDamage;
        public float range;
        public float areaOfEffect;
        public List<AbilityEffect> effects;

        // Animation/Action Data (for both game and real robots)
        public List<ActionSequence> actionSequence;

        // Unlock Requirements
        public int unlockLevel;
        public int unlockCost;
    }

    [Serializable]
    public enum AbilityType
    {
        Attack,
        Defense,
        Buff,
        Debuff,
        Heal,
        Movement,
        Ultimate
    }

    [Serializable]
    public enum AbilityElement
    {
        Physical,
        Water,
        Fire,
        Earth,
        Wind,
        Lightning,
        Nature,
        Celestial,
        Shadow
    }

    [Serializable]
    public class AbilityEffect
    {
        public EffectType effectType;
        public float value;
        public float duration;
        public float chance;
    }

    [Serializable]
    public enum EffectType
    {
        Damage,
        Heal,
        Stun,
        Slow,
        Burn,
        Freeze,
        Poison,
        Blind,
        Knockback,
        Pull,
        AttackUp,
        DefenseUp,
        SpeedUp,
        AttackDown,
        DefenseDown,
        SpeedDown,
        Shield,
        Reflect,
        LifeSteal,
        EnergyDrain
    }

    /// <summary>
    /// Action Sequence - Exact movements for real robot execution!
    /// This is the key to transferring game actions to real robots!
    /// </summary>
    [Serializable]
    public class ActionSequence
    {
        public int stepNumber;
        public float duration;              // Duration in seconds
        public List<ServoMovement> servoMovements;
        public List<MotorCommand> motorCommands;
        public string soundEffect;
        public string visualEffect;
    }

    [Serializable]
    public class ServoMovement
    {
        public string servoId;
        public int targetAngle;
        public int speed;
        public EasingType easing;
    }

    [Serializable]
    public class MotorCommand
    {
        public string motorId;
        public int speed;               // -100 to 100 (negative = reverse)
        public float duration;
    }

    [Serializable]
    public enum EasingType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Bounce,
        Elastic
    }

    /// <summary>
    /// Robot Visuals - How the robot looks in-game
    /// </summary>
    [Serializable]
    public class RobotVisuals
    {
        public string bodyMeshId;
        public string headMeshId;
        public string[] armMeshIds;
        public string[] legMeshIds;

        // Colors (RGB hex for universality)
        public string primaryColor;
        public string secondaryColor;
        public string accentColor;
        public string glowColor;

        // Materials/Textures
        public string materialType;     // "metal", "crystal", "ancient", etc.
        public float glossiness;
        public float emissiveIntensity;

        // Accessories
        public List<string> accessoryIds;

        // Khmer-inspired decorations
        public bool hasNagaCrest;
        public bool hasAnkorWatPatterns;
        public bool hasCelestialGlow;

        public RobotVisuals()
        {
            primaryColor = "#C0C0C0";
            secondaryColor = "#FFD700";
            accentColor = "#FF4500";
            glowColor = "#00FFFF";
            materialType = "metal";
            glossiness = 0.7f;
            emissiveIntensity = 0.3f;
            accessoryIds = new List<string>();
        }
    }

    /// <summary>
    /// Battle History - Learning and statistics
    /// </summary>
    [Serializable]
    public class RobotBattleHistory
    {
        public int totalBattles;
        public int wins;
        public int losses;
        public int draws;

        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalKnockouts;

        public float averageBattleDuration;
        public int longestWinStreak;
        public int currentWinStreak;

        // Learning data
        public List<LearnedPattern> learnedPatterns;

        // Ranking
        public int rankingPoints;
        public string rankTier;

        public RobotBattleHistory()
        {
            learnedPatterns = new List<LearnedPattern>();
            rankTier = "Bronze";
        }

        public float GetWinRate()
        {
            if (totalBattles == 0) return 0f;
            return (float)wins / totalBattles * 100f;
        }
    }

    [Serializable]
    public class LearnedPattern
    {
        public string patternId;
        public string description;
        public string triggerCondition;     // When to use this pattern
        public string response;             // What to do
        public float confidence;            // How confident the AI is (0-1)
        public int timesUsed;
        public int timesSuccessful;
    }

    #endregion
}

