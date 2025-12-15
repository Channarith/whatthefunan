using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WhatTheFunan.Core;

namespace WhatTheFunan.MiniGames
{
    /// <summary>
    /// Cooking mini-game featuring traditional Funan/Cambodian dishes.
    /// Combine ingredients, follow recipes, and timing-based cooking.
    /// </summary>
    public class CookingGame : MonoBehaviour
    {
        #region Events
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;
        public static event Action<Recipe> OnRecipeSelected;
        public static event Action<Ingredient> OnIngredientAdded;
        public static event Action<CookingStep> OnStepStarted;
        public static event Action<CookingStep, float> OnStepProgress;
        public static event Action<CookingStep, bool> OnStepCompleted;
        public static event Action<Recipe, int> OnDishCompleted; // Recipe, quality (1-3 stars)
        #endregion

        #region Game State
        public enum CookingState
        {
            Idle,
            SelectingRecipe,
            Preparing,
            Cooking,
            Completed,
            Failed
        }

        [SerializeField] private CookingState _state = CookingState.Idle;
        public CookingState State => _state;
        public bool IsPlaying => _state != CookingState.Idle;
        #endregion

        #region Recipe Database
        [Header("Recipes")]
        [SerializeField] private List<Recipe> _recipes = new List<Recipe>();
        
        private Dictionary<string, Recipe> _recipeLookup = new Dictionary<string, Recipe>();
        private List<string> _unlockedRecipeIds = new List<string>();
        
        public IReadOnlyList<Recipe> Recipes => _recipes;
        public IReadOnlyList<string> UnlockedRecipeIds => _unlockedRecipeIds;
        #endregion

        #region Ingredient Database
        [Header("Ingredients")]
        [SerializeField] private List<Ingredient> _ingredients = new List<Ingredient>();
        
        private Dictionary<string, Ingredient> _ingredientLookup = new Dictionary<string, Ingredient>();
        #endregion

        #region Current Cooking Session
        private Recipe _currentRecipe;
        private int _currentStepIndex;
        private float _stepTimer;
        private float _stepAccuracy;
        private List<float> _stepScores = new List<float>();
        private List<string> _addedIngredients = new List<string>();
        
        public Recipe CurrentRecipe => _currentRecipe;
        public CookingStep CurrentStep => _currentRecipe?.steps.ElementAtOrDefault(_currentStepIndex);
        public float StepProgress => CurrentStep != null ? _stepTimer / CurrentStep.duration : 0;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private float _perfectWindow = 0.1f; // 10% window for perfect timing
        [SerializeField] private float _goodWindow = 0.25f;    // 25% window for good timing
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeDatabases();
        }

        private void InitializeDatabases()
        {
            _recipeLookup.Clear();
            foreach (var recipe in _recipes)
            {
                _recipeLookup[recipe.recipeId] = recipe;
            }
            
            _ingredientLookup.Clear();
            foreach (var ingredient in _ingredients)
            {
                _ingredientLookup[ingredient.ingredientId] = ingredient;
            }
            
            // Unlock starter recipes
            if (_recipes.Count > 0 && _unlockedRecipeIds.Count == 0)
            {
                _unlockedRecipeIds.Add(_recipes[0].recipeId);
            }
        }
        #endregion

        #region Game Flow
        /// <summary>
        /// Start the cooking mini-game.
        /// </summary>
        public void StartCooking()
        {
            if (IsPlaying) return;
            
            _state = CookingState.SelectingRecipe;
            GameManager.Instance?.EnterMiniGame();
            OnGameStarted?.Invoke();
            
            Debug.Log("[CookingGame] Cooking session started");
        }

