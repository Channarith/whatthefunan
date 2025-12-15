using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using WhatTheFunan.Core;

namespace WhatTheFunan.RPG
{
    /// <summary>
    /// Manages NPC dialogues with branching conversations and choices.
    /// Supports localization, portraits, and dialogue events.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        #region Singleton
        private static DialogueSystem _instance;
        public static DialogueSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Dialogue> OnDialogueStarted;
        public static event Action OnDialogueEnded;
        public static event Action<DialogueLine> OnLineDisplayed;
        public static event Action<DialogueChoice> OnChoiceMade;
        public static event Action<string> OnDialogueEvent;
        #endregion

        #region Dialogue Data
        [Header("Dialogue Database")]
        [SerializeField] private List<Dialogue> _dialogues = new List<Dialogue>();
        
        private Dictionary<string, Dialogue> _dialogueLookup = new Dictionary<string, Dialogue>();
        
        // Current dialogue state
        private Dialogue _currentDialogue;
        private DialogueNode _currentNode;
        private int _currentLineIndex;
        
        public bool IsInDialogue => _currentDialogue != null;
        public Dialogue CurrentDialogue => _currentDialogue;
        public DialogueLine CurrentLine => _currentNode?.lines.ElementAtOrDefault(_currentLineIndex);
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private float _textSpeed = 50f; // Characters per second
        [SerializeField] private float _autoAdvanceDelay = 2f;
        [SerializeField] private bool _skipOnTap = true;
        #endregion

        #region Dialogue History
        private List<string> _viewedDialogueIds = new List<string>();
        private Dictionary<string, string> _choiceHistory = new Dictionary<string, string>();
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
            
            InitializeDialogues();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void InitializeDialogues()
        {
            _dialogueLookup.Clear();
            foreach (var dialogue in _dialogues)
            {
                if (!string.IsNullOrEmpty(dialogue.dialogueId))
                {
                    _dialogueLookup[dialogue.dialogueId] = dialogue;
                }
            }
        }
        #endregion

        #region Dialogue Control
        /// <summary>
        /// Start a dialogue by ID.
        /// </summary>
        public bool StartDialogue(string dialogueId)
        {
            if (!_dialogueLookup.TryGetValue(dialogueId, out Dialogue dialogue))
            {
                Debug.LogWarning($"[DialogueSystem] Dialogue not found: {dialogueId}");
                return false;
            }
            
            return StartDialogue(dialogue);
        }

        /// <summary>
        /// Start a dialogue.
        /// </summary>
        public bool StartDialogue(Dialogue dialogue)
        {
            if (dialogue == null || dialogue.nodes.Count == 0)
            {
                Debug.LogWarning("[DialogueSystem] Invalid dialogue");
                return false;
            }
            
            if (IsInDialogue)
            {
                Debug.LogWarning("[DialogueSystem] Already in dialogue");
                return false;
            }
            
            _currentDialogue = dialogue;
            _currentNode = dialogue.nodes[0];
            _currentLineIndex = 0;
            
            // Mark as viewed
            if (!_viewedDialogueIds.Contains(dialogue.dialogueId))
            {
                _viewedDialogueIds.Add(dialogue.dialogueId);
            }
            
            // Notify game manager
            GameManager.Instance?.EnterDialogue();
            
            OnDialogueStarted?.Invoke(dialogue);
            DisplayCurrentLine();
            
            Debug.Log($"[DialogueSystem] Started dialogue: {dialogue.dialogueId}");
            return true;
        }

        /// <summary>
        /// Advance to the next line or choice.
        /// </summary>
        public void Advance()
        {
            if (!IsInDialogue) return;
            
            // Check if there are more lines in current node
            if (_currentLineIndex < _currentNode.lines.Count - 1)
            {
                _currentLineIndex++;
                DisplayCurrentLine();
            }
            else
            {
                // End of node - check for choices or next node
                if (_currentNode.choices.Count > 0)
                {
                    // Wait for player choice
                    // UI should display choices
                }
                else if (!string.IsNullOrEmpty(_currentNode.nextNodeId))
                {
                    // Go to next node
                    GoToNode(_currentNode.nextNodeId);
                }
                else
                {
                    // End dialogue
                    EndDialogue();
                }
            }
        }

        /// <summary>
        /// Select a dialogue choice.
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (!IsInDialogue || _currentNode == null) return;
            if (choiceIndex < 0 || choiceIndex >= _currentNode.choices.Count) return;
            
            var choice = _currentNode.choices[choiceIndex];
            
            // Record choice for branching story
            string choiceKey = $"{_currentDialogue.dialogueId}_{_currentNode.nodeId}";
            _choiceHistory[choiceKey] = choice.choiceId;
            
            OnChoiceMade?.Invoke(choice);
            
            // Trigger choice event if any
            if (!string.IsNullOrEmpty(choice.eventId))
            {
                TriggerEvent(choice.eventId);
            }
            
