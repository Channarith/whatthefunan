using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace WhatTheFunan.Core
{
    /// <summary>
    /// Central game manager that controls game state, initialization, and core systems.
    /// Singleton pattern ensures only one instance exists throughout the game.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action OnGameInitialized;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<GameState> OnGameStateChanged;
        #endregion

        #region Game State
        public enum GameState
        {
            Initializing,
            MainMenu,
            Loading,
            Playing,
            Paused,
            InCombat,
            InCutscene,
            InDialogue,
            InMiniGame,
            GameOver
        }

        [SerializeField] private GameState _currentState = GameState.Initializing;
        public GameState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnGameStateChanged?.Invoke(_currentState);
                    Debug.Log($"[GameManager] State changed to: {_currentState}");
                }
            }
        }
        #endregion

        #region Settings
        [Header("Game Settings")]
        [SerializeField] private bool _showDisclaimer = true;
        [SerializeField] private string _gameVersion = "0.1.0";
        [SerializeField] private string _buildNumber = "1";

        public string GameVersion => _gameVersion;
        public string FullVersion => $"{_gameVersion}+{_buildNumber}";
        #endregion

        #region Runtime Data
        public bool IsInitialized { get; private set; }
        public bool IsPaused => CurrentState == GameState.Paused;
        public float PlayTime { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize core systems
            InitializeGame();
        }

        private void Start()
        {
            // Show disclaimer on first launch (legal requirement)
            if (_showDisclaimer && !PlayerPrefs.HasKey("DisclaimerAccepted"))
            {
                ShowDisclaimer();
            }
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.InCombat)
            {
                PlayTime += Time.deltaTime;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentState == GameState.Playing)
            {
                PauseGame();
                SaveSystem.Instance?.QuickSave();
            }
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Instance?.QuickSave();
        }
        #endregion

        #region Initialization
        private void InitializeGame()
        {
            Debug.Log($"[GameManager] Initializing What the Funan v{FullVersion}");
            
            CurrentState = GameState.Initializing;
            
            // Set target frame rate for mobile
            Application.targetFrameRate = 60;
            
            // Prevent screen from sleeping
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            // Initialize random seed
            UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
            
            IsInitialized = true;
            CurrentState = GameState.MainMenu;
            
            OnGameInitialized?.Invoke();
            Debug.Log("[GameManager] Initialization complete");
        }
        #endregion

        #region Game State Control
        public void StartGame()
        {
            CurrentState = GameState.Playing;
            Debug.Log("[GameManager] Game started");
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing || CurrentState == GameState.InCombat)
            {
                CurrentState = GameState.Paused;
                Time.timeScale = 0f;
                OnGamePaused?.Invoke();
                Debug.Log("[GameManager] Game paused");
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
                Time.timeScale = 1f;
                OnGameResumed?.Invoke();
                Debug.Log("[GameManager] Game resumed");
            }
        }

        public void EnterCombat()
        {
            CurrentState = GameState.InCombat;
        }

        public void ExitCombat()
        {
            CurrentState = GameState.Playing;
        }

        public void EnterCutscene()
        {
            CurrentState = GameState.InCutscene;
        }

        public void ExitCutscene()
        {
            CurrentState = GameState.Playing;
        }

        public void EnterDialogue()
        {
            CurrentState = GameState.InDialogue;
        }

        public void ExitDialogue()
        {
            CurrentState = GameState.Playing;
        }

        public void EnterMiniGame()
        {
            CurrentState = GameState.InMiniGame;
        }

        public void ExitMiniGame()
        {
            CurrentState = GameState.Playing;
        }

        public void GameOver()
        {
            CurrentState = GameState.GameOver;
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            CurrentState = GameState.MainMenu;
            SceneController.Instance?.LoadScene("MainMenu");
        }
        #endregion

        #region Disclaimer (Legal Requirement)
        private void ShowDisclaimer()
        {
            // Display the legally required disclaimer
            string disclaimer = @"'What the Funan' is a work of fiction inspired by the ancient Funan Kingdom (1st-9th Century CE). All characters, events, and locations are fictional. Any resemblance to real historical figures is coincidental. This game celebrates the shared cultural heritage of Southeast Asia and is created with respect for all cultures and traditions depicted.";
            
            Debug.Log($"[GameManager] Disclaimer: {disclaimer}");
            
            // TODO: Show disclaimer UI panel
            // For now, mark as accepted
            PlayerPrefs.SetInt("DisclaimerAccepted", 1);
            PlayerPrefs.Save();
        }
        #endregion

        #region Utility
        public void QuitGame()
        {
            SaveSystem.Instance?.QuickSave();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion
    }
}

