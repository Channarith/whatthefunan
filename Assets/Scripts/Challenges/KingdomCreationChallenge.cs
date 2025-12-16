using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Challenges
{
    /// <summary>
    /// KINGDOM CREATION CHALLENGE! 🏰
    /// Build the most beautiful, creative, or silly kingdom!
    /// Weekly and monthly competitions with amazing prizes!
    /// </summary>
    public class KingdomCreationChallenge : MonoBehaviour
    {
        public static KingdomCreationChallenge Instance { get; private set; }

        [Header("Challenge Settings")]
        [SerializeField] private KingdomTheme _currentTheme;
        [SerializeField] private List<KingdomTheme> _availableThemes = new List<KingdomTheme>();

        [Header("Scoring Criteria")]
        [SerializeField] private List<ScoringCategory> _scoringCategories = new List<ScoringCategory>();

        // Events
        public event Action<KingdomSnapshot> OnKingdomSnapshotTaken;
        public event Action<KingdomSubmission> OnKingdomSubmitted;
        public event Action<List<KingdomSubmission>> OnLeaderboardUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeThemes();
                InitializeScoringCategories();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeThemes()
        {
            _availableThemes = new List<KingdomTheme>
            {
                // ============================================================
                // WEEKLY THEMES - Different each week!
                // ============================================================
                
                new KingdomTheme
                {
                    themeId = "theme_traditional",
                    displayName = "🏛️ Traditional Temple Kingdom",
                    description = "Build like the ancient Khmer kings! Temples, towers, and timeless beauty!",
                    requirements = new string[] { "At least 1 temple", "Stone paths", "Guardian statues" },
                    bonusPoints = new string[] { "Water features +100", "Symmetry +150", "Golden decorations +50" },
                    bannedItems = new string[] { "Modern items", "Silly decorations" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_jungle",
                    displayName = "🌴 Jungle Paradise",
                    description = "Let nature take over! Trees, vines, and hidden jungle secrets!",
                    requirements = new string[] { "Lots of trees", "Vine decorations", "Hidden paths" },
                    bonusPoints = new string[] { "Ta Prohm style +200", "Wildlife +100", "Waterfalls +150" },
                    bannedItems = new string[] { "Too much stone" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_silly",
                    displayName = "🤪 MAXIMUM SILLINESS!",
                    description = "The SILLIEST kingdom wins! Be ridiculous! Be random! Be FUNNY!",
                    requirements = new string[] { "Must make people laugh!" },
                    bonusPoints = new string[] { "Whoopee cushions +100", "Funny statues +150", "Chaos +200" },
                    bannedItems = new string[] { "Boring stuff" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_water",
                    displayName = "💧 Floating Water Kingdom",
                    description = "Embrace the rivers! Build on water, bridges everywhere, boat parking!",
                    requirements = new string[] { "Must be near/on water", "Bridges", "Boats" },
                    bonusPoints = new string[] { "Floating buildings +200", "Fish ponds +100", "Lotus gardens +150" },
                    bannedItems = new string[] { "No fire decorations" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_festival",
                    displayName = "🎉 Festival Kingdom",
                    description = "Celebrate every day! Lanterns, stages, and party decorations!",
                    requirements = new string[] { "Lanterns", "Stage or performance area", "Colorful decorations" },
                    bonusPoints = new string[] { "Fireworks area +150", "Food stalls +100", "Dance floor +200" },
                    bannedItems = new string[] { }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_spooky",
                    displayName = "👻 Spooky (But Not Scary!) Kingdom",
                    description = "Friendly ghosts, silly bats, and haunted (but cute!) houses!",
                    requirements = new string[] { "Ghost decorations", "Dark colors", "Mysterious areas" },
                    bonusPoints = new string[] { "Naga tunnels +150", "Glowing items +100", "Fog effects +100" },
                    bannedItems = new string[] { "Actually scary stuff" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_rainbow",
                    displayName = "🌈 Rainbow Color Kingdom",
                    description = "Use ALL THE COLORS! The most colorful kingdom wins!",
                    requirements = new string[] { "At least 5 different colors", "Variety!" },
                    bonusPoints = new string[] { "All rainbow colors +300", "Color coordination +150" },
                    bannedItems = new string[] { "All gray/brown" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_tiny",
                    displayName = "🐜 Tiny Kingdom (Small Is Beautiful!)",
                    description = "Limited space! Build the BEST kingdom in the SMALLEST area!",
                    requirements = new string[] { "Stay within tiny boundary", "Efficiency!" },
                    bonusPoints = new string[] { "Multi-use buildings +200", "Clever design +150" },
                    bannedItems = new string[] { "Spreading out" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_mega",
                    displayName = "🏔️ MEGA Kingdom (Go Big!)",
                    description = "BIGGEST wins! Tallest towers! Widest roads! MAXIMUM SIZE!",
                    requirements = new string[] { "BIG buildings", "Wide roads", "Tall structures" },
                    bonusPoints = new string[] { "Tallest building +300", "Biggest area +200" },
                    bannedItems = new string[] { "Tiny decorations" }
                },
                
                new KingdomTheme
                {
                    themeId = "theme_character",
                    displayName = "💕 Character Fan Kingdom",
                    description = "Dedicate your kingdom to your FAVORITE character!",
                    requirements = new string[] { "Theme around one character", "Their favorite things" },
                    bonusPoints = new string[] { "Character statue +200", "Their colors +100", "Story elements +150" },
                    bannedItems = new string[] { }
                }
            };
        }

        private void InitializeScoringCategories()
        {
            _scoringCategories = new List<ScoringCategory>
            {
                // ============================================================
                // HOW KINGDOMS ARE JUDGED
                // ============================================================
                
                new ScoringCategory
                {
                    categoryId = "creativity",
                    displayName = "✨ Creativity",
                    description = "How unique and original is the kingdom?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Community // Players vote!
                },
                
                new ScoringCategory
                {
                    categoryId = "theme_match",
                    displayName = "🎯 Theme Match",
                    description = "How well does it match the week's theme?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Automatic // System checks requirements
                },
                
                new ScoringCategory
                {
                    categoryId = "beauty",
                    displayName = "😍 Beauty",
                    description = "How pretty is it? Do you want to live there?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Community
                },
                
                new ScoringCategory
                {
                    categoryId = "silliness",
                    displayName = "🤣 Silliness Factor",
                    description = "How much does it make you laugh?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Community
                },
                
                new ScoringCategory
                {
                    categoryId = "detail",
                    displayName = "🔍 Attention to Detail",
                    description = "Are there cool little touches everywhere?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Community
                },
                
                new ScoringCategory
                {
                    categoryId = "popularity",
                    displayName = "❤️ Popularity",
                    description = "How many visits and likes?",
                    maxPoints = 100,
                    judgedBy = JudgeType.Automatic // Based on visit count
                }
            };
        }

        public void SetCurrentTheme(string themeId)
        {
            _currentTheme = _availableThemes.Find(t => t.themeId == themeId);
            Debug.Log($"🎨 Theme set: {_currentTheme?.displayName}");
        }

        public KingdomSnapshot TakeKingdomSnapshot()
        {
            // Capture current state of player's kingdom
            var snapshot = new KingdomSnapshot
            {
                snapshotId = Guid.NewGuid().ToString(),
                capturedAt = DateTime.UtcNow,
                buildings = GetAllBuildings(),
                decorations = GetAllDecorations(),
                layout = GetLayoutData(),
                statistics = CalculateStatistics()
            };

            // Take screenshot for thumbnail
            // snapshot.thumbnailPath = CaptureScreenshot();

            Debug.Log($"📸 Kingdom snapshot taken! {snapshot.buildings.Count} buildings, {snapshot.decorations.Count} decorations");
            OnKingdomSnapshotTaken?.Invoke(snapshot);

            return snapshot;
        }

        public void SubmitKingdomToChallenge(string challengeId, string title, string description)
        {
            var snapshot = TakeKingdomSnapshot();

            var submission = new KingdomSubmission
            {
                submissionId = Guid.NewGuid().ToString(),
                playerId = "current_player", // From auth
                playerName = "Player",
                kingdomName = title,
                description = description,
                themeId = _currentTheme?.themeId,
                snapshot = snapshot,
                submittedAt = DateTime.UtcNow,
                scores = new Dictionary<string, float>(),
                totalVotes = 0
            };

            // Calculate automatic scores
            submission.scores["theme_match"] = CalculateThemeMatchScore(snapshot);
            submission.scores["popularity"] = 0; // Starts at 0, increases with visits

            // Submit to challenge
            var entry = new ChallengeEntry
            {
                entryId = submission.submissionId,
                playerId = submission.playerId,
                playerName = submission.playerName,
                entryData = JsonUtility.ToJson(submission),
                submittedAt = submission.submittedAt
            };

            RotatingChallengeManager.Instance?.SubmitChallengeEntry(challengeId, entry);

            Debug.Log($"🏰 Kingdom '{title}' submitted to challenge!");
            OnKingdomSubmitted?.Invoke(submission);
        }

        private float CalculateThemeMatchScore(KingdomSnapshot snapshot)
        {
            if (_currentTheme == null) return 50f;

            float score = 50f; // Base score

            // Check requirements
            foreach (var requirement in _currentTheme.requirements)
            {
                if (CheckRequirement(snapshot, requirement))
                {
                    score += 10f;
                }
            }

            // Check bonus points
            foreach (var bonus in _currentTheme.bonusPoints)
            {
                if (CheckBonus(snapshot, bonus))
                {
                    // Extract point value from string like "Water features +100"
                    var parts = bonus.Split('+');
                    if (parts.Length > 1 && float.TryParse(parts[1].Trim(), out float points))
                    {
                        score += points / 10f; // Scale down
                    }
                }
            }

            // Check banned items (penalty)
            foreach (var banned in _currentTheme.bannedItems)
            {
                if (HasBannedItem(snapshot, banned))
                {
                    score -= 20f;
                }
            }

            return Mathf.Clamp(score, 0f, 100f);
        }

        private bool CheckRequirement(KingdomSnapshot snapshot, string requirement)
        {
            // Simplified check - in real implementation would analyze snapshot data
            return true;
        }

        private bool CheckBonus(KingdomSnapshot snapshot, string bonus)
        {
            return UnityEngine.Random.value > 0.5f; // Placeholder
        }

        private bool HasBannedItem(KingdomSnapshot snapshot, string banned)
        {
            return false; // Placeholder
        }

        public void VoteForKingdom(string submissionId, string categoryId, int score)
        {
            // Score from 1-10, gets converted to category points
            Debug.Log($"👍 Voted {score}/10 for {categoryId} on submission {submissionId}");
        }

        public void VisitKingdom(string submissionId)
        {
            // Increase popularity score
            Debug.Log($"👀 Visited kingdom: {submissionId}");
        }

        // Placeholder methods - would integrate with actual KingdomManager
        private List<BuildingData> GetAllBuildings()
        {
            return new List<BuildingData>();
        }

        private List<DecorationData> GetAllDecorations()
        {
            return new List<DecorationData>();
        }

        private string GetLayoutData()
        {
            return "{}"; // JSON layout data
        }

        private KingdomStatistics CalculateStatistics()
        {
            return new KingdomStatistics
            {
                totalBuildings = 0,
                totalDecorations = 0,
                areaUsed = 0,
                uniqueItemTypes = 0,
                colorVariety = 0
            };
        }

        // Public accessors
        public KingdomTheme GetCurrentTheme() => _currentTheme;
        public List<KingdomTheme> GetAvailableThemes() => _availableThemes;
        public List<ScoringCategory> GetScoringCategories() => _scoringCategories;
    }

    #region Kingdom Challenge Data Classes

    [Serializable]
    public class KingdomTheme
    {
        public string themeId;
        public string displayName;
        public string description;
        public string[] requirements;
        public string[] bonusPoints;
        public string[] bannedItems;
        public Sprite themeIcon;
        public Color themeColor;
    }

    public enum JudgeType
    {
        Community,  // Players vote
        Automatic,  // System calculates
        Staff       // Moderators judge
    }

    [Serializable]
    public class ScoringCategory
    {
        public string categoryId;
        public string displayName;
        public string description;
        public int maxPoints;
        public JudgeType judgedBy;
    }

    [Serializable]
    public class KingdomSnapshot
    {
        public string snapshotId;
        public DateTime capturedAt;
        public List<BuildingData> buildings;
        public List<DecorationData> decorations;
        public string layout;
        public KingdomStatistics statistics;
        public string thumbnailPath;
    }

    [Serializable]
    public class BuildingData
    {
        public string buildingId;
        public string buildingType;
        public Vector3 position;
        public Quaternion rotation;
        public int upgradeLevel;
    }

    [Serializable]
    public class DecorationData
    {
        public string decorationId;
        public string decorationType;
        public Vector3 position;
        public Quaternion rotation;
        public Color customColor;
    }

    [Serializable]
    public class KingdomStatistics
    {
        public int totalBuildings;
        public int totalDecorations;
        public float areaUsed;
        public int uniqueItemTypes;
        public int colorVariety;
        public int villagerCount;
        public int visitorCount;
    }

    [Serializable]
    public class KingdomSubmission
    {
        public string submissionId;
        public string playerId;
        public string playerName;
        public string kingdomName;
        public string description;
        public string themeId;
        public KingdomSnapshot snapshot;
        public DateTime submittedAt;
        public Dictionary<string, float> scores;
        public int totalVotes;
        public int visitCount;
    }

    #endregion
}

