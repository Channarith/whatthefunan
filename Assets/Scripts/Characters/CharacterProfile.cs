using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Characters
{
    /// <summary>
    /// Complete character profile containing all data for a playable guardian.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "What the Funan/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        #region Identity
        [Header("Identity")]
        [SerializeField] private string _characterId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _title;
        [SerializeField] private string _species;
        [TextArea(3, 6)]
        [SerializeField] private string _shortDescription;
        [TextArea(5, 10)]
        [SerializeField] private string _fullBiography;
        [SerializeField] private CharacterRole _role;
        [SerializeField] private CharacterElement _element;
        [SerializeField] private CharacterRarity _rarity;
        
        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public string Title => _title;
        public string Species => _species;
        public string ShortDescription => _shortDescription;
        public string FullBiography => _fullBiography;
        public CharacterRole Role => _role;
        public CharacterElement Element => _element;
        public CharacterRarity Rarity => _rarity;
        #endregion

        #region Visuals
        [Header("Visuals")]
        [SerializeField] private Sprite _portrait;
        [SerializeField] private Sprite _fullBodyArt;
        [SerializeField] private Sprite _chibiIcon;
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private Color _themeColor;
        [SerializeField] private Color _accentColor;
        
        public Sprite Portrait => _portrait;
        public Sprite FullBodyArt => _fullBodyArt;
        public Sprite ChibiIcon => _chibiIcon;
        public GameObject ModelPrefab => _modelPrefab;
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public Color ThemeColor => _themeColor;
        public Color AccentColor => _accentColor;
        #endregion

        #region Base Stats
        [Header("Base Stats (Level 1)")]
        [SerializeField] private int _baseHealth = 100;
        [SerializeField] private int _baseAttack = 20;
        [SerializeField] private int _baseDefense = 15;
        [SerializeField] private int _baseSpeed = 20;
        [SerializeField] private int _baseStamina = 100;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private float _moveSpeed = 5f;
        
        [Header("Stat Growth Per Level")]
        [SerializeField] private float _healthGrowth = 12f;
        [SerializeField] private float _attackGrowth = 3f;
        [SerializeField] private float _defenseGrowth = 2f;
        [SerializeField] private float _speedGrowth = 1f;
        
        public int BaseHealth => _baseHealth;
        public int BaseAttack => _baseAttack;
        public int BaseDefense => _baseDefense;
        public int BaseSpeed => _baseSpeed;
        public int BaseStamina => _baseStamina;
        public float AttackRange => _attackRange;
        public float MoveSpeed => _moveSpeed;
        #endregion

        #region Abilities
        [Header("Abilities")]
        [SerializeField] private AbilityData _basicAttack;
        [SerializeField] private AbilityData _specialAbility;
        [SerializeField] private AbilityData _ultimateAbility;
        [SerializeField] private AbilityData _passiveAbility;
        [SerializeField] private List<AbilityData> _comboMoves = new List<AbilityData>();
        
        public AbilityData BasicAttack => _basicAttack;
        public AbilityData SpecialAbility => _specialAbility;
        public AbilityData UltimateAbility => _ultimateAbility;
        public AbilityData PassiveAbility => _passiveAbility;
        public List<AbilityData> ComboMoves => _comboMoves;
        #endregion

        #region Personality
        [Header("Personality")]
        [SerializeField] private PersonalityTraits _personality;
        [SerializeField] private List<string> _likes = new List<string>();
        [SerializeField] private List<string> _dislikes = new List<string>();
        [SerializeField] private string _favoriteFood;
        [SerializeField] private string _hobby;
        [SerializeField] private string _fear;
        [SerializeField] private string _dream;
        
        public PersonalityTraits Personality => _personality;
        public List<string> Likes => _likes;
        public List<string> Dislikes => _dislikes;
        public string FavoriteFood => _favoriteFood;
        public string Hobby => _hobby;
        public string Fear => _fear;
        public string Dream => _dream;
        #endregion

        #region Voice Lines
        [Header("Voice Lines")]
        [SerializeField] private VoiceLineCollection _voiceLines;
        
        public VoiceLineCollection VoiceLines => _voiceLines;
        #endregion

        #region Relationships
        [Header("Relationships")]
        [SerializeField] private List<CharacterRelationship> _relationships = new List<CharacterRelationship>();
        
        public List<CharacterRelationship> Relationships => _relationships;
        #endregion

        #region Backstory
        [Header("Backstory")]
        [SerializeField] private string _birthplace;
        [SerializeField] private string _occupation;
        [SerializeField] private string _familyBackground;
        [TextArea(5, 10)]
        [SerializeField] private string _backstory;
        [SerializeField] private List<string> _keyMemories = new List<string>();
        
        public string Birthplace => _birthplace;
        public string Occupation => _occupation;
        public string FamilyBackground => _familyBackground;
        public string Backstory => _backstory;
        public List<string> KeyMemories => _keyMemories;
        #endregion

        #region Unlock
        [Header("Unlock Requirements")]
        [SerializeField] private UnlockMethod _unlockMethod;
        [SerializeField] private string _unlockQuestId;
        [SerializeField] private int _unlockCoinCost;
        [SerializeField] private int _unlockGemCost;
        [SerializeField] private int _unlockLevel;
        [SerializeField] private bool _isStarterCharacter;
        
        public UnlockMethod UnlockMethod => _unlockMethod;
        public string UnlockQuestId => _unlockQuestId;
        public int UnlockCoinCost => _unlockCoinCost;
        public int UnlockGemCost => _unlockGemCost;
        public int UnlockLevel => _unlockLevel;
        public bool IsStarterCharacter => _isStarterCharacter;
        #endregion

        #region Costumes
        [Header("Costumes")]
        [SerializeField] private List<CostumeData> _costumes = new List<CostumeData>();
        
        public List<CostumeData> Costumes => _costumes;
        #endregion

        #region Emotes
        [Header("Emotes")]
        [SerializeField] private List<EmoteData> _emotes = new List<EmoteData>();
        
        public List<EmoteData> Emotes => _emotes;
        #endregion

        #region Methods
        /// <summary>
        /// Calculate stats at a given level.
        /// </summary>
        public CharacterStats GetStatsAtLevel(int level)
        {
            return new CharacterStats
            {
                health = Mathf.RoundToInt(_baseHealth + (_healthGrowth * (level - 1))),
                attack = Mathf.RoundToInt(_baseAttack + (_attackGrowth * (level - 1))),
                defense = Mathf.RoundToInt(_baseDefense + (_defenseGrowth * (level - 1))),
                speed = Mathf.RoundToInt(_baseSpeed + (_speedGrowth * (level - 1))),
                stamina = _baseStamina
            };
        }

        /// <summary>
        /// Get a random voice line of a specific type.
        /// </summary>
        public string GetVoiceLine(VoiceLineType type)
        {
            return _voiceLines?.GetRandomLine(type) ?? "";
        }

        /// <summary>
        /// Get relationship with another character.
        /// </summary>
        public CharacterRelationship GetRelationship(string otherCharacterId)
        {
            return _relationships.Find(r => r.characterId == otherCharacterId);
        }
        #endregion
    }

    #region Enums
    public enum CharacterRole
    {
        Tank,           // High HP, protects allies
        DPS,            // High damage dealer
        Support,        // Heals and buffs
        Speedster,      // Fast, combo-based
        AllRounder      // Balanced stats
    }

    public enum CharacterElement
    {
        None,
        Water,          // Naga
        Fire,           // Makara
        Earth,          // Prohm, Elephant
        Wind,           // Monkey
        Light,          // Apsara
        Shadow          // Boss element
    }

    public enum CharacterRarity
    {
        Common,         // Starter
        Rare,           // Quest unlock
        Epic,           // Special unlock
        Legendary       // Premium/Event
    }

    public enum UnlockMethod
    {
        Starter,        // Available from start
        Quest,          // Complete specific quest
        Purchase,       // Buy with coins/gems
        Achievement,    // Unlock achievement
        Event,          // Limited time event
        Gacha           // Random from pulls
    }

    public enum VoiceLineType
    {
        Greeting,
        Selection,
        BattleStart,
        Attack,
        SpecialAttack,
        Ultimate,
        Hurt,
        LowHealth,
        Victory,
        Defeat,
        LevelUp,
        Idle,
        InteractFriendly,
        InteractRival,
        ItemPickup,
        QuestComplete,
        Funny
    }
    #endregion

    #region Data Classes
    [Serializable]
    public class CharacterStats
    {
        public int health;
        public int attack;
        public int defense;
        public int speed;
        public int stamina;
    }

    [Serializable]
    public class PersonalityTraits
    {
        [Range(0, 100)] public int bravery = 50;
        [Range(0, 100)] public int kindness = 50;
        [Range(0, 100)] public int wisdom = 50;
        [Range(0, 100)] public int humor = 50;
        [Range(0, 100)] public int loyalty = 50;
        [Range(0, 100)] public int curiosity = 50;
    }

    [Serializable]
    public class AbilityData
    {
        public string abilityId;
        public string abilityName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        public float damage;
        public float cooldown;
        public float staminaCost;
        public float range;
        public AbilityType type;
        public CharacterElement element;
        public List<string> statusEffects;
        public AnimationClip animation;
        public GameObject vfxPrefab;
        public AudioClip sfx;
    }

    public enum AbilityType
    {
        Melee,
        Ranged,
        AoE,
        Buff,
        Heal,
        Debuff
    }

    [Serializable]
    public class CharacterRelationship
    {
        public string characterId;
        public string characterName;
        public RelationshipType type;
        [Range(-100, 100)] public int affinity;
        [TextArea(2, 4)]
        public string relationshipDescription;
        public List<string> specialDialogue;
    }

    public enum RelationshipType
    {
        Friend,
        BestFriend,
        Rival,
        Mentor,
        Student,
        Family,
        RomanticInterest, // For older audiences
        Acquaintance
    }

    [Serializable]
    public class VoiceLineCollection
    {
        public List<VoiceLineEntry> lines = new List<VoiceLineEntry>();
        
        public string GetRandomLine(VoiceLineType type)
        {
            var matching = lines.FindAll(l => l.type == type);
            if (matching.Count == 0) return "";
            return matching[UnityEngine.Random.Range(0, matching.Count)].text;
        }
    }

    [Serializable]
    public class VoiceLineEntry
    {
        public VoiceLineType type;
        [TextArea(1, 3)]
        public string text;
        public AudioClip audioClip;
    }

    [Serializable]
    public class CostumeData
    {
        public string costumeId;
        public string costumeName;
        [TextArea(2, 3)]
        public string description;
        public Sprite preview;
        public Material[] materials;
        public GameObject meshOverride;
        public CostumeRarity rarity;
        public UnlockMethod unlockMethod;
        public int unlockCost;
        public bool isDefault;
    }

    public enum CostumeRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class EmoteData
    {
        public string emoteId;
        public string emoteName;
        public Sprite icon;
        public AnimationClip animation;
        public AudioClip sound;
        public bool isDefault;
    }
    #endregion
}