        /// <summary>
        /// Select a recipe to cook.
        /// </summary>
        public bool SelectRecipe(string recipeId)
        {
            if (_state != CookingState.SelectingRecipe) return false;
            
            if (!_recipeLookup.TryGetValue(recipeId, out Recipe recipe))
            {
                Debug.LogWarning($"[CookingGame] Recipe not found: {recipeId}");
                return false;
            }
            
            if (!_unlockedRecipeIds.Contains(recipeId))
            {
                Debug.LogWarning($"[CookingGame] Recipe locked: {recipeId}");
                return false;
            }
            
            _currentRecipe = recipe;
            _currentStepIndex = 0;
            _stepScores.Clear();
            _addedIngredients.Clear();
            
            _state = CookingState.Preparing;
            OnRecipeSelected?.Invoke(recipe);
            
            Debug.Log($"[CookingGame] Selected recipe: {recipe.recipeName}");
            return true;
        }

        /// <summary>
        /// Add an ingredient during preparation.
        /// </summary>
        public bool AddIngredient(string ingredientId)
        {
            if (_state != CookingState.Preparing) return false;
            
            if (!_ingredientLookup.TryGetValue(ingredientId, out Ingredient ingredient))
            {
                Debug.LogWarning($"[CookingGame] Ingredient not found: {ingredientId}");
                return false;
            }
            
            // Check if this ingredient is needed
            if (!_currentRecipe.requiredIngredients.Contains(ingredientId))
            {
                Debug.Log($"[CookingGame] Ingredient not in recipe: {ingredient.ingredientName}");
                // Adding wrong ingredients affects quality
                _stepScores.Add(0.5f);
            }
            else if (_addedIngredients.Contains(ingredientId))
            {
                Debug.Log($"[CookingGame] Ingredient already added: {ingredient.ingredientName}");
                return false;
            }
            else
            {
                _addedIngredients.Add(ingredientId);
                _stepScores.Add(1f);
            }
            
            OnIngredientAdded?.Invoke(ingredient);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            
            // Check if all ingredients added
            if (_addedIngredients.Count >= _currentRecipe.requiredIngredients.Count)
            {
                StartCookingSteps();
            }
            
            return true;
        }

        /// <summary>
        /// Start the cooking steps after ingredients are added.
        /// </summary>
        private void StartCookingSteps()
        {
            _state = CookingState.Cooking;
            _currentStepIndex = 0;
            StartCurrentStep();
        }

        /// <summary>
        /// Start the current cooking step.
        /// </summary>
        private void StartCurrentStep()
        {
            var step = CurrentStep;
            if (step == null)
            {
                CompleteDish();
                return;
            }
            
            _stepTimer = 0f;
            _stepAccuracy = 0f;
            
            OnStepStarted?.Invoke(step);
            StartCoroutine(StepRoutine(step));
            
            Debug.Log($"[CookingGame] Step started: {step.instruction}");
        }

        private IEnumerator StepRoutine(CookingStep step)
        {
            while (_stepTimer < step.duration)
            {
                _stepTimer += Time.deltaTime;
                OnStepProgress?.Invoke(step, StepProgress);
                yield return null;
            }
            
            // Auto-complete if player didn't tap at right time
            CompleteStep(false);
        }

        /// <summary>
        /// Player input during a cooking step (e.g., tap at right moment).
        /// </summary>
        public void StepInput()
        {
            if (_state != CookingState.Cooking) return;
            
            var step = CurrentStep;
            if (step == null) return;
            
            // Calculate timing accuracy
            float targetTime = step.duration * step.targetTiming;
            float timingDiff = Mathf.Abs(_stepTimer - targetTime) / step.duration;
            
            if (timingDiff <= _perfectWindow)
            {
                _stepAccuracy = 1f;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            }
            else if (timingDiff <= _goodWindow)
            {
                _stepAccuracy = 0.75f;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Medium);
            }
            else
            {
                _stepAccuracy = 0.5f;
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }
            
