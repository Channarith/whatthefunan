using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Challenges
{
    /// <summary>
    /// DAILY CHALLENGE SYSTEM! 🌅
    /// Fresh challenges every day to keep players coming back!
    /// Features: Daily rotation, streaks, bonus challenges, and special events!
    /// </summary>
    public class DailyChallengeSystem : MonoBehaviour
    {
        public static DailyChallengeSystem Instance { get; private set; }

        [Header("Daily Challenge Settings")]
        [SerializeField] private int _normalChallengesPerDay = 5;
        [SerializeField] private int _bonusChallengesPerDay = 2;
        [SerializeField] private int _maxStreak = 30;

        [Header("Current State")]
        [SerializeField] private List<DailyChallenge> _todaysChallenges = new List<DailyChallenge>();
        [SerializeField] private DailyChallenge _bonusChallenge;
        [SerializeField] private DailyChallenge _mysteryChallenge;
        [SerializeField] private int _currentStreak;
        [SerializeField] private DateTime _lastPlayDate;

        [Header("Challenge Pools")]
        [SerializeField] private List<DailyChallengeDefinition> _easyPool = new List<DailyChallengeDefinition>();
        [SerializeField] private List<DailyChallengeDefinition> _mediumPool = new List<DailyChallengeDefinition>();
        [SerializeField] private List<DailyChallengeDefinition> _hardPool = new List<DailyChallengeDefinition>();
        [SerializeField] private List<DailyChallengeDefinition> _sillyPool = new List<DailyChallengeDefinition>();
        [SerializeField] private List<DailyChallengeDefinition> _socialPool = new List<DailyChallengeDefinition>();
        [SerializeField] private List<DailyChallengeDefinition> _mysteryPool = new List<DailyChallengeDefinition>();

        // Events
        public event Action<List<DailyChallenge>> OnDailyChallengesRefreshed;
        public event Action<DailyChallenge> OnChallengeCompleted;
        public event Action<int> OnStreakUpdated;
        public event Action<DailyReward> OnStreakRewardEarned;
        public event Action OnAllDailiesCompleted;
        public event Action<DailyChallenge> OnBonusChallengeUnlocked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeChallengePools();
                LoadSavedState();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            CheckForNewDay();
        }

        private void InitializeChallengePools()
        {
            // ================================================================
            // EASY CHALLENGES - Quick wins to start the day!
            // ================================================================
            _easyPool = new List<DailyChallengeDefinition>
            {
                // EXPLORATION
                new DailyChallengeDefinition
                {
                    challengeId = "daily_explore_area",
                    displayName = "🗺️ Little Explorer",
                    description = "Visit 3 different areas in Funan!",
                    category = DailyChallengeCategory.Exploration,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 50, xp = 25 },
                    estimatedMinutes = 5
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_find_secret",
                    displayName = "🔍 Secret Finder",
                    description = "Discover 1 hidden spot!",
                    category = DailyChallengeCategory.Exploration,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 75, xp = 30 },
                    estimatedMinutes = 10
                },

                // COLLECTION
                new DailyChallengeDefinition
                {
                    challengeId = "daily_collect_flowers",
                    displayName = "🌸 Flower Picker",
                    description = "Collect 5 flowers!",
                    category = DailyChallengeCategory.Collection,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 40, xp = 20 },
                    estimatedMinutes = 5
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_collect_resources",
                    displayName = "📦 Resource Gatherer",
                    description = "Gather 10 resources of any type!",
                    category = DailyChallengeCategory.Collection,
                    targetCount = 10,
                    rewards = new DailyReward { coins = 60, xp = 30 },
                    estimatedMinutes = 8
                },

                // MINI-GAMES
                new DailyChallengeDefinition
                {
                    challengeId = "daily_fish_any",
                    displayName = "🐟 Gone Fishing",
                    description = "Catch 3 fish!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 50, xp = 25 },
                    estimatedMinutes = 5
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_cook_anything",
                    displayName = "🍳 Quick Cook",
                    description = "Cook 2 dishes!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 2,
                    rewards = new DailyReward { coins = 50, xp = 25 },
                    estimatedMinutes = 5
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_dance_once",
                    displayName = "💃 Quick Dance",
                    description = "Complete 1 dance!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 40, xp = 20 },
                    estimatedMinutes = 3
                },

                // BUILDING
                new DailyChallengeDefinition
                {
                    challengeId = "daily_place_decoration",
                    displayName = "🏠 Home Decorator",
                    description = "Place 3 decorations in your kingdom!",
                    category = DailyChallengeCategory.Building,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 45, xp = 20 },
                    estimatedMinutes = 3
                },

                // SOCIAL
                new DailyChallengeDefinition
                {
                    challengeId = "daily_pet_character",
                    displayName = "🤗 Friendly Pat",
                    description = "Pet a character 3 times!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 30, xp = 15 },
                    estimatedMinutes = 2
                },

                // COMBAT
                new DailyChallengeDefinition
                {
                    challengeId = "daily_defeat_enemies",
                    displayName = "⚔️ Shadow Smasher",
                    description = "Defeat 5 shadow creatures!",
                    category = DailyChallengeCategory.Combat,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 60, xp = 30 },
                    estimatedMinutes = 8
                }
            };

            // ================================================================
            // MEDIUM CHALLENGES - A bit more effort, better rewards!
            // ================================================================
            _mediumPool = new List<DailyChallengeDefinition>
            {
                new DailyChallengeDefinition
                {
                    challengeId = "daily_fish_big",
                    displayName = "🐋 Big Catch",
                    description = "Catch a fish weighing over 5kg!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 5 },
                    estimatedMinutes = 15
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_cook_perfect",
                    displayName = "👨‍🍳 Perfect Dish",
                    description = "Cook a dish with 3-star rating!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 5 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_dance_combo",
                    displayName = "💫 Combo Dancer",
                    description = "Get a 20-hit combo in dance!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 5 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_race_win",
                    displayName = "🏆 Race Champion",
                    description = "Win a mount race!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 5 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_build_structure",
                    displayName = "🏗️ Master Builder",
                    description = "Build a new structure in your kingdom!",
                    category = DailyChallengeCategory.Building,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 120, xp = 60, gems = 5 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_complete_quest",
                    displayName = "📜 Quest Completer",
                    description = "Complete 1 quest!",
                    category = DailyChallengeCategory.Quest,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 150, xp = 75 },
                    estimatedMinutes = 15
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_codex_learn",
                    displayName = "📚 Knowledge Seeker",
                    description = "Read 3 codex entries!",
                    category = DailyChallengeCategory.Education,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 80, xp = 60 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_photo_take",
                    displayName = "📸 Snapshot",
                    description = "Take 3 photos in photo mode!",
                    category = DailyChallengeCategory.Creative,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 75, xp = 40 },
                    estimatedMinutes = 8
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_music_create",
                    displayName = "🎵 Beat Maker",
                    description = "Create a music track with 3+ instruments!",
                    category = DailyChallengeCategory.Creative,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 5 },
                    estimatedMinutes = 15
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_defeat_boss",
                    displayName = "👹 Boss Beater",
                    description = "Defeat a mini-boss!",
                    category = DailyChallengeCategory.Combat,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 150, xp = 75, gems = 10 },
                    estimatedMinutes = 15
                }
            };

            // ================================================================
            // HARD CHALLENGES - For dedicated players! Big rewards!
            // ================================================================
            _hardPool = new List<DailyChallengeDefinition>
            {
                new DailyChallengeDefinition
                {
                    challengeId = "daily_fish_legendary",
                    displayName = "🐉 Legendary Fisher",
                    description = "Catch a LEGENDARY fish!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 300, xp = 150, gems = 25 },
                    estimatedMinutes = 30,
                    isHard = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_dance_perfect",
                    displayName = "⭐ Perfect Performance",
                    description = "Get an S-rank in any dance!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 250, xp = 125, gems = 20 },
                    estimatedMinutes = 20,
                    isHard = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_cook_all_recipes",
                    displayName = "👨‍🍳 Cooking Marathon",
                    description = "Cook 5 different recipes today!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 250, xp = 125, gems = 20 },
                    estimatedMinutes = 25,
                    isHard = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_no_damage",
                    displayName = "🛡️ Untouchable",
                    description = "Complete a combat without taking damage!",
                    category = DailyChallengeCategory.Combat,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 300, xp = 150, gems = 25 },
                    estimatedMinutes = 15,
                    isHard = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_complete_all_minigames",
                    displayName = "🎮 Mini-Game Master",
                    description = "Play ALL mini-games at least once today!",
                    category = DailyChallengeCategory.MiniGame,
                    targetCount = 4,
                    rewards = new DailyReward { coins = 400, xp = 200, gems = 30 },
                    estimatedMinutes = 40,
                    isHard = true
                }
            };

            // ================================================================
            // SILLY CHALLENGES - Fun and ridiculous! 🤪
            // ================================================================
            _sillyPool = new List<DailyChallengeDefinition>
            {
                new DailyChallengeDefinition
                {
                    challengeId = "daily_fart_attack",
                    displayName = "💨 Fart Fighter",
                    description = "Use 5 fart-based attacks! PFFFT!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 100, xp = 50, specialItem = "Whoopee Cushion" },
                    estimatedMinutes = 10,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_fall_10_times",
                    displayName = "🤕 Clumsy Today",
                    description = "Fall down 10 times! Oops!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 10,
                    rewards = new DailyReward { coins = 75, xp = 40 },
                    estimatedMinutes = 5,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_spin_dizzy",
                    displayName = "🌀 Dizzy Spinner",
                    description = "Spin in circles until dizzy 3 times!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 60, xp = 30 },
                    estimatedMinutes = 3,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_make_laugh",
                    displayName = "😂 Comedy Hour",
                    description = "Make 5 characters laugh with emotes!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 80, xp = 40 },
                    estimatedMinutes = 5,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_wear_silly",
                    displayName = "🎭 Costume Chaos",
                    description = "Wear a silly costume for 10 minutes!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 50, xp = 25 },
                    estimatedMinutes = 10,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_elephant_sneeze",
                    displayName = "🐘 ACHOO!",
                    description = "Make Champa sneeze with her trunk attack 3 times!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 3,
                    rewards = new DailyReward { coins = 80, xp = 40 },
                    estimatedMinutes = 5,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_monkey_banana",
                    displayName = "🍌 Banana Bonanza",
                    description = "Feed Kavi 10 bananas!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 10,
                    rewards = new DailyReward { coins = 70, xp = 35 },
                    estimatedMinutes = 5,
                    isSilly = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_hairball",
                    displayName = "🐱 Hairball Hero",
                    description = "Use Sena's Legendary Hairball attack!",
                    category = DailyChallengeCategory.Silly,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50 },
                    estimatedMinutes = 8,
                    isSilly = true
                }
            };

            // ================================================================
            // SOCIAL CHALLENGES - Connect with friends!
            // ================================================================
            _socialPool = new List<DailyChallengeDefinition>
            {
                new DailyChallengeDefinition
                {
                    challengeId = "daily_send_gift",
                    displayName = "🎁 Gift Giver",
                    description = "Send a gift to a friend!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 50, xp = 25, gems = 5 },
                    estimatedMinutes = 2
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_visit_friend",
                    displayName = "🏠 Friendly Visit",
                    description = "Visit a friend's kingdom!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 75, xp = 40, gems = 5 },
                    estimatedMinutes = 5
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_help_friend",
                    displayName = "🤝 Helping Hand",
                    description = "Help a friend with their kingdom!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 100, xp = 50, gems = 10 },
                    estimatedMinutes = 10
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_wave_emote",
                    displayName = "👋 Friendly Wave",
                    description = "Wave at 5 other players or NPCs!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 5,
                    rewards = new DailyReward { coins = 40, xp = 20 },
                    estimatedMinutes = 3
                },
                new DailyChallengeDefinition
                {
                    challengeId = "daily_coop_game",
                    displayName = "👥 Team Player",
                    description = "Complete a co-op activity with a friend!",
                    category = DailyChallengeCategory.Social,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 150, xp = 75, gems = 15 },
                    estimatedMinutes = 15
                }
            };

            // ================================================================
            // MYSTERY CHALLENGES - Unlock by completing dailies!
            // ================================================================
            _mysteryPool = new List<DailyChallengeDefinition>
            {
                new DailyChallengeDefinition
                {
                    challengeId = "mystery_golden_fish",
                    displayName = "❓ ???",
                    revealedName = "✨ Golden Opportunity",
                    description = "Catch the GOLDEN FISH that appears for 1 hour!",
                    category = DailyChallengeCategory.Mystery,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 500, xp = 250, gems = 50, specialItem = "Golden Scale" },
                    estimatedMinutes = 60,
                    isMystery = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "mystery_hidden_dance",
                    displayName = "❓ ???",
                    revealedName = "🕺 Secret Dance",
                    description = "A mysterious dance appeared! Learn it before midnight!",
                    category = DailyChallengeCategory.Mystery,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 400, xp = 200, gems = 40, specialItem = "Mystery Dance Scroll" },
                    estimatedMinutes = 20,
                    isMystery = true
                },
                new DailyChallengeDefinition
                {
                    challengeId = "mystery_naga_treasure",
                    displayName = "❓ ???",
                    revealedName = "🐍 Naga's Secret",
                    description = "The Naga Prince hints at hidden treasure... Find it!",
                    category = DailyChallengeCategory.Mystery,
                    targetCount = 1,
                    rewards = new DailyReward { coins = 600, xp = 300, gems = 75, specialItem = "Naga Pearl" },
                    estimatedMinutes = 30,
                    isMystery = true
                }
            };
        }

        private void CheckForNewDay()
        {
            DateTime today = DateTime.UtcNow.Date;

            if (_lastPlayDate.Date < today)
            {
                // It's a new day!
                if (_lastPlayDate.Date == today.AddDays(-1))
                {
                    // Consecutive day - increase streak!
                    _currentStreak = Mathf.Min(_currentStreak + 1, _maxStreak);
                    Debug.Log($"🔥 STREAK: Day {_currentStreak}!");
                }
                else
                {
                    // Streak broken
                    _currentStreak = 1;
                    Debug.Log("💔 Streak reset. Starting fresh!");
                }

                _lastPlayDate = today;
                GenerateNewDailyChallenges();
                CheckStreakReward();
                SaveState();

                OnStreakUpdated?.Invoke(_currentStreak);
            }
        }

        private void GenerateNewDailyChallenges()
        {
            _todaysChallenges.Clear();

            // Always include: 2 easy, 2 medium, 1 random from pools
            var easyChallenges = PickRandom(_easyPool, 2);
            var mediumChallenges = PickRandom(_mediumPool, 2);

            foreach (var def in easyChallenges)
                _todaysChallenges.Add(CreateChallenge(def));

            foreach (var def in mediumChallenges)
                _todaysChallenges.Add(CreateChallenge(def));

            // Add variety based on day of week
            DayOfWeek today = DateTime.UtcNow.DayOfWeek;
            switch (today)
            {
                case DayOfWeek.Monday:
                    // Motivation Monday - easy start!
                    _todaysChallenges.Add(CreateChallenge(PickRandom(_easyPool, 1)[0]));
                    break;
                case DayOfWeek.Tuesday:
                    // Try-hard Tuesday - add a hard challenge!
                    if (_hardPool.Count > 0)
                        _todaysChallenges.Add(CreateChallenge(PickRandom(_hardPool, 1)[0]));
                    break;
                case DayOfWeek.Wednesday:
                    // Wacky Wednesday - silly challenges!
                    if (_sillyPool.Count > 0)
                        _todaysChallenges.Add(CreateChallenge(PickRandom(_sillyPool, 1)[0]));
                    break;
                case DayOfWeek.Thursday:
                    // Thoughtful Thursday - educational
                    var eduChallenge = _mediumPool.Find(c => c.category == DailyChallengeCategory.Education);
                    if (eduChallenge != null)
                        _todaysChallenges.Add(CreateChallenge(eduChallenge));
                    break;
                case DayOfWeek.Friday:
                    // Friend Friday - social challenges!
                    if (_socialPool.Count > 0)
                        _todaysChallenges.Add(CreateChallenge(PickRandom(_socialPool, 1)[0]));
                    break;
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    // Weekend Warrior - DOUBLE challenges, bonus rewards!
                    _todaysChallenges.Add(CreateChallenge(PickRandom(_mediumPool, 1)[0]));
                    if (_sillyPool.Count > 0)
                        _todaysChallenges.Add(CreateChallenge(PickRandom(_sillyPool, 1)[0]));
                    break;
            }

            // Always add one silly challenge for fun!
            if (_sillyPool.Count > 0 && !_todaysChallenges.Exists(c => c.definition.isSilly))
            {
                _todaysChallenges.Add(CreateChallenge(PickRandom(_sillyPool, 1)[0]));
            }

            // Generate mystery challenge (hidden until all dailies complete)
            if (_mysteryPool.Count > 0)
            {
                _mysteryChallenge = CreateChallenge(PickRandom(_mysteryPool, 1)[0]);
                _mysteryChallenge.isLocked = true;
            }

            Debug.Log($"🌅 Generated {_todaysChallenges.Count} daily challenges!");
            OnDailyChallengesRefreshed?.Invoke(_todaysChallenges);
        }

        private DailyChallenge CreateChallenge(DailyChallengeDefinition def)
        {
            return new DailyChallenge
            {
                definition = def,
                currentProgress = 0,
                isCompleted = false,
                isLocked = false,
                startedAt = DateTime.UtcNow,
                expiresAt = DateTime.UtcNow.Date.AddDays(1) // Expires at midnight
            };
        }

        private List<DailyChallengeDefinition> PickRandom(List<DailyChallengeDefinition> pool, int count)
        {
            var result = new List<DailyChallengeDefinition>();
            var shuffled = new List<DailyChallengeDefinition>(pool);

            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
            {
                result.Add(shuffled[i]);
            }

            return result;
        }

        public void UpdateProgress(string challengeId, int amount = 1)
        {
            var challenge = _todaysChallenges.Find(c => c.definition.challengeId == challengeId);
            if (challenge == null || challenge.isCompleted) return;

            challenge.currentProgress += amount;

            if (challenge.currentProgress >= challenge.definition.targetCount)
            {
                CompleteChallenge(challenge);
            }
        }

        private void CompleteChallenge(DailyChallenge challenge)
        {
            challenge.isCompleted = true;
            challenge.completedAt = DateTime.UtcNow;

            // Award rewards
            AwardRewards(challenge.definition.rewards);

            Debug.Log($"✅ Challenge Complete: {challenge.definition.displayName}!");
            OnChallengeCompleted?.Invoke(challenge);

            // Check if all dailies complete
            CheckAllDailiesComplete();
        }

        private void CheckAllDailiesComplete()
        {
            bool allComplete = true;
            foreach (var challenge in _todaysChallenges)
            {
                if (!challenge.isCompleted)
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete)
            {
                Debug.Log("🎉 ALL DAILY CHALLENGES COMPLETE!");
                OnAllDailiesCompleted?.Invoke();

                // Unlock mystery challenge!
                if (_mysteryChallenge != null && _mysteryChallenge.isLocked)
                {
                    _mysteryChallenge.isLocked = false;
                    Debug.Log($"🔓 MYSTERY CHALLENGE UNLOCKED: {_mysteryChallenge.definition.revealedName}!");
                    OnBonusChallengeUnlocked?.Invoke(_mysteryChallenge);
                }

                // Bonus reward for completing all!
                var bonusReward = new DailyReward
                {
                    coins = 200,
                    xp = 100,
                    gems = 10
                };
                AwardRewards(bonusReward);
                Debug.Log("🎁 BONUS: All-complete reward given!");
            }
        }

        private void CheckStreakReward()
        {
            // Streak milestones with escalating rewards
            DailyReward streakReward = null;
            string milestone = "";

            switch (_currentStreak)
            {
                case 3:
                    milestone = "3-Day Streak!";
                    streakReward = new DailyReward { coins = 100, gems = 10 };
                    break;
                case 7:
                    milestone = "WEEK WARRIOR! 🗓️";
                    streakReward = new DailyReward { coins = 300, gems = 30, specialItem = "Week Warrior Badge" };
                    break;
                case 14:
                    milestone = "TWO WEEK TITAN! 💪";
                    streakReward = new DailyReward { coins = 500, gems = 50, specialItem = "Dedication Crown" };
                    break;
                case 30:
                    milestone = "MONTHLY MASTER! 👑";
                    streakReward = new DailyReward { coins = 1000, gems = 100, specialItem = "Legendary Streak Trophy" };
                    break;
            }

            if (streakReward != null)
            {
                Debug.Log($"🔥 STREAK MILESTONE: {milestone}");
                AwardRewards(streakReward);
                OnStreakRewardEarned?.Invoke(streakReward);
            }

            // Daily streak bonus (small bonus every day)
            int dailyStreakBonus = Mathf.Min(_currentStreak * 5, 100); // Max 100 coins
            var dailyBonus = new DailyReward { coins = dailyStreakBonus };
            AwardRewards(dailyBonus);
            Debug.Log($"🔥 Streak Bonus: +{dailyStreakBonus} coins!");
        }

        private void AwardRewards(DailyReward reward)
        {
            // Interface with economy system
            Debug.Log($"💰 Rewarded: {reward.coins} coins, {reward.xp} XP, {reward.gems} gems");
            if (!string.IsNullOrEmpty(reward.specialItem))
                Debug.Log($"🎁 Special Item: {reward.specialItem}");
        }

        private void LoadSavedState()
        {
            _currentStreak = PlayerPrefs.GetInt("DailyStreak", 0);
            string lastPlayStr = PlayerPrefs.GetString("LastPlayDate", "");
            if (DateTime.TryParse(lastPlayStr, out DateTime lastPlay))
            {
                _lastPlayDate = lastPlay;
            }
            else
            {
                _lastPlayDate = DateTime.MinValue;
            }
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt("DailyStreak", _currentStreak);
            PlayerPrefs.SetString("LastPlayDate", _lastPlayDate.ToString("o"));
            PlayerPrefs.Save();
        }

        // Public accessors
        public List<DailyChallenge> GetTodaysChallenges() => _todaysChallenges;
        public DailyChallenge GetMysteryChallenge() => _mysteryChallenge;
        public int GetCurrentStreak() => _currentStreak;
        public DateTime GetLastPlayDate() => _lastPlayDate;
        public TimeSpan GetTimeUntilReset() => DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
    }

    #region Daily Challenge Data Classes

    public enum DailyChallengeCategory
    {
        Exploration,
        Collection,
        MiniGame,
        Building,
        Social,
        Combat,
        Quest,
        Education,
        Creative,
        Silly,
        Mystery
    }

    [Serializable]
    public class DailyChallengeDefinition
    {
        public string challengeId;
        public string displayName;
        public string revealedName; // For mystery challenges
        public string description;
        public DailyChallengeCategory category;
        public int targetCount;
        public DailyReward rewards;
        public int estimatedMinutes;
        public bool isHard;
        public bool isSilly;
        public bool isMystery;
        public Sprite icon;
    }

    [Serializable]
    public class DailyChallenge
    {
        public DailyChallengeDefinition definition;
        public int currentProgress;
        public bool isCompleted;
        public bool isLocked;
        public DateTime startedAt;
        public DateTime completedAt;
        public DateTime expiresAt;
    }

    [Serializable]
    public class DailyReward
    {
        public int coins;
        public int xp;
        public int gems;
        public string specialItem;
    }

    #endregion
}

