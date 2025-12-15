using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Tutorial
{
    /// <summary>
    /// Manages tutorial and onboarding experience.
    /// Guides new players through game mechanics step by step.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        #region Singleton
        private static TutorialSystem _instance;
        public static TutorialSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<TutorialStep> OnStepStarted;
        public static event Action<TutorialStep> OnStepCompleted;
        public static event Action<TutorialSequence> OnSequenceStarted;
        public static event Action<TutorialSequence> OnSequenceCompleted;
        public static event Action OnTutorialComplete;
        #endregion

        #region Tutorial Data
        [Header("Tutorial Sequences")]
        [SerializeField] private List<TutorialSequence> _sequences = new List<TutorialSequence>();
        
        private Dictionary<string, TutorialSequence> _sequenceLookup = new Dictionary<string, TutorialSequence>();
        private HashSet<string> _completedSequences = new HashSet<string>();
        private HashSet<string> _completedSteps = new HashSet<string>();
        
        private TutorialSequence _currentSequence;
        private int _currentStepIndex;
        private bool _isActive;
        
        public bool IsActive => _isActive;
        public TutorialStep CurrentStep => _currentSequence?.steps[_currentStepIndex];
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private bool _autoStartTutorial = true;
        [SerializeField] private bool _canSkipTutorial = true;
        [SerializeField] private float _highlightPulseSpeed = 2f;
        #endregion

        #region UI References
        [Header("UI")]
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private TMPro.TextMeshProUGUI _instructionText;
        [SerializeField] private GameObject _highlightPrefab;
        [SerializeField] private GameObject _handPointerPrefab;
        [SerializeField] private UnityEngine.UI.Button _skipButton;
        [SerializeField] private UnityEngine.UI.Button _nextButton;
        
        private GameObject _currentHighlight;
        private GameObject _currentPointer;
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
            
            InitializeTutorials();
            LoadProgress();
        }

        private void Start()
        {
            if (_autoStartTutorial && !HasCompletedInitialTutorial())
            {
                StartSequence("intro");
            }
            
            SetupButtons();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeTutorials()
        {
            _sequenceLookup.Clear();
            foreach (var sequence in _sequences)
            {
                _sequenceLookup[sequence.sequenceId] = sequence;
            }
        }

        private void SetupButtons()
        {
            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(SkipTutorial);
                _skipButton.gameObject.SetActive(_canSkipTutorial);
            }
            
            if (_nextButton != null)
            {
                _nextButton.onClick.AddListener(CompleteCurrentStep);
            }
        }
        #endregion

        #region Sequence Management
        /// <summary>
        /// Start a tutorial sequence.
        /// </summary>
        public bool StartSequence(string sequenceId)
        {
            if (_isActive)
            {
                Debug.LogWarning("[TutorialSystem] Tutorial already active");
                return false;
            }
            
            if (!_sequenceLookup.TryGetValue(sequenceId, out TutorialSequence sequence))
            {
                Debug.LogWarning($"[TutorialSystem] Sequence not found: {sequenceId}");
                return false;
            }
            
            if (_completedSequences.Contains(sequenceId) && !sequence.canReplay)
            {
                Debug.Log($"[TutorialSystem] Sequence already completed: {sequenceId}");
                return false;
            }
            
            _currentSequence = sequence;
            _currentStepIndex = 0;
            _isActive = true;
            
            // Pause game during tutorial
            Core.GameManager.Instance?.PauseGame();
            
            ShowTutorialUI();
            OnSequenceStarted?.Invoke(sequence);
            
            StartStep(_currentSequence.steps[0]);
            
            Debug.Log($"[TutorialSystem] Started sequence: {sequenceId}");
            return true;
        }

        /// <summary>
        /// Complete the current tutorial sequence.
        /// </summary>
        public void CompleteSequence()
        {
            if (!_isActive) return;
            
            _completedSequences.Add(_currentSequence.sequenceId);
            SaveProgress();
            
            OnSequenceCompleted?.Invoke(_currentSequence);
            
            // Check if this was the last tutorial
            if (_currentSequence.sequenceId == "intro")
            {
                OnTutorialComplete?.Invoke();
            }
            
            // Start next sequence if specified
            string nextSequence = _currentSequence.nextSequenceId;
            
            HideTutorialUI();
            ClearHighlights();
            
            _currentSequence = null;
            _isActive = false;
            
            // Resume game
            Core.GameManager.Instance?.ResumeGame();
            
            if (!string.IsNullOrEmpty(nextSequence))
            {
                StartSequence(nextSequence);
            }
            
            Debug.Log("[TutorialSystem] Sequence completed");
        }

        /// <summary>
        /// Skip the current tutorial.
        /// </summary>
        public void SkipTutorial()
        {
            if (!_isActive || !_canSkipTutorial) return;
            
            // Mark as completed
            _completedSequences.Add(_currentSequence.sequenceId);
            SaveProgress();
            
            HideTutorialUI();
            ClearHighlights();
            
            _currentSequence = null;
            _isActive = false;
            
            Core.GameManager.Instance?.ResumeGame();
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            Debug.Log("[TutorialSystem] Tutorial skipped");
        }
        #endregion

        #region Step Management
        private void StartStep(TutorialStep step)
        {
            if (step == null) return;
            
            // Update UI
            if (_instructionText != null)
            {
                _instructionText.text = step.instruction;
            }
            
            // Show next button based on step type
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(step.requiresManualAdvance);
            }
            
            // Highlight target if specified
            if (!string.IsNullOrEmpty(step.highlightTargetId))
            {
                HighlightElement(step.highlightTargetId);
            }
            
            // Show pointer if specified
            if (step.showPointer && !string.IsNullOrEmpty(step.highlightTargetId))
            {
                ShowPointer(step.highlightTargetId);
            }
            
            OnStepStarted?.Invoke(step);
            
            // Auto-advance after delay if specified
            if (step.autoAdvanceDelay > 0)
            {
                StartCoroutine(AutoAdvanceCoroutine(step.autoAdvanceDelay));
            }
            
            Debug.Log($"[TutorialSystem] Step started: {step.stepId}");
        }

        /// <summary>
        /// Complete the current step and move to next.
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (!_isActive) return;
            
            var step = CurrentStep;
            if (step == null) return;
            
            _completedSteps.Add(step.stepId);
            OnStepCompleted?.Invoke(step);
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            // Clear current highlights
            ClearHighlights();
            
            // Move to next step
            _currentStepIndex++;
            
            if (_currentStepIndex < _currentSequence.steps.Count)
            {
                StartStep(_currentSequence.steps[_currentStepIndex]);
            }
            else
            {
                CompleteSequence();
            }
        }

        /// <summary>
        /// Called when a tutorial trigger is activated.
        /// </summary>
        public void OnTriggerActivated(string triggerId)
        {
            if (!_isActive) return;
            
            var step = CurrentStep;
            if (step == null) return;
            
            if (step.triggerType == TutorialTrigger.Custom && step.triggerId == triggerId)
            {
                CompleteCurrentStep();
            }
        }

        /// <summary>
        /// Called when player performs an action.
        /// </summary>
        public void OnActionPerformed(TutorialTrigger action)
        {
            if (!_isActive) return;
            
            var step = CurrentStep;
            if (step == null) return;
            
            if (step.triggerType == action)
            {
                CompleteCurrentStep();
            }
        }

        private IEnumerator AutoAdvanceCoroutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            
            if (_isActive && CurrentStep != null && !CurrentStep.requiresManualAdvance)
            {
                CompleteCurrentStep();
            }
        }
        #endregion

        #region Highlighting
        private void HighlightElement(string elementId)
        {
            // TODO: Find UI element by ID and highlight it
            // For now, just log
            Debug.Log($"[TutorialSystem] Highlighting: {elementId}");
            
            // Create highlight effect
            // _currentHighlight = Instantiate(_highlightPrefab);
            // Position over target element
        }

        private void ShowPointer(string targetId)
        {
            // TODO: Show animated hand pointer
            Debug.Log($"[TutorialSystem] Showing pointer to: {targetId}");
        }

        private void ClearHighlights()
        {
            if (_currentHighlight != null)
            {
                Destroy(_currentHighlight);
                _currentHighlight = null;
            }
            
            if (_currentPointer != null)
            {
                Destroy(_currentPointer);
                _currentPointer = null;
            }
        }
        #endregion

        #region UI
        private void ShowTutorialUI()
        {
            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(true);
            }
        }

        private void HideTutorialUI()
        {
            if (_tutorialPanel != null)
            {
                _tutorialPanel.SetActive(false);
            }
        }
        #endregion

        #region Query
        /// <summary>
        /// Check if initial tutorial is complete.
        /// </summary>
        public bool HasCompletedInitialTutorial()
        {
            return _completedSequences.Contains("intro");
        }

        /// <summary>
        /// Check if a specific sequence is complete.
        /// </summary>
        public bool IsSequenceComplete(string sequenceId)
        {
            return _completedSequences.Contains(sequenceId);
        }

        /// <summary>
        /// Reset tutorial progress.
        /// </summary>
        public void ResetProgress()
        {
            _completedSequences.Clear();
            _completedSteps.Clear();
            SaveProgress();
            Debug.Log("[TutorialSystem] Progress reset");
        }
        #endregion

        #region Save/Load
        private void SaveProgress()
        {
            string completed = string.Join(",", _completedSequences);
            PlayerPrefs.SetString("Tutorial_Completed", completed);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            string completed = PlayerPrefs.GetString("Tutorial_Completed", "");
            if (!string.IsNullOrEmpty(completed))
            {
                _completedSequences = new HashSet<string>(completed.Split(','));
            }
        }
        #endregion
    }

    #region Tutorial Data Classes
    public enum TutorialTrigger
    {
        Manual,         // Player taps next button
        Tap,            // Player taps anywhere
        Move,           // Player moves character
        Attack,         // Player attacks
        Jump,           // Player jumps
        Interact,       // Player interacts with object
        OpenMenu,       // Player opens a menu
        CollectItem,    // Player picks up item
        Custom          // Custom trigger ID
    }

    [Serializable]
    public class TutorialSequence
    {
        public string sequenceId;
        public string sequenceName;
        public List<TutorialStep> steps = new List<TutorialStep>();
        public bool canReplay = false;
        public string nextSequenceId;
    }

    [Serializable]
    public class TutorialStep
    {
        public string stepId;
        [TextArea] public string instruction;
        
        [Header("Trigger")]
        public TutorialTrigger triggerType = TutorialTrigger.Manual;
        public string triggerId;
        public bool requiresManualAdvance = true;
        public float autoAdvanceDelay = 0f;
        
        [Header("Highlighting")]
        public string highlightTargetId;
        public bool showPointer = false;
        
        [Header("Audio")]
        public AudioClip voiceOver;
        public string sfxId;
    }
    #endregion
}