            CompleteStep(true);
        }

        private void CompleteStep(bool playerInput)
        {
            StopAllCoroutines();
            
            float score = playerInput ? _stepAccuracy : 0.25f;
            _stepScores.Add(score);
            
            OnStepCompleted?.Invoke(CurrentStep, score >= 0.75f);
            
            _currentStepIndex++;
            
            if (_currentStepIndex < _currentRecipe.steps.Count)
            {
                StartCurrentStep();
            }
            else
            {
                CompleteDish();
            }
        }

        private void CompleteDish()
        {
            _state = CookingState.Completed;
            
            // Calculate overall quality (1-3 stars)
            float avgScore = _stepScores.Count > 0 ? _stepScores.Average() : 0.5f;
            int stars = avgScore >= 0.9f ? 3 : (avgScore >= 0.7f ? 2 : 1);
            
            OnDishCompleted?.Invoke(_currentRecipe, stars);
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            
            // Grant rewards based on stars
            GrantRewards(stars);
            
            // Unlock new recipes based on completion
            UnlockNewRecipes();
            
            Debug.Log($"[CookingGame] Completed: {_currentRecipe.recipeName} with {stars} stars!");
            
            // Return to idle after delay
            StartCoroutine(ResetAfterDelay(3f));
        }

        private IEnumerator ResetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _state = CookingState.Idle;
            _currentRecipe = null;
        }

        /// <summary>
        /// End the cooking session.
        /// </summary>
        public void EndCooking()
        {
            StopAllCoroutines();
            _state = CookingState.Idle;
            _currentRecipe = null;
            
            GameManager.Instance?.ExitMiniGame();
            OnGameEnded?.Invoke();
            
            Debug.Log("[CookingGame] Cooking session ended");
        }
        #endregion

        #region Rewards
        private void GrantRewards(int stars)
        {
            int coins = _currentRecipe.baseReward * stars;
            Economy.CurrencyManager.Instance?.AddCoins(coins);
            
            // Add to recipe collection
            // CollectionManager.Instance?.UnlockRecipe(_currentRecipe.recipeId);
        }

        private void UnlockNewRecipes()
        {
            // Unlock next recipe in progression
            int currentIndex = _recipes.IndexOf(_currentRecipe);
            if (currentIndex >= 0 && currentIndex < _recipes.Count - 1)
            {
                string nextRecipeId = _recipes[currentIndex + 1].recipeId;
                if (!_unlockedRecipeIds.Contains(nextRecipeId))
                {
                    _unlockedRecipeIds.Add(nextRecipeId);
                    Debug.Log($"[CookingGame] Unlocked new recipe: {nextRecipeId}");
                }
            }
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Get available (unlocked) recipes.
        /// </summary>
        public List<Recipe> GetAvailableRecipes()
        {
            return _recipes.Where(r => _unlockedRecipeIds.Contains(r.recipeId)).ToList();
        }

        /// <summary>
        /// Check if a recipe is unlocked.
        /// </summary>
        public bool IsRecipeUnlocked(string recipeId)
        {
            return _unlockedRecipeIds.Contains(recipeId);
        }
        #endregion
    }

    #region Cooking Data Classes
    [Serializable]
    public class Recipe
    {
        public string recipeId;
        public string recipeName;
        [TextArea] public string description;
        public Sprite icon;
        public Sprite dishImage;
        
        [Header("Requirements")]
        public List<string> requiredIngredients = new List<string>();
        
        [Header("Cooking Steps")]
        public List<CookingStep> steps = new List<CookingStep>();
        
        [Header("Rewards")]
        public int baseReward = 50;
        public string buffId;       // Temporary buff when consumed
        public float buffDuration;
        
        [Header("Cultural Info")]
        [TextArea] public string culturalNote; // Educational content about the dish
    }

    [Serializable]
    public class CookingStep
    {
        public string stepId;
        public string instruction;
        public StepType type;
        public float duration;
        [Range(0f, 1f)] public float targetTiming; // When to tap (0-1)
        public Sprite stepImage;
    }

    public enum StepType
    {
        Chop,
        Stir,
        Flip,
        Season,
        Heat,
        Wait
    }

    [Serializable]
    public class Ingredient
    {
        public string ingredientId;
        public string ingredientName;
        public Sprite icon;
        public IngredientCategory category;
    }

    public enum IngredientCategory
    {
        Protein,
        Vegetable,
        Spice,
        Grain,
        Sauce,
        Fruit
    }
    #endregion
}

