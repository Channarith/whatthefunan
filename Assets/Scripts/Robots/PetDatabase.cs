using UnityEngine;
using System.Collections.Generic;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// PET DATABASE! 🐾
    /// All pets available to pair with robots!
    /// Based on Khmer mythology creatures!
    /// </summary>
    public class PetDatabase : MonoBehaviour
    {
        public static PetDatabase Instance { get; private set; }

        [Header("Pets")]
        [SerializeField] private List<PetData> _allPets;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePets();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePets()
        {
            _allPets = new List<PetData>
            {
                // ============================================================
                // MYTHOLOGICAL CREATURE PETS 🇰🇭
                // ============================================================

                // NAGA SERPENT 🐍
                new PetData
                {
                    petName = "Baby Naga",
                    petType = "naga_serpent",
                    element = "Water",
                    role = "Attacker",
                    description = "A young seven-headed serpent with boundless potential!",
                    stats = new PetStats { power = 60, speed = 70, defense = 40, intelligence = 75, energy = 65, precision = 70 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_water_spray", abilityName = "💦 Water Spray", description = "Spray water at enemies!", power = 30, energyCost = 15, cooldown = 3f, element = AbilityElement.Water },
                        new PetAbility { abilityId = "pet_tail_whip", abilityName = "🐍 Tail Whip", description = "Whip enemies with serpent tail!", power = 40, energyCost = 20, cooldown = 5f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_hypnotic_gaze", abilityName = "👁️ Hypnotic Gaze", description = "Stun enemies with mesmerizing eyes!", power = 0, energyCost = 25, cooldown = 10f, element = AbilityElement.Water }
                    }
                },

                // CHAMPA ELEPHANT 🐘
                new PetData
                {
                    petName = "Mini Champa",
                    petType = "champa_elephant",
                    element = "Earth",
                    role = "Tank",
                    description = "A baby elephant with ancient wisdom and thick skin!",
                    stats = new PetStats { power = 75, speed = 30, defense = 80, intelligence = 60, energy = 70, precision = 40 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_trunk_slap", abilityName = "🐘 Trunk Slap", description = "Slap enemies with trunk!", power = 45, energyCost = 15, cooldown = 4f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_earth_stomp", abilityName = "🌍 Earth Stomp", description = "Stomp the ground to stun nearby enemies!", power = 35, energyCost = 25, cooldown = 8f, element = AbilityElement.Earth },
                        new PetAbility { abilityId = "pet_protect", abilityName = "🛡️ Protective Stance", description = "Shield the robot from damage!", power = 0, energyCost = 20, cooldown = 12f, element = AbilityElement.Earth }
                    }
                },

                // GARUDA BIRD 🦅
                new PetData
                {
                    petName = "Garuda Chick",
                    petType = "garuda_bird",
                    element = "Wind",
                    role = "Scout",
                    description = "A divine bird hatchling with incredible speed!",
                    stats = new PetStats { power = 50, speed = 90, defense = 30, intelligence = 65, energy = 55, precision = 80 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_dive_bomb", abilityName = "💨 Dive Bomb", description = "Dive at enemies from above!", power = 50, energyCost = 20, cooldown = 5f, element = AbilityElement.Wind },
                        new PetAbility { abilityId = "pet_wing_gust", abilityName = "🌪️ Wing Gust", description = "Blow enemies back with powerful wings!", power = 25, energyCost = 15, cooldown = 6f, element = AbilityElement.Wind },
                        new PetAbility { abilityId = "pet_aerial_scout", abilityName = "👁️ Aerial Scout", description = "Reveal enemy weaknesses!", power = 0, energyCost = 10, cooldown = 15f, element = AbilityElement.Wind }
                    }
                },

                // SENA LION 🦁
                new PetData
                {
                    petName = "Sena Cub",
                    petType = "sena_lion",
                    element = "Fire",
                    role = "Attacker",
                    description = "A royal lion cub with a fierce spirit!",
                    stats = new PetStats { power = 80, speed = 60, defense = 55, intelligence = 50, energy = 60, precision = 55 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_claw_scratch", abilityName = "🐾 Claw Scratch", description = "Scratch enemies with sharp claws!", power = 45, energyCost = 15, cooldown = 3f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_mini_roar", abilityName = "🦁 Mini Roar", description = "A cute but intimidating roar!", power = 30, energyCost = 20, cooldown = 8f, element = AbilityElement.Fire },
                        new PetAbility { abilityId = "pet_pounce", abilityName = "💥 Pounce Attack", description = "Leap onto enemies!", power = 55, energyCost = 25, cooldown = 6f, element = AbilityElement.Physical }
                    }
                },

                // MAKARA DRAGON 🔥
                new PetData
                {
                    petName = "Makara Hatchling",
                    petType = "makara_dragon",
                    element = "Fire",
                    role = "Attacker",
                    description = "A baby sea dragon with fire and water powers!",
                    stats = new PetStats { power = 70, speed = 55, defense = 50, intelligence = 55, energy = 70, precision = 50 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_fire_breath", abilityName = "🔥 Baby Fire Breath", description = "Breathe small flames!", power = 40, energyCost = 20, cooldown = 5f, element = AbilityElement.Fire },
                        new PetAbility { abilityId = "pet_water_jet", abilityName = "💧 Water Jet", description = "Shoot water from snout!", power = 35, energyCost = 18, cooldown = 5f, element = AbilityElement.Water },
                        new PetAbility { abilityId = "pet_steam_cloud", abilityName = "☁️ Steam Cloud", description = "Create concealing steam!", power = 20, energyCost = 22, cooldown = 10f, element = AbilityElement.Fire }
                    }
                },

                // KAVI MONKEY 🐒
                new PetData
                {
                    petName = "Kavi Jr.",
                    petType = "kavi_monkey",
                    element = "Wind",
                    role = "Trickster",
                    description = "A mischievous little monkey who loves pranks!",
                    stats = new PetStats { power = 45, speed = 85, defense = 35, intelligence = 70, energy = 55, precision = 65 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_banana_throw", abilityName = "🍌 Banana Throw", description = "Throw a banana at enemies!", power = 25, energyCost = 10, cooldown = 2f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_tickle_attack", abilityName = "🤣 Tickle Attack", description = "Tickle enemies into submission!", power = 15, energyCost = 15, cooldown = 5f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_distraction", abilityName = "🎪 Distraction", description = "Confuse enemies with antics!", power = 0, energyCost = 20, cooldown = 8f, element = AbilityElement.Wind }
                    }
                },

                // MEALEA DANCER (Mini Apsara) 💃
                new PetData
                {
                    petName = "Little Apsara",
                    petType = "mealea_apsara",
                    element = "Celestial",
                    role = "Healer",
                    description = "A tiny celestial dancer with healing grace!",
                    stats = new PetStats { power = 40, speed = 70, defense = 35, intelligence = 85, energy = 75, precision = 75 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_healing_dance", abilityName = "💃 Healing Dance", description = "Dance to restore health!", power = 0, energyCost = 25, cooldown = 10f, element = AbilityElement.Celestial },
                        new PetAbility { abilityId = "pet_feather_toss", abilityName = "✨ Feather Toss", description = "Throw celestial feathers!", power = 30, energyCost = 15, cooldown = 4f, element = AbilityElement.Celestial },
                        new PetAbility { abilityId = "pet_grace_buff", abilityName = "🌟 Grace Buff", description = "Increase robot speed and precision!", power = 0, energyCost = 20, cooldown = 15f, element = AbilityElement.Celestial }
                    }
                },

                // KINNARI BIRD 🎵
                new PetData
                {
                    petName = "Kinnari Songbird",
                    petType = "kinnari_bird",
                    element = "Celestial",
                    role = "Buffer",
                    description = "A musical half-bird with enchanting songs!",
                    stats = new PetStats { power = 35, speed = 65, defense = 30, intelligence = 80, energy = 70, precision = 85 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_power_song", abilityName = "🎵 Power Song", description = "Sing to boost robot power!", power = 0, energyCost = 20, cooldown = 12f, element = AbilityElement.Celestial },
                        new PetAbility { abilityId = "pet_lullaby", abilityName = "😴 Lullaby", description = "Sing enemies to sleep!", power = 0, energyCost = 25, cooldown = 15f, element = AbilityElement.Celestial },
                        new PetAbility { abilityId = "pet_sonic_screech", abilityName = "📢 Sonic Screech", description = "Damaging sound wave!", power = 40, energyCost = 20, cooldown = 6f, element = AbilityElement.Celestial }
                    }
                },

                // REAHU SHADOW 🌑
                new PetData
                {
                    petName = "Shadow Sprite",
                    petType = "reahu_shadow",
                    element = "Shadow",
                    role = "Debuffer",
                    description = "A fragment of eclipse darkness!",
                    stats = new PetStats { power = 55, speed = 75, defense = 40, intelligence = 80, energy = 60, precision = 70 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_shadow_bite", abilityName = "🌑 Shadow Bite", description = "Bite from the shadows!", power = 45, energyCost = 18, cooldown = 4f, element = AbilityElement.Shadow },
                        new PetAbility { abilityId = "pet_curse", abilityName = "💀 Mini Curse", description = "Curse enemy to deal less damage!", power = 0, energyCost = 22, cooldown = 10f, element = AbilityElement.Shadow },
                        new PetAbility { abilityId = "pet_vanish", abilityName = "👻 Vanish", description = "Become invisible briefly!", power = 0, energyCost = 15, cooldown = 12f, element = AbilityElement.Shadow }
                    }
                },

                // PROHM ANCIENT 🦕
                new PetData
                {
                    petName = "Ancient Hatchling",
                    petType = "prohm_ancient",
                    element = "Earth",
                    role = "Tank",
                    description = "A prehistoric creature from temple depths!",
                    stats = new PetStats { power = 65, speed = 25, defense = 85, intelligence = 55, energy = 80, precision = 40 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_fossil_slam", abilityName = "🦴 Fossil Slam", description = "Slam with ancient power!", power = 50, energyCost = 20, cooldown = 5f, element = AbilityElement.Earth },
                        new PetAbility { abilityId = "pet_stone_skin", abilityName = "🪨 Stone Skin", description = "Harden skin to block damage!", power = 0, energyCost = 25, cooldown = 15f, element = AbilityElement.Earth },
                        new PetAbility { abilityId = "pet_regenerate", abilityName = "💚 Regenerate", description = "Slowly heal over time!", power = 0, energyCost = 30, cooldown = 20f, element = AbilityElement.Nature }
                    }
                },

                // YAK GIANT 👹
                new PetData
                {
                    petName = "Baby Yak",
                    petType = "yak_giant",
                    element = "Earth",
                    role = "Tank",
                    description = "A small but fierce temple guardian!",
                    stats = new PetStats { power = 85, speed = 20, defense = 80, intelligence = 30, energy = 75, precision = 30 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_giant_punch", abilityName = "👊 Giant Punch", description = "Powerful fist attack!", power = 60, energyCost = 22, cooldown = 5f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_guard_stance", abilityName = "🛡️ Guard Stance", description = "Take reduced damage!", power = 0, energyCost = 20, cooldown = 10f, element = AbilityElement.Earth },
                        new PetAbility { abilityId = "pet_intimidate", abilityName = "😠 Intimidate", description = "Scare enemies to reduce their attack!", power = 0, energyCost = 18, cooldown = 12f, element = AbilityElement.Physical }
                    }
                },

                // HANUMAN WARRIOR 🐒⚔️
                new PetData
                {
                    petName = "Hanuman Apprentice",
                    petType = "hanuman_warrior",
                    element = "Physical",
                    role = "Attacker",
                    description = "A young warrior monkey training to be a hero!",
                    stats = new PetStats { power = 70, speed = 75, defense = 50, intelligence = 55, energy = 55, precision = 65 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_staff_strike", abilityName = "🏑 Staff Strike", description = "Strike with a mini staff!", power = 45, energyCost = 18, cooldown = 4f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_flying_kick", abilityName = "🦵 Flying Kick", description = "Jump and kick enemies!", power = 50, energyCost = 22, cooldown = 6f, element = AbilityElement.Physical },
                        new PetAbility { abilityId = "pet_battle_cry", abilityName = "⚔️ Battle Cry", description = "Boost attack power temporarily!", power = 0, energyCost = 20, cooldown = 15f, element = AbilityElement.Physical }
                    }
                },

                // REAM EYSO THUNDER ⚡
                new PetData
                {
                    petName = "Thunder Sprite",
                    petType = "ream_eyso_thunder",
                    element = "Lightning",
                    role = "Attacker",
                    description = "A small spirit of lightning and storms!",
                    stats = new PetStats { power = 65, speed = 80, defense = 30, intelligence = 60, energy = 70, precision = 75 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_spark_shock", abilityName = "⚡ Spark Shock", description = "Zap enemies with electricity!", power = 40, energyCost = 18, cooldown = 3f, element = AbilityElement.Lightning },
                        new PetAbility { abilityId = "pet_chain_lightning", abilityName = "⛓️ Chain Lightning", description = "Lightning jumps between enemies!", power = 30, energyCost = 25, cooldown = 8f, element = AbilityElement.Lightning },
                        new PetAbility { abilityId = "pet_static_shield", abilityName = "🛡️ Static Shield", description = "Shock enemies who attack!", power = 20, energyCost = 22, cooldown = 12f, element = AbilityElement.Lightning }
                    }
                },

                // MONI MEKHALA CRYSTAL 💎
                new PetData
                {
                    petName = "Crystal Fairy",
                    petType = "moni_mekhala_spirit",
                    element = "Lightning",
                    role = "Support",
                    description = "A sparkling spirit with crystal magic!",
                    stats = new PetStats { power = 50, speed = 70, defense = 40, intelligence = 85, energy = 75, precision = 80 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_crystal_shard", abilityName = "💎 Crystal Shard", description = "Throw sharp crystals!", power = 35, energyCost = 15, cooldown = 4f, element = AbilityElement.Lightning },
                        new PetAbility { abilityId = "pet_reflect_shield", abilityName = "🪞 Reflect Shield", description = "Reflect projectiles back!", power = 0, energyCost = 28, cooldown = 15f, element = AbilityElement.Lightning },
                        new PetAbility { abilityId = "pet_flash", abilityName = "✨ Blinding Flash", description = "Blind all enemies briefly!", power = 0, energyCost = 20, cooldown = 12f, element = AbilityElement.Celestial }
                    }
                },

                // THORANI EARTH SPIRIT 🌍
                new PetData
                {
                    petName = "Earth Sprout",
                    petType = "thorani_earth",
                    element = "Nature",
                    role = "Healer",
                    description = "A nature spirit born from sacred ground!",
                    stats = new PetStats { power = 40, speed = 45, defense = 60, intelligence = 75, energy = 85, precision = 55 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_vine_whip", abilityName = "🌿 Vine Whip", description = "Whip with living vines!", power = 35, energyCost = 15, cooldown = 4f, element = AbilityElement.Nature },
                        new PetAbility { abilityId = "pet_healing_bloom", abilityName = "🌸 Healing Bloom", description = "Bloom flowers that heal!", power = 0, energyCost = 25, cooldown = 10f, element = AbilityElement.Nature },
                        new PetAbility { abilityId = "pet_root_trap", abilityName = "🌱 Root Trap", description = "Trap enemies with roots!", power = 20, energyCost = 22, cooldown = 12f, element = AbilityElement.Nature }
                    }
                },

                // NEANG KANGREI FOREST 🌳
                new PetData
                {
                    petName = "Forest Wisp",
                    petType = "kangrei_forest",
                    element = "Nature",
                    role = "Support",
                    description = "A spirit of the bamboo forest!",
                    stats = new PetStats { power = 45, speed = 60, defense = 45, intelligence = 70, energy = 80, precision = 60 },
                    abilities = new List<PetAbility>
                    {
                        new PetAbility { abilityId = "pet_bamboo_spear", abilityName = "🎋 Bamboo Spear", description = "Throw sharp bamboo!", power = 40, energyCost = 18, cooldown = 4f, element = AbilityElement.Nature },
                        new PetAbility { abilityId = "pet_forest_mist", abilityName = "🌫️ Forest Mist", description = "Create concealing mist!", power = 0, energyCost = 20, cooldown = 10f, element = AbilityElement.Nature },
                        new PetAbility { abilityId = "pet_nature_bond", abilityName = "🌿 Nature Bond", description = "Boost nature damage!", power = 0, energyCost = 22, cooldown = 15f, element = AbilityElement.Nature }
                    }
                }
            };

            // Generate IDs for all pets
            foreach (var pet in _allPets)
            {
                if (string.IsNullOrEmpty(pet.petId))
                {
                    pet.petId = System.Guid.NewGuid().ToString();
                }
            }

            Debug.Log($"🐾 Loaded {_allPets.Count} pets!");
        }

        #region Public Methods

        public List<PetData> GetAllPets() => _allPets;

        public PetData GetPetByType(string petType)
        {
            return _allPets.Find(p => p.petType == petType);
        }

        public PetData GetPetByName(string petName)
        {
            return _allPets.Find(p => p.petName == petName);
        }

        public List<PetData> GetPetsByElement(string element)
        {
            return _allPets.FindAll(p => p.element == element);
        }

        public List<PetData> GetPetsByRole(string role)
        {
            return _allPets.FindAll(p => p.role == role);
        }

        /// <summary>
        /// Get best pets for a specific robot
        /// </summary>
        public List<PetData> GetRecommendedPetsForRobot(RobotData robot)
        {
            var recommendations = new List<PetData>();

            // Get recommended pet types from fusion system
            var recommendedTypes = RobotPetFusion.Instance?.GetRecommendedPets(robot);
            if (recommendedTypes != null)
            {
                foreach (var petType in recommendedTypes)
                {
                    var pet = GetPetByType(petType);
                    if (pet != null && !recommendations.Contains(pet))
                    {
                        recommendations.Add(pet);
                    }
                }
            }

            // Also add element-complementary pets
            var robotElement = robot.combatStats.elementalAffinity.GetDominantElement();
            var complementaryElement = GetComplementaryElement(robotElement);
            var elementPets = GetPetsByElement(complementaryElement);
            recommendations.AddRange(elementPets);

            return recommendations;
        }

        private string GetComplementaryElement(string element)
        {
            return element switch
            {
                "Water" => "Wind",
                "Fire" => "Earth",
                "Earth" => "Fire",
                "Wind" => "Water",
                "Lightning" => "Water",
                "Nature" => "Celestial",
                "Celestial" => "Nature",
                "Shadow" => "Lightning",
                _ => "Physical"
            };
        }

        #endregion
    }
}

