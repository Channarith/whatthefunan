using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Challenges
{
    /// <summary>
    /// ADDITIONAL FUN CHALLENGES! 🎮
    /// More mini-games and competitions to keep players engaged!
    /// </summary>

    // ============================================================================
    // DANCE CHALLENGE - Rhythm-based dancing competition!
    // ============================================================================
    public class DanceChallenge : MonoBehaviour
    {
        public static DanceChallenge Instance { get; private set; }

        [Header("Dance Settings")]
        [SerializeField] private List<DanceRoutine> _availableRoutines = new List<DanceRoutine>();
        [SerializeField] private DanceRoutine _currentRoutine;
        [SerializeField] private float _perfectTiming = 0.1f;
        [SerializeField] private float _goodTiming = 0.25f;

        private int _perfectCount;
        private int _goodCount;
        private int _missCount;
        private int _combo;
        private int _maxCombo;
        private float _totalScore;

        public event Action<DanceRating> OnDanceMove;
        public event Action<DanceResult> OnDanceComplete;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDances();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDances()
        {
            _availableRoutines = new List<DanceRoutine>
            {
                new DanceRoutine
                {
                    routineId = "apsara_classic",
                    displayName = "🌸 Classic Apsara",
                    description = "Elegant celestial dance! Slow and graceful!",
                    difficulty = DanceDifficulty.Easy,
                    bpm = 80,
                    moves = new DanceMove[] { DanceMove.HandGesture, DanceMove.SlowSpin, DanceMove.Bow }
                },
                new DanceRoutine
                {
                    routineId = "monkey_funk",
                    displayName = "🐵 Kavi's Monkey Funk",
                    description = "Silly monkey dance! Jump and wiggle!",
                    difficulty = DanceDifficulty.Medium,
                    bpm = 120,
                    moves = new DanceMove[] { DanceMove.Jump, DanceMove.ArmWave, DanceMove.ButtWiggle }
                },
                new DanceRoutine
                {
                    routineId = "elephant_stomp",
                    displayName = "🐘 Champa's Elephant Stomp",
                    description = "BOOM BOOM! Stomp your feet like a happy elephant!",
                    difficulty = DanceDifficulty.Easy,
                    bpm = 90,
                    moves = new DanceMove[] { DanceMove.Stomp, DanceMove.TrunkWave, DanceMove.Spin }
                },
                new DanceRoutine
                {
                    routineId = "naga_slither",
                    displayName = "🐍 Naga Slither Groove",
                    description = "Smooth and hypnotic! Wave like the seven-headed serpent!",
                    difficulty = DanceDifficulty.Hard,
                    bpm = 100,
                    moves = new DanceMove[] { DanceMove.Wave, DanceMove.Slither, DanceMove.HeadBob }
                },
                new DanceRoutine
                {
                    routineId = "silly_disco",
                    displayName = "🕺 MAXIMUM SILLY DISCO",
                    description = "No rules! Just be as silly as possible!",
                    difficulty = DanceDifficulty.Any,
                    bpm = 140,
                    moves = new DanceMove[] { DanceMove.Random, DanceMove.Random, DanceMove.Random }
                }
            };
        }

        public void StartDance(string routineId)
        {
            _currentRoutine = _availableRoutines.Find(r => r.routineId == routineId);
            if (_currentRoutine == null) return;

            _perfectCount = 0;
            _goodCount = 0;
            _missCount = 0;
            _combo = 0;
            _maxCombo = 0;
            _totalScore = 0;

            Debug.Log($"💃 Starting dance: {_currentRoutine.displayName}");
        }

        public void RegisterMove(float timing)
        {
            DanceRating rating;

            if (Mathf.Abs(timing) <= _perfectTiming)
            {
                rating = DanceRating.Perfect;
                _perfectCount++;
                _combo++;
                _totalScore += 100 * (1 + _combo * 0.1f);
            }
            else if (Mathf.Abs(timing) <= _goodTiming)
            {
                rating = DanceRating.Good;
                _goodCount++;
                _combo++;
                _totalScore += 50 * (1 + _combo * 0.05f);
            }
            else
            {
                rating = DanceRating.Miss;
                _missCount++;
                _combo = 0;
            }

            _maxCombo = Mathf.Max(_maxCombo, _combo);
            OnDanceMove?.Invoke(rating);
        }

        public DanceResult FinishDance()
        {
            var result = new DanceResult
            {
                routineId = _currentRoutine.routineId,
                score = _totalScore,
                perfectCount = _perfectCount,
                goodCount = _goodCount,
                missCount = _missCount,
                maxCombo = _maxCombo,
                grade = CalculateGrade()
            };

            Debug.Log($"🎉 Dance complete! Score: {result.score}, Grade: {result.grade}");
            OnDanceComplete?.Invoke(result);
            return result;
        }

        private string CalculateGrade()
        {
            float accuracy = (float)(_perfectCount + _goodCount) / (_perfectCount + _goodCount + _missCount);
            if (_missCount == 0) return "S+ PERFECT! 🌟";
            if (accuracy >= 0.95f) return "S";
            if (accuracy >= 0.9f) return "A";
            if (accuracy >= 0.8f) return "B";
            if (accuracy >= 0.7f) return "C";
            return "Try Again! 💪";
        }
    }

    // ============================================================================
    // COOKING COMPETITION - Who makes the best dish?
    // ============================================================================
    public class CookingCompetition : MonoBehaviour
    {
        public static CookingCompetition Instance { get; private set; }

        [Header("Competition Settings")]
        [SerializeField] private List<CompetitionDish> _competitionDishes = new List<CompetitionDish>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeDishes();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDishes()
        {
            _competitionDishes = new List<CompetitionDish>
            {
                new CompetitionDish
                {
                    dishId = "amok_fish",
                    displayName = "🐟 Fish Amok",
                    description = "The KING of Cambodian food! Creamy coconut curry!",
                    difficulty = 3,
                    maxPoints = 100,
                    ingredients = new string[] { "Fish", "Coconut Milk", "Kroeung Paste", "Banana Leaf" },
                    secretIngredient = "Love! 💕",
                    perfectTip = "Steam it gently! Don't rush the curry!"
                },
                new CompetitionDish
                {
                    dishId = "lok_lak",
                    displayName = "🥩 Beef Lok Lak",
                    description = "Sizzling beef with lime pepper sauce! TASTY!",
                    difficulty = 2,
                    maxPoints = 80,
                    ingredients = new string[] { "Beef", "Lime", "Black Pepper", "Lettuce", "Tomato" },
                    secretIngredient = "Extra crispy edges!",
                    perfectTip = "High heat! Fast cooking!"
                },
                new CompetitionDish
                {
                    dishId = "num_banh_chok",
                    displayName = "🍜 Num Banh Chok",
                    description = "Khmer noodles with fish gravy! Traditional breakfast!",
                    difficulty = 4,
                    maxPoints = 120,
                    ingredients = new string[] { "Rice Noodles", "Fish Gravy", "Cucumber", "Bean Sprouts", "Mint" },
                    secretIngredient = "Fresh morning herbs!",
                    perfectTip = "Make the gravy smooth and fragrant!"
                },
                new CompetitionDish
                {
                    dishId = "kuy_teav",
                    displayName = "🍲 Kuy Teav",
                    description = "Hearty noodle soup! Ultimate comfort food!",
                    difficulty = 2,
                    maxPoints = 70,
                    ingredients = new string[] { "Noodles", "Pork", "Broth", "Bean Sprouts", "Green Onion" },
                    secretIngredient = "Crispy fried garlic on top!",
                    perfectTip = "The broth is everything!"
                },
                new CompetitionDish
                {
                    dishId = "bai_sach_chrouk",
                    displayName = "🐷 Bai Sach Chrouk",
                    description = "Grilled pork with rice! Simple but AMAZING!",
                    difficulty = 1,
                    maxPoints = 60,
                    ingredients = new string[] { "Pork", "Rice", "Pickled Vegetables", "Egg" },
                    secretIngredient = "Charcoal grill flavor!",
                    perfectTip = "Marinate overnight!"
                },
                new CompetitionDish
                {
                    dishId = "silly_disaster",
                    displayName = "🤪 Mystery CHAOS Dish!",
                    description = "Random ingredients! Create something... unique!",
                    difficulty = 5,
                    maxPoints = 200,
                    ingredients = new string[] { "???", "???", "???", "Chaos" },
                    secretIngredient = "Pure imagination!",
                    perfectTip = "There are no rules! BE CREATIVE!"
                }
            };
        }

        public CookingResult JudgeDish(string dishId, CookingAttempt attempt)
        {
            var dish = _competitionDishes.Find(d => d.dishId == dishId);
            if (dish == null) return null;

            var result = new CookingResult
            {
                dishId = dishId,
                tasteScore = UnityEngine.Random.Range(60, 100),
                presentationScore = UnityEngine.Random.Range(60, 100),
                creativityScore = UnityEngine.Random.Range(60, 100),
                funnyComments = GetFunnyJudgeComments()
            };

            result.totalScore = (result.tasteScore + result.presentationScore + result.creativityScore) / 3f;

            return result;
        }

        private string[] GetFunnyJudgeComments()
        {
            return new string[]
            {
                "🧑‍🍳 Chef Kavi: 'I tried to steal a bite! SO GOOD!'",
                "👸 Princess Champa: 'Smells like happiness!'",
                "🐍 Naga Prince: 'All seven of my heads approve!'",
                "🤣 'Did you... did you put a whoopee cushion in the soup?!'",
                "😋 'My tummy is SO HAPPY right now!'"
            };
        }
    }

    // ============================================================================
    // PHOTO CONTEST - Best screenshots!
    // ============================================================================
    public class PhotoContest : MonoBehaviour
    {
        public static PhotoContest Instance { get; private set; }

        [Header("Photo Categories")]
        [SerializeField] private List<PhotoCategory> _categories = new List<PhotoCategory>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeCategories();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeCategories()
        {
            _categories = new List<PhotoCategory>
            {
                new PhotoCategory
                {
                    categoryId = "funniest",
                    displayName = "😂 FUNNIEST Photo",
                    description = "Make us laugh! Silly faces, weird angles, comedy gold!",
                    tips = new string[] { "Catch characters mid-sneeze!", "Zoom in on funny faces!", "Timing is everything!" }
                },
                new PhotoCategory
                {
                    categoryId = "prettiest",
                    displayName = "😍 Most BEAUTIFUL Photo",
                    description = "Capture the beauty of Funan! Sunsets, temples, nature!",
                    tips = new string[] { "Golden hour lighting!", "Rule of thirds!", "Find hidden spots!" }
                },
                new PhotoCategory
                {
                    categoryId = "action",
                    displayName = "💥 Best ACTION Shot",
                    description = "Mid-battle! Mid-dance! Mid-CHAOS! Freeze the action!",
                    tips = new string[] { "Pause at the right moment!", "Get multiple angles!", "Capture abilities!" }
                },
                new PhotoCategory
                {
                    categoryId = "friendship",
                    displayName = "💕 Best FRIENDSHIP Photo",
                    description = "Characters together! Wholesome moments! Friend goals!",
                    tips = new string[] { "Group poses!", "Candid moments!", "Matching outfits!" }
                },
                new PhotoCategory
                {
                    categoryId = "weird",
                    displayName = "🤔 WEIRDEST Photo",
                    description = "Glitches, strange angles, things that make you go 'huh?'",
                    tips = new string[] { "Find the bugs!", "Unusual perspectives!", "Embrace the chaos!" }
                }
            };
        }

        public void SubmitPhoto(string categoryId, Texture2D photo, string caption)
        {
            Debug.Log($"📸 Photo submitted to {categoryId}: '{caption}'");
        }
    }

    // ============================================================================
    // TREASURE HUNT - Find hidden items!
    // ============================================================================
    public class TreasureHuntChallenge : MonoBehaviour
    {
        public static TreasureHuntChallenge Instance { get; private set; }

        [Header("Hunt Settings")]
        [SerializeField] private List<TreasureClue> _currentClues = new List<TreasureClue>();
        private int _foundCount;
        private int _totalTreasures;

        public event Action<TreasureClue> OnClueRevealed;
        public event Action<int, int> OnTreasureFound; // found, total

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

        public void StartNewHunt(string huntId)
        {
            _foundCount = 0;
            _currentClues = GenerateClues(huntId);
            _totalTreasures = _currentClues.Count;

            Debug.Log($"🗺️ Treasure Hunt started! {_totalTreasures} treasures to find!");
        }

        private List<TreasureClue> GenerateClues(string huntId)
        {
            return new List<TreasureClue>
            {
                new TreasureClue
                {
                    clueId = "clue_1",
                    riddleText = "I stand where two rivers meet, guarding secrets at Funan's feet! 🌊",
                    hint = "Look near the water...",
                    locationDescription = "River confluence shrine"
                },
                new TreasureClue
                {
                    clueId = "clue_2",
                    riddleText = "Seven heads watch over me, in shadows deep I hold the key! 🐍",
                    hint = "The Naga knows...",
                    locationDescription = "Naga statue garden"
                },
                new TreasureClue
                {
                    clueId = "clue_3",
                    riddleText = "Where dancers spin and music plays, I hide in rhythmic, joyful ways! 💃",
                    hint = "Follow the music...",
                    locationDescription = "Dance pavilion"
                },
                new TreasureClue
                {
                    clueId = "clue_4",
                    riddleText = "Roots above and stones below, where temples crumble, treasures grow! 🌳",
                    hint = "Ancient and overgrown...",
                    locationDescription = "Ta Prohm-style ruins"
                },
                new TreasureClue
                {
                    clueId = "clue_5",
                    riddleText = "PFFFT! Hehe! Look where it's FUNNY! 💨😂",
                    hint = "Kavi probably hid this one...",
                    locationDescription = "Near the whoopee cushion traps"
                }
            };
        }

        public void FoundTreasure(string clueId)
        {
            _foundCount++;
            Debug.Log($"🎉 Treasure found! {_foundCount}/{_totalTreasures}");
            OnTreasureFound?.Invoke(_foundCount, _totalTreasures);
        }
    }

    #region Supporting Data Classes

    public enum DanceDifficulty { Easy, Medium, Hard, Expert, Any }
    public enum DanceMove { HandGesture, SlowSpin, Bow, Jump, ArmWave, ButtWiggle, Stomp, TrunkWave, Spin, Wave, Slither, HeadBob, Random }
    public enum DanceRating { Perfect, Good, Miss }

    [Serializable]
    public class DanceRoutine
    {
        public string routineId;
        public string displayName;
        public string description;
        public DanceDifficulty difficulty;
        public float bpm;
        public DanceMove[] moves;
        public AudioClip music;
    }

    [Serializable]
    public class DanceResult
    {
        public string routineId;
        public float score;
        public int perfectCount;
        public int goodCount;
        public int missCount;
        public int maxCombo;
        public string grade;
    }

    [Serializable]
    public class CompetitionDish
    {
        public string dishId;
        public string displayName;
        public string description;
        public int difficulty; // 1-5
        public int maxPoints;
        public string[] ingredients;
        public string secretIngredient;
        public string perfectTip;
        public Sprite dishImage;
    }

    [Serializable]
    public class CookingAttempt
    {
        public string[] usedIngredients;
        public float cookingTime;
        public int stirCount;
        public bool followedRecipe;
    }

    [Serializable]
    public class CookingResult
    {
        public string dishId;
        public float tasteScore;
        public float presentationScore;
        public float creativityScore;
        public float totalScore;
        public string[] funnyComments;
    }

    [Serializable]
    public class PhotoCategory
    {
        public string categoryId;
        public string displayName;
        public string description;
        public string[] tips;
    }

    [Serializable]
    public class TreasureClue
    {
        public string clueId;
        public string riddleText;
        public string hint;
        public string locationDescription;
        public Vector3 treasurePosition;
        public bool isFound;
    }

    #endregion
}