            // Go to next node based on choice
            if (!string.IsNullOrEmpty(choice.nextNodeId))
            {
                GoToNode(choice.nextNodeId);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Skip to end of current line (instant display).
        /// </summary>
        public void SkipToLineEnd()
        {
            // UI should handle this - show full text immediately
        }

        /// <summary>
        /// End the current dialogue.
        /// </summary>
        public void EndDialogue()
        {
            if (!IsInDialogue) return;
            
            // Trigger end event
            if (!string.IsNullOrEmpty(_currentDialogue.endEventId))
            {
                TriggerEvent(_currentDialogue.endEventId);
            }
            
            var endedDialogue = _currentDialogue;
            
            _currentDialogue = null;
            _currentNode = null;
            _currentLineIndex = 0;
            
            // Notify game manager
            GameManager.Instance?.ExitDialogue();
            
            OnDialogueEnded?.Invoke();
            
            Debug.Log($"[DialogueSystem] Ended dialogue: {endedDialogue.dialogueId}");
        }

        private void GoToNode(string nodeId)
        {
            var node = _currentDialogue.nodes.FirstOrDefault(n => n.nodeId == nodeId);
            if (node == null)
            {
                Debug.LogWarning($"[DialogueSystem] Node not found: {nodeId}");
                EndDialogue();
                return;
            }
            
            _currentNode = node;
            _currentLineIndex = 0;
            
            // Trigger node event
            if (!string.IsNullOrEmpty(node.eventId))
            {
                TriggerEvent(node.eventId);
            }
            
            DisplayCurrentLine();
        }

        private void DisplayCurrentLine()
        {
            var line = CurrentLine;
            if (line == null)
            {
                Advance();
                return;
            }
            
            OnLineDisplayed?.Invoke(line);
            
            // Haptic feedback on important lines
            if (line.isImportant)
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }
        }

        private void TriggerEvent(string eventId)
        {
            OnDialogueEvent?.Invoke(eventId);
            
            // Handle built-in events
            switch (eventId)
            {
                case "start_quest":
                    // Parse quest ID from event parameters
                    break;
                case "give_item":
                    // Give item to player
                    break;
                case "unlock_codex":
                    // Unlock codex entry
                    break;
            }
            
            Debug.Log($"[DialogueSystem] Event triggered: {eventId}");
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Check if a dialogue has been viewed.
        /// </summary>
        public bool HasViewedDialogue(string dialogueId)
        {
            return _viewedDialogueIds.Contains(dialogueId);
        }

        /// <summary>
        /// Get a previous choice made in a dialogue.
        /// </summary>
        public string GetPreviousChoice(string dialogueId, string nodeId)
        {
            string key = $"{dialogueId}_{nodeId}";
            return _choiceHistory.GetValueOrDefault(key, null);
        }

        /// <summary>
        /// Get dialogue by ID.
        /// </summary>
        public Dialogue GetDialogue(string dialogueId)
        {
            return _dialogueLookup.GetValueOrDefault(dialogueId, null);
        }

        /// <summary>
        /// Get current choices if available.
        /// </summary>
        public List<DialogueChoice> GetCurrentChoices()
        {
            if (!IsInDialogue || _currentNode == null) return new List<DialogueChoice>();
            if (_currentLineIndex < _currentNode.lines.Count - 1) return new List<DialogueChoice>();
            return _currentNode.choices;
        }
        #endregion

        #region Save/Load
        public DialogueSaveData GetSaveData()
        {
            return new DialogueSaveData
            {
                viewedDialogueIds = new List<string>(_viewedDialogueIds),
                choiceHistory = new Dictionary<string, string>(_choiceHistory)
            };
        }

        public void LoadSaveData(DialogueSaveData data)
        {
            _viewedDialogueIds = new List<string>(data.viewedDialogueIds);
            _choiceHistory = new Dictionary<string, string>(data.choiceHistory);
        }

        [Serializable]
        public class DialogueSaveData
        {
            public List<string> viewedDialogueIds;
            public Dictionary<string, string> choiceHistory;
        }
        #endregion
    }

    #region Dialogue Data Classes
    [Serializable]
    public class Dialogue
    {
        [Header("Identity")]
        public string dialogueId;
        public string speakerName;
        public Sprite speakerPortrait;
        
        [Header("Nodes")]
        public List<DialogueNode> nodes = new List<DialogueNode>();
        
        [Header("Events")]
        public string startEventId;
        public string endEventId;
        
        [Header("Requirements")]
        public List<string> requiredQuestIds = new List<string>();
        public string requiredChoiceId;
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public List<DialogueLine> lines = new List<DialogueLine>();
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public string nextNodeId;
        public string eventId;
    }

    [Serializable]
    public class DialogueLine
    {
        public string speakerName;          // Override speaker for this line
        public Sprite speakerPortrait;      // Override portrait
        [TextArea] public string text;
        public string voiceClipId;
        public string animationTrigger;
        public float delay = 0f;            // Delay before showing
        public bool isImportant = false;    // Highlight important lines
        public string emotion;              // happy, sad, angry, surprised, etc.
    }

    [Serializable]
    public class DialogueChoice
    {
        public string choiceId;
        public string text;
        public string nextNodeId;
        public string eventId;
        public string requiredQuestId;      // Only show if quest active
        public string requiredItemId;       // Only show if player has item
        public bool isHidden = false;       // Hidden until conditions met
        
        // For branching story
        public string storyFlagId;
        public string storyFlagValue;
    }
    #endregion
}

