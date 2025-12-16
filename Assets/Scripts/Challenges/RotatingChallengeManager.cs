using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Challenges
{
    /// <summary>
    /// Manages rotating weekly and monthly challenges to keep gameplay fresh!
    /// New challenges every week, big competitions every month!
    /// </summary>
    public class RotatingChallengeManager : MonoBehaviour
    {
        public static RotatingChallengeManager Instance { get; private set; }

        [Header("Current Challenges")]
        [SerializeField] private Challenge _currentWeeklyChallenge;
        [SerializeField] private Challenge _currentMonthlyChallenge;
        [SerializeField] private List<Challenge> _activeDailyChallenges = new List<Challenge>();

        [Header("Challenge Pools")]
        [SerializeField] private List<ChallengeDefinition> _weeklyPool = new List<ChallengeDefinition>();
        [SerializeField] private List<ChallengeDefinition> _monthlyPool = new List<ChallengeDefinition>();
        [SerializeField] private List<ChallengeDefinition> _dailyPool = new List<ChallengeDefinition>();

        // Events
        public event Action<Challenge> OnNewWeeklyChallenge;
        public event Action<Challenge> OnNewMonthlyChallenge;
        public event Action<Challenge> OnChallengeCompleted;
        public event Action<ChallengeReward> OnRewardEarned;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeChallengePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            CheckForNewChallenges();
        }

        private void InitializeChallengePools()
        {
            // ============================================================
            // WEEKLY CHALLENGES - Fun, accessible, everyone can participate!
            // ============================================================
            _weeklyPool = new List<ChallengeDefinition>
            {
                // MUSIC CREATION WEEK
                new ChallengeDefinition
                {
                    challengeId = "weekly_music_creation",
                    displayName = "🎵 MUSIC MAKER WEEK! 🎵",
                    description = "Create your own Funan beats! Use drums, gongs, and xylophones to make music!",
                    challengeType = ChallengeType.MusicCreation,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 500, gems = 25, specialItem = "Golden Gong" }
                },

                // KINGDOM BEAUTY CONTEST
                new ChallengeDefinition
                {
                    challengeId = "weekly_kingdom_beauty",
                    displayName = "🏰 PRETTIEST KINGDOM WEEK! 🏰",
                    description = "Decorate your kingdom and show it off! Most creative kingdom wins!",
                    challengeType = ChallengeType.KingdomCreation,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 750, gems = 50, specialItem = "Royal Throne" }
                },

                // DANCE PARTY WEEK
                new ChallengeDefinition
                {
                    challengeId = "weekly_dance_party",
                    displayName = "💃 APSARA DANCE WEEK! 💃",
                    description = "Learn new dances and perform them! Best dancers get prizes!",
                    challengeType = ChallengeType.DanceChallenge,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 400, gems = 20, specialItem = "Dance Ribbon" }
                },

                // COOKING COMPETITION
                new ChallengeDefinition
                {
                    challengeId = "weekly_master_chef",
                    displayName = "🍜 MASTER CHEF WEEK! 🍜",
                    description = "Cook the most delicious Khmer dishes! Amok, Lok Lak, and more!",
                    challengeType = ChallengeType.CookingChallenge,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 450, gems = 20, specialItem = "Golden Wok" }
                },

                // PHOTO MODE WEEK
                new ChallengeDefinition
                {
                    challengeId = "weekly_photo_contest",
                    displayName = "📸 BEST PHOTO WEEK! 📸",
                    description = "Take the funniest, prettiest, or silliest photos in your kingdom!",
                    challengeType = ChallengeType.PhotoContest,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 350, gems = 15, specialItem = "Golden Camera" }
                },

                // FISHING TOURNAMENT
                new ChallengeDefinition
                {
                    challengeId = "weekly_fishing_tourney",
                    displayName = "🐟 BIG FISH WEEK! 🐟",
                    description = "Catch the BIGGEST fish in the Mekong! Who's the best fisher?",
                    challengeType = ChallengeType.FishingTournament,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 400, gems = 20, specialItem = "Lucky Fishing Rod" }
                },

                // RACING CHAMPIONSHIP
                new ChallengeDefinition
                {
                    challengeId = "weekly_racing_champ",
                    displayName = "🏃 SPEED DEMON WEEK! 🏃",
                    description = "Race your mounts! Who has the fastest elephant? Quickest Naga?",
                    challengeType = ChallengeType.RacingChampionship,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 500, gems = 25, specialItem = "Speed Saddle" }
                },

                // FRIENDSHIP WEEK
                new ChallengeDefinition
                {
                    challengeId = "weekly_friendship",
                    displayName = "💝 FRIENDSHIP WEEK! 💝",
                    description = "Send gifts, help friends, and spread kindness! Most helpful friend wins!",
                    challengeType = ChallengeType.FriendshipChallenge,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = false, // Everyone wins by being kind!
                    rewards = new ChallengeReward { coins = 300, gems = 30, specialItem = "Friendship Bracelet" }
                },

                // TREASURE HUNT
                new ChallengeDefinition
                {
                    challengeId = "weekly_treasure_hunt",
                    displayName = "🗺️ TREASURE HUNT WEEK! 🗺️",
                    description = "Find hidden treasures around Funan! Solve riddles to find them all!",
                    challengeType = ChallengeType.TreasureHunt,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = false,
                    rewards = new ChallengeReward { coins = 600, gems = 35, specialItem = "Treasure Map" }
                },

                // COSTUME PARTY
                new ChallengeDefinition
                {
                    challengeId = "weekly_costume_party",
                    displayName = "👗 COSTUME PARTY WEEK! 👗",
                    description = "Dress up your characters in the SILLIEST outfits! Fashion show time!",
                    challengeType = ChallengeType.CostumeContest,
                    duration = TimeSpan.FromDays(7),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 400, gems = 20, specialItem = "Fancy Hat" }
                }
            };

            // ============================================================
            // MONTHLY CHALLENGES - BIG events with BIG rewards!
            // ============================================================
            _monthlyPool = new List<ChallengeDefinition>
            {
                // GRAND KINGDOM CHAMPIONSHIP
                new ChallengeDefinition
                {
                    challengeId = "monthly_kingdom_grand",
                    displayName = "👑 GRAND KINGDOM CHAMPIONSHIP! 👑",
                    description = "Build the most AMAZING kingdom over the whole month! Grand prizes await!",
                    challengeType = ChallengeType.KingdomCreation,
                    duration = TimeSpan.FromDays(30),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 5000, gems = 500, specialItem = "Royal Crown", legendaryUnlock = "Golden Palace" }
                },

                // FUNAN'S GOT TALENT
                new ChallengeDefinition
                {
                    challengeId = "monthly_got_talent",
                    displayName = "⭐ FUNAN'S GOT TALENT! ⭐",
                    description = "Show off ANY talent! Dancing, music, fishing, cooking - BE THE STAR!",
                    challengeType = ChallengeType.TalentShow,
                    duration = TimeSpan.FromDays(30),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 3000, gems = 300, specialItem = "Star Trophy", legendaryUnlock = "Talent Stage" }
                },

                // WATER FESTIVAL CELEBRATION
                new ChallengeDefinition
                {
                    challengeId = "monthly_water_festival",
                    displayName = "💧 WATER FESTIVAL MONTH! 💧",
                    description = "Celebrate Bon Om Touk! Boat races, water games, and river fun!",
                    challengeType = ChallengeType.SeasonalFestival,
                    duration = TimeSpan.FromDays(30),
                    isCompetitive = false,
                    rewards = new ChallengeReward { coins = 2000, gems = 200, specialItem = "Festival Boat", legendaryUnlock = "River Parade Float" }
                },

                // STORY CREATOR MONTH
                new ChallengeDefinition
                {
                    challengeId = "monthly_story_creator",
                    displayName = "📖 STORY CREATOR MONTH! 📖",
                    description = "Create your own Funan legend! Write stories, make comics, share tales!",
                    challengeType = ChallengeType.StoryCreation,
                    duration = TimeSpan.FromDays(30),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 2500, gems = 250, specialItem = "Storyteller's Scroll", legendaryUnlock = "Library Building" }
                },

                // ULTIMATE MUSIC FESTIVAL
                new ChallengeDefinition
                {
                    challengeId = "monthly_music_fest",
                    displayName = "🎶 FUNAN MUSIC FESTIVAL! 🎶",
                    description = "Create songs, perform concerts, and become a MUSIC LEGEND!",
                    challengeType = ChallengeType.MusicCreation,
                    duration = TimeSpan.FromDays(30),
                    isCompetitive = true,
                    rewards = new ChallengeReward { coins = 3500, gems = 350, specialItem = "Legendary Instrument", legendaryUnlock = "Concert Stage" }
                }
            };

            // ============================================================
            // DAILY CHALLENGES - Quick fun every day!
            // ============================================================
            _dailyPool = new List<ChallengeDefinition>
            {
                new ChallengeDefinition { challengeId = "daily_fish_5", displayName = "Catch 5 Fish!", challengeType = ChallengeType.FishingTournament, rewards = new ChallengeReward { coins = 50 } },
                new ChallengeDefinition { challengeId = "daily_cook_3", displayName = "Cook 3 Meals!", challengeType = ChallengeType.CookingChallenge, rewards = new ChallengeReward { coins = 50 } },
                new ChallengeDefinition { challengeId = "daily_dance_1", displayName = "Dance Once!", challengeType = ChallengeType.DanceChallenge, rewards = new ChallengeReward { coins = 30 } },
                new ChallengeDefinition { challengeId = "daily_photo_1", displayName = "Take a Silly Photo!", challengeType = ChallengeType.PhotoContest, rewards = new ChallengeReward { coins = 30 } },
                new ChallengeDefinition { challengeId = "daily_gift_1", displayName = "Send a Gift!", challengeType = ChallengeType.FriendshipChallenge, rewards = new ChallengeReward { coins = 40, gems = 5 } },
                new ChallengeDefinition { challengeId = "daily_build_1", displayName = "Build Something!", challengeType = ChallengeType.KingdomCreation, rewards = new ChallengeReward { coins = 60 } },
                new ChallengeDefinition { challengeId = "daily_race_1", displayName = "Win a Race!", challengeType = ChallengeType.RacingChampionship, rewards = new ChallengeReward { coins = 50 } },
                new ChallengeDefinition { challengeId = "daily_music_1", displayName = "Make a Beat!", challengeType = ChallengeType.MusicCreation, rewards = new ChallengeReward { coins = 40 } }
            };
        }

        private void CheckForNewChallenges()
        {
            DateTime now = DateTime.UtcNow;

            // Check if we need a new weekly challenge (every Monday)
            if (_currentWeeklyChallenge == null || _currentWeeklyChallenge.endTime < now)
            {
                StartNewWeeklyChallenge();
            }

            // Check if we need a new monthly challenge (1st of each month)
            if (_currentMonthlyChallenge == null || _currentMonthlyChallenge.endTime < now)
            {
                StartNewMonthlyChallenge();
            }

            // Refresh daily challenges
            RefreshDailyChallenges();
        }

        public void StartNewWeeklyChallenge()
        {
            // Pick a random challenge from the pool
            int index = UnityEngine.Random.Range(0, _weeklyPool.Count);
            var definition = _weeklyPool[index];

            _currentWeeklyChallenge = new Challenge
            {
                definition = definition,
                startTime = GetNextMondayUtc(),
                endTime = GetNextMondayUtc().AddDays(7),
                participants = new List<ChallengeParticipant>(),
                state = ChallengeState.Active
            };

            Debug.Log($"🎉 NEW WEEKLY CHALLENGE: {definition.displayName}");
            OnNewWeeklyChallenge?.Invoke(_currentWeeklyChallenge);
        }

        public void StartNewMonthlyChallenge()
        {
            int index = UnityEngine.Random.Range(0, _monthlyPool.Count);
            var definition = _monthlyPool[index];

            _currentMonthlyChallenge = new Challenge
            {
                definition = definition,
                startTime = GetFirstOfMonthUtc(),
                endTime = GetFirstOfMonthUtc().AddMonths(1),
                participants = new List<ChallengeParticipant>(),
                state = ChallengeState.Active
            };

            Debug.Log($"👑 NEW MONTHLY CHALLENGE: {definition.displayName}");
            OnNewMonthlyChallenge?.Invoke(_currentMonthlyChallenge);
        }

        private void RefreshDailyChallenges()
        {
            _activeDailyChallenges.Clear();
            
            // Pick 3 random daily challenges
            var shuffled = new List<ChallengeDefinition>(_dailyPool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            for (int i = 0; i < Mathf.Min(3, shuffled.Count); i++)
            {
                _activeDailyChallenges.Add(new Challenge
                {
                    definition = shuffled[i],
                    startTime = DateTime.UtcNow.Date,
                    endTime = DateTime.UtcNow.Date.AddDays(1),
                    state = ChallengeState.Active
                });
            }
        }

        public void SubmitChallengeEntry(string challengeId, ChallengeEntry entry)
        {
            Challenge challenge = GetChallengeById(challengeId);
            if (challenge == null || challenge.state != ChallengeState.Active)
            {
                Debug.LogWarning("Challenge not found or not active!");
                return;
            }

            // Add or update participant entry
            var participant = challenge.participants.Find(p => p.playerId == entry.playerId);
            if (participant == null)
            {
                participant = new ChallengeParticipant
                {
                    playerId = entry.playerId,
                    playerName = entry.playerName
                };
                challenge.participants.Add(participant);
            }

            participant.entries.Add(entry);
            participant.lastEntryTime = DateTime.UtcNow;

            Debug.Log($"📤 Entry submitted for {challenge.definition.displayName} by {entry.playerName}!");
        }

        public void VoteForEntry(string challengeId, string entryId, string voterId)
        {
            Challenge challenge = GetChallengeById(challengeId);
            if (challenge == null) return;

            foreach (var participant in challenge.participants)
            {
                var entry = participant.entries.Find(e => e.entryId == entryId);
                if (entry != null && !entry.voterIds.Contains(voterId))
                {
                    entry.votes++;
                    entry.voterIds.Add(voterId);
                    Debug.Log($"👍 Vote added! Entry now has {entry.votes} votes!");
                    return;
                }
            }
        }

        public List<ChallengeParticipant> GetLeaderboard(string challengeId, int topCount = 10)
        {
            Challenge challenge = GetChallengeById(challengeId);
            if (challenge == null) return new List<ChallengeParticipant>();

            var sorted = new List<ChallengeParticipant>(challenge.participants);
            sorted.Sort((a, b) => b.GetTotalVotes().CompareTo(a.GetTotalVotes()));

            return sorted.GetRange(0, Mathf.Min(topCount, sorted.Count));
        }

        public void EndChallengeAndAwardPrizes(string challengeId)
        {
            Challenge challenge = GetChallengeById(challengeId);
            if (challenge == null) return;

            challenge.state = ChallengeState.Completed;

            // Award prizes to top participants
            var leaderboard = GetLeaderboard(challengeId, 10);
            
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var participant = leaderboard[i];
                var reward = CalculateReward(challenge.definition.rewards, i + 1);
                
                // Award the reward
                AwardReward(participant.playerId, reward, i + 1);
                
                Debug.Log($"🏆 #{i + 1} - {participant.playerName} wins: {reward.coins} coins, {reward.gems} gems!");
            }

            // Also award participation rewards to everyone else
            foreach (var participant in challenge.participants)
            {
                if (!leaderboard.Contains(participant))
                {
                    var participationReward = new ChallengeReward
                    {
                        coins = challenge.definition.rewards.coins / 10,
                        gems = challenge.definition.rewards.gems / 10
                    };
                    AwardReward(participant.playerId, participationReward, 0);
                }
            }

            OnChallengeCompleted?.Invoke(challenge);
        }

        private ChallengeReward CalculateReward(ChallengeReward baseReward, int placement)
        {
            float multiplier = placement switch
            {
                1 => 1.0f,    // 1st place: 100%
                2 => 0.7f,    // 2nd place: 70%
                3 => 0.5f,    // 3rd place: 50%
                _ => 0.2f     // Everyone else: 20%
            };

            return new ChallengeReward
            {
                coins = Mathf.RoundToInt(baseReward.coins * multiplier),
                gems = Mathf.RoundToInt(baseReward.gems * multiplier),
                specialItem = placement <= 3 ? baseReward.specialItem : null,
                legendaryUnlock = placement == 1 ? baseReward.legendaryUnlock : null
            };
        }

        private void AwardReward(string playerId, ChallengeReward reward, int placement)
        {
            // In real implementation, this would interface with CurrencyManager
            Debug.Log($"💰 Awarding {playerId}: {reward.coins} coins, {reward.gems} gems");
            
            if (!string.IsNullOrEmpty(reward.specialItem))
                Debug.Log($"🎁 Special Item: {reward.specialItem}");
            
            if (!string.IsNullOrEmpty(reward.legendaryUnlock))
                Debug.Log($"⭐ LEGENDARY UNLOCK: {reward.legendaryUnlock}");

            OnRewardEarned?.Invoke(reward);
        }

        private Challenge GetChallengeById(string challengeId)
        {
            if (_currentWeeklyChallenge?.definition.challengeId == challengeId)
                return _currentWeeklyChallenge;
            if (_currentMonthlyChallenge?.definition.challengeId == challengeId)
                return _currentMonthlyChallenge;
            return _activeDailyChallenges.Find(c => c.definition.challengeId == challengeId);
        }

        private DateTime GetNextMondayUtc()
        {
            DateTime today = DateTime.UtcNow.Date;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            return today.AddDays(daysUntilMonday);
        }

        private DateTime GetFirstOfMonthUtc()
        {
            DateTime now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, 1).AddMonths(1);
        }

        // Public accessors
        public Challenge GetCurrentWeeklyChallenge() => _currentWeeklyChallenge;
        public Challenge GetCurrentMonthlyChallenge() => _currentMonthlyChallenge;
        public List<Challenge> GetDailyChallenges() => _activeDailyChallenges;
    }

    #region Data Classes

    public enum ChallengeType
    {
        MusicCreation,      // Make your own music!
        KingdomCreation,    // Build the best kingdom!
        DanceChallenge,     // Dance performances
        CookingChallenge,   // Cooking competitions
        PhotoContest,       // Best photos
        FishingTournament,  // Catch the biggest fish
        RacingChampionship, // Mount races
        FriendshipChallenge,// Help friends
        TreasureHunt,       // Find hidden items
        CostumeContest,     // Fashion show
        TalentShow,         // Any talent!
        SeasonalFestival,   // Festival events
        StoryCreation       // Create stories
    }

    public enum ChallengeState
    {
        Upcoming,
        Active,
        Voting,     // Entries closed, voting open
        Completed
    }

    [Serializable]
    public class ChallengeDefinition
    {
        public string challengeId;
        public string displayName;
        public string description;
        public ChallengeType challengeType;
        public TimeSpan duration;
        public bool isCompetitive;
        public ChallengeReward rewards;
        public Sprite icon;
        public string[] requirements;
    }

    [Serializable]
    public class Challenge
    {
        public ChallengeDefinition definition;
        public DateTime startTime;
        public DateTime endTime;
        public List<ChallengeParticipant> participants;
        public ChallengeState state;
    }

    [Serializable]
    public class ChallengeParticipant
    {
        public string playerId;
        public string playerName;
        public List<ChallengeEntry> entries = new List<ChallengeEntry>();
        public DateTime lastEntryTime;

        public int GetTotalVotes()
        {
            int total = 0;
            foreach (var entry in entries)
                total += entry.votes;
            return total;
        }
    }

    [Serializable]
    public class ChallengeEntry
    {
        public string entryId;
        public string playerId;
        public string playerName;
        public string entryData;        // JSON data for the entry (music, kingdom, photo, etc.)
        public DateTime submittedAt;
        public int votes;
        public List<string> voterIds = new List<string>();
        public Sprite thumbnail;
    }

    [Serializable]
    public class ChallengeReward
    {
        public int coins;
        public int gems;
        public string specialItem;
        public string legendaryUnlock;
        public int xpBonus;
    }

    #endregion
}

