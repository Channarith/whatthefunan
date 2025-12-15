using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Data
{
    /// <summary>
    /// Database of all playable characters in What the Funan.
    /// Based on ancient Funan mythology and wildlife.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "What the Funan/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [Header("Playable Characters")]
        public List<CharacterData> characters = new List<CharacterData>();
        
        /// <summary>
        /// Get character by ID.
        /// </summary>
        public CharacterData GetCharacter(string characterId)
        {
            return characters.Find(c => c.characterId == characterId);
        }
        
        /// <summary>
        /// Get characters by class.
        /// </summary>
        public List<CharacterData> GetByClass(CharacterClass characterClass)
        {
            return characters.FindAll(c => c.characterClass == characterClass);
        }
        
        /// <summary>
        /// Get starter characters.
        /// </summary>
        public List<CharacterData> GetStarterCharacters()
        {
            return characters.FindAll(c => c.isStarter);
        }
    }

    public enum CharacterClass
    {
        Tank,       // High HP, defensive abilities
        Warrior,    // Balanced melee combat
        Scout,      // Fast, agile, recon
        Mage,       // Magic damage, elemental
        Support     // Healing, buffs
    }

    public enum CharacterElement
    {
        None,
        Water,      // Naga element - healing, flow
        Fire,       // Dragon element - damage, passion
        Earth,      // Elephant element - defense, stability
        Air,        // Garuda element - speed, freedom
        Spirit      // Apsara element - holy, celestial
    }

    [Serializable]
    public class CharacterData
    {
        [Header("Identity")]
        public string characterId;
        public string characterName;
        [TextArea] public string description;
        [TextArea] public string backstory;
        
        [Header("Visuals")]
        public Sprite portrait;
        public Sprite fullBodyArt;
        public GameObject prefab;
        public RuntimeAnimatorController animatorController;
        
        [Header("Classification")]
        public CharacterClass characterClass;
        public CharacterElement element;
        public CharacterRarity rarity;
        
        [Header("Base Stats")]
        public int baseHealth = 100;
        public int baseAttack = 10;
        public int baseDefense = 10;
        public int baseSpeed = 10;
        public int baseEnergy = 100;
        
        [Header("Growth Per Level")]
        public float healthGrowth = 10f;
        public float attackGrowth = 2f;
        public float defenseGrowth = 2f;
        public float speedGrowth = 1f;
        
        [Header("Abilities")]
        public List<AbilityData> abilities = new List<AbilityData>();
        
        [Header("Unlock")]
        public bool isStarter = false;
        public int unlockCost; // Gems
        public string unlockCondition;
        
        [Header("Voice")]
        public AudioClip[] voiceLines;
        public string voiceActorName;
        
        [Header("Cultural Note")]
        [TextArea] public string culturalSignificance;
    }

    public enum CharacterRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class AbilityData
    {
        public string abilityId;
        public string abilityName;
        [TextArea] public string description;
        public Sprite icon;
        
        public AbilityType type;
        public float damage;
        public float cooldown;
        public float energyCost;
        public float range;
        
        public int unlockLevel;
        
        public GameObject effectPrefab;
        public AudioClip soundEffect;
    }

    public enum AbilityType
    {
        Basic,
        Special,
        Ultimate,
        Passive
    }

    #region Default Characters
    /*
    ============================================================================
    WHAT THE FUNAN - CHARACTER ROSTER
    ============================================================================
    
    1. DOMREY - THE ELEPHANT WARRIOR (Tank)
    ----------------------------------------
    ID: "domrey"
    Class: Tank
    Element: Earth
    Rarity: Common (Starter)
    
    Description: A mighty elephant warrior from the royal guard of Funan.
    Domrey is known for his unwavering loyalty and incredible strength.
    
    Base Stats: HP 150, ATK 8, DEF 15, SPD 6
    
    Abilities:
    - Trunk Slam (Basic): Powerful melee attack
    - Earth Shield (Special): Reduce incoming damage for allies
    - Stampede (Ultimate): Charge forward, damaging all enemies
    - Thick Hide (Passive): +20% defense
    
    Cultural Note: Elephants were symbols of royal power in ancient 
    Southeast Asian kingdoms, used in war and ceremonies.
    
    ============================================================================
    
    2. SVAA - THE MONKEY SCOUT (Scout)
    -----------------------------------
    ID: "svaa"
    Class: Scout
    Element: Air
    Rarity: Common (Starter)
    
    Description: A mischievous but brave monkey who serves as a temple 
    guardian and messenger across the kingdom.
    
    Base Stats: HP 80, ATK 10, DEF 6, SPD 15
    
    Abilities:
    - Staff Strike (Basic): Quick melee combo
    - Acrobatic Dodge (Special): Evade and counterattack
    - Temple Leap (Ultimate): Jump to any location, stunning enemies
    - Nimble (Passive): +30% dodge chance
    
    Cultural Note: Monkeys appear frequently in Southeast Asian temple 
    carvings and are associated with the Ramayana epic.
    
    ============================================================================
    
    3. KRAHORM - THE PANDA MONK (Warrior/Support Hybrid)
    ----------------------------------------------------
    ID: "krahorm"
    Class: Warrior
    Element: Spirit
    Rarity: Rare
    
    Description: A wise red panda who has mastered both martial arts 
    and meditation at the mountain temples.
    
    Base Stats: HP 100, ATK 12, DEF 10, SPD 10
    
    Abilities:
    - Zen Strike (Basic): Balanced melee attack with heal
    - Inner Peace (Special): Heal self and nearby allies
    - Enlightenment (Ultimate): Massive AoE attack + team buff
    - Balance (Passive): Gain ATK when HP is low
    
    Cultural Note: Buddhist monks were important figures in Funan, 
    bringing teachings from India.
    
    ============================================================================
    
    4. NEAK - THE NAGA MAGE (Mage)
    -------------------------------
    ID: "neak"
    Class: Mage
    Element: Water
    Rarity: Epic
    
    Description: A mystical naga from the underwater kingdom beneath 
    the great rivers. Neak wields powerful water magic.
    
    Base Stats: HP 70, ATK 15, DEF 5, SPD 8
    
    Abilities:
    - Water Bolt (Basic): Ranged magic attack
    - Tidal Wave (Special): AoE water damage
    - Serpent's Blessing (Ultimate): Massive heal + water shield
    - Scales of Protection (Passive): Resist fire damage
    
    Cultural Note: Nagas are seven-headed serpent deities that guard 
    water and treasure in Hindu-Buddhist mythology.
    
    ============================================================================
    
    5. MEAS - THE APSARA DANCER (Support)
    --------------------------------------
    ID: "meas"
    Class: Support
    Element: Spirit
    Rarity: Epic
    
    Description: A celestial dancer blessed by the heavens. Her 
    graceful movements heal wounds and inspire allies.
    
    Base Stats: HP 90, ATK 5, DEF 8, SPD 12
    
    Abilities:
    - Blessing Dance (Basic): Heal lowest HP ally
    - Celestial Grace (Special): Team-wide buff
    - Divine Performance (Ultimate): Full heal + resurrection
    - Heavenly Aura (Passive): Allies near Meas regenerate HP
    
    Cultural Note: Apsaras are celestial dancers depicted extensively 
    in Angkorian temple carvings.
    
    ============================================================================
    
    6. REACHSEY - THE DRAGON PRINCE (Warrior)
    ------------------------------------------
    ID: "reachsey"
    Class: Warrior
    Element: Fire
    Rarity: Legendary
    
    Description: A young dragon from the Eastern mountains, seeking 
    to prove himself worthy of his noble lineage.
    
    Base Stats: HP 110, ATK 14, DEF 8, SPD 11
    
    Abilities:
    - Dragon Claw (Basic): Fiery melee strikes
    - Flame Breath (Special): Cone fire damage
    - Dragon's Wrath (Ultimate): Transform, massive damage boost
    - Dragon Blood (Passive): Immune to burn, heal from fire
    
    Cultural Note: Dragons in Southeast Asian culture represent 
    prosperity, power, and auspicious fortune.
    
    ============================================================================
    
    7. KROLA - THE STEGOSAURUS GUARDIAN (Tank)
    -------------------------------------------
    ID: "krola"
    Class: Tank
    Element: Earth
    Rarity: Legendary
    
    Description: A mysterious ancient creature awakened from stone 
    at Ta Prohm. Krola is bewildered by the modern world.
    
    Base Stats: HP 180, ATK 7, DEF 18, SPD 4
    
    Abilities:
    - Tail Spike (Basic): Strong counterattack
    - Stone Armor (Special): Become invulnerable briefly
    - Ancient Roar (Ultimate): Stun all enemies
    - Prehistoric (Passive): Cannot be knocked back
    
    Cultural Note: Inspired by the "stegosaurus" carving at Ta Prohm 
    temple - a playful tribute to this famous relief.
    
    ============================================================================
    */
    #endregion
}

