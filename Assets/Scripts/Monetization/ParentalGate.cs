using UnityEngine;
using System;

namespace WhatTheFunan.Monetization
{
    /// <summary>
    /// Parental gate to prevent accidental purchases by children.
    /// Uses simple math problems or gestures.
    /// Required for COPPA compliance.
    /// </summary>
    public class ParentalGate : MonoBehaviour
    {
        #region Singleton
        private static ParentalGate _instance;
        public static ParentalGate Instance => _instance;
        #endregion

        #region Events
        public static event Action<ParentalGateChallenge> OnChallengePresented;
        public static event Action OnGatePassed;
        public static event Action OnGateFailed;
        public static event Action OnGateCancelled;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private GateType _gateType = GateType.MathProblem;
        [SerializeField] private int _maxAttempts = 3;
        [SerializeField] private float _lockoutDuration = 60f; // seconds
        
        public enum GateType
        {
            MathProblem,    // "What is 7 + 5?"
            DateOfBirth,    // "Enter your birth year"
            TextChallenge   // "Type 'I AM A PARENT'"
        }
        #endregion

        #region State
        private int _failedAttempts;
        private float _lockoutEndTime;
        private Action _onSuccess;
        private Action _onFailure;
        private ParentalGateChallenge _currentChallenge;
        
        public bool IsLocked => Time.time < _lockoutEndTime;
        public float LockoutRemaining => Mathf.Max(0, _lockoutEndTime - Time.time);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Gate Flow
        /// <summary>
        /// Show the parental gate.
        /// </summary>
        public void ShowGate(Action onSuccess, Action onFailure = null)
        {
            if (IsLocked)
            {
                Debug.LogWarning($"[ParentalGate] Locked for {LockoutRemaining:F0} more seconds");
                onFailure?.Invoke();
                return;
            }
            
            _onSuccess = onSuccess;
            _onFailure = onFailure;
            
            _currentChallenge = GenerateChallenge();
            OnChallengePresented?.Invoke(_currentChallenge);
            
            Debug.Log($"[ParentalGate] Challenge: {_currentChallenge.question}");
        }

        /// <summary>
        /// Submit an answer to the parental gate.
        /// </summary>
        public void SubmitAnswer(string answer)
        {
            if (_currentChallenge == null)
            {
                Debug.LogWarning("[ParentalGate] No active challenge");
                return;
            }
            
            bool isCorrect = ValidateAnswer(answer);
            
            if (isCorrect)
            {
                _failedAttempts = 0;
                _currentChallenge = null;
                
                OnGatePassed?.Invoke();
                _onSuccess?.Invoke();
                
                // Notify IAP manager
                IAPManager.Instance?.OnParentalGatePassed();
                
                Debug.Log("[ParentalGate] Gate passed!");
            }
            else
            {
                _failedAttempts++;
                
                if (_failedAttempts >= _maxAttempts)
                {
                    // Lock out
                    _lockoutEndTime = Time.time + _lockoutDuration;
                    _failedAttempts = 0;
                    _currentChallenge = null;
                    
                    OnGateFailed?.Invoke();
                    _onFailure?.Invoke();
                    
                    IAPManager.Instance?.OnParentalGateFailed();
                    
                    Debug.Log($"[ParentalGate] Too many failures. Locked for {_lockoutDuration}s");
                }
                else
                {
                    // Allow retry
                    Debug.Log($"[ParentalGate] Wrong answer. {_maxAttempts - _failedAttempts} attempts remaining");
                }
            }
        }

        /// <summary>
        /// Cancel the parental gate.
        /// </summary>
        public void Cancel()
        {
            _currentChallenge = null;
            OnGateCancelled?.Invoke();
            _onFailure?.Invoke();
            
            IAPManager.Instance?.OnParentalGateFailed();
            
            Debug.Log("[ParentalGate] Cancelled");
        }
        #endregion

        #region Challenge Generation
        private ParentalGateChallenge GenerateChallenge()
        {
            switch (_gateType)
            {
                case GateType.MathProblem:
                    return GenerateMathChallenge();
                    
                case GateType.DateOfBirth:
                    return GenerateBirthYearChallenge();
                    
                case GateType.TextChallenge:
                    return GenerateTextChallenge();
                    
                default:
                    return GenerateMathChallenge();
            }
        }

        private ParentalGateChallenge GenerateMathChallenge()
        {
            // Generate a harder math problem that kids can't easily solve
            int a = UnityEngine.Random.Range(10, 50);
            int b = UnityEngine.Random.Range(10, 50);
            int operation = UnityEngine.Random.Range(0, 3);
            
            string question;
            int answer;
            
            switch (operation)
            {
                case 0: // Addition
                    question = $"What is {a} + {b}?";
                    answer = a + b;
                    break;
                case 1: // Subtraction (ensure positive result)
                    if (a < b) (a, b) = (b, a);
                    question = $"What is {a} - {b}?";
                    answer = a - b;
                    break;
                case 2: // Multiplication
                    a = UnityEngine.Random.Range(3, 12);
                    b = UnityEngine.Random.Range(3, 12);
                    question = $"What is {a} × {b}?";
                    answer = a * b;
                    break;
                default:
                    question = $"What is {a} + {b}?";
                    answer = a + b;
                    break;
            }
            
            return new ParentalGateChallenge
            {
                type = GateType.MathProblem,
                question = question,
                correctAnswer = answer.ToString(),
                hint = "Solve this math problem to continue."
            };
        }

        private ParentalGateChallenge GenerateBirthYearChallenge()
        {
            int currentYear = DateTime.Now.Year;
            int minValidYear = currentYear - 100;
            int maxValidYear = currentYear - 18; // Must be 18+
            
            return new ParentalGateChallenge
            {
                type = GateType.DateOfBirth,
                question = "Enter your birth year:",
                minValue = minValidYear,
                maxValue = maxValidYear,
                hint = "You must be 18 or older to make purchases."
            };
        }

        private ParentalGateChallenge GenerateTextChallenge()
        {
            return new ParentalGateChallenge
            {
                type = GateType.TextChallenge,
                question = "Type the following: I AM A PARENT",
                correctAnswer = "I AM A PARENT",
                hint = "Type the exact phrase shown above."
            };
        }
        #endregion

        #region Validation
        private bool ValidateAnswer(string answer)
        {
            if (_currentChallenge == null) return false;
            
            switch (_currentChallenge.type)
            {
                case GateType.MathProblem:
                    return answer.Trim() == _currentChallenge.correctAnswer;
                    
                case GateType.DateOfBirth:
                    if (int.TryParse(answer, out int year))
                    {
                        return year >= _currentChallenge.minValue && 
                               year <= _currentChallenge.maxValue;
                    }
                    return false;
                    
                case GateType.TextChallenge:
                    return answer.Trim().ToUpper() == _currentChallenge.correctAnswer.ToUpper();
                    
                default:
                    return false;
            }
        }
        #endregion
    }

    #region Challenge Data
    [Serializable]
    public class ParentalGateChallenge
    {
        public ParentalGate.GateType type;
        public string question;
        public string correctAnswer;
        public int minValue;
        public int maxValue;
        public string hint;
    }
    #endregion
}

