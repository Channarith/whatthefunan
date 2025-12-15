using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using WhatTheFunan.Core;

namespace WhatTheFunan.MiniGames
{
    /// <summary>
    /// Apsara Dance rhythm game featuring traditional Cambodian music.
    /// Tap, hold, and swipe to the beat of traditional instruments.
    /// </summary>
    public class RhythmGame : MonoBehaviour
    {
        #region Events
        public static event Action OnGameStarted;
        public static event Action OnGameEnded;
        public static event Action<Song> OnSongStarted;
        public static event Action<Song, int, int> OnSongCompleted; // Song, score, maxScore
        public static event Action<RhythmNote> OnNoteSpawned;
        public static event Action<RhythmNote, NoteResult> OnNoteHit;
        public static event Action<int> OnComboChanged;
        #endregion

        #region Game State
        public enum RhythmState
        {
            Idle,
            SelectingSong,
            Playing,
            Paused,
            Completed
        }

        [SerializeField] private RhythmState _state = RhythmState.Idle;
        public RhythmState State => _state;
        public bool IsPlaying => _state == RhythmState.Playing;
        #endregion

        #region Song Database
        [Header("Songs")]
        [SerializeField] private List<Song> _songs = new List<Song>();
        
        private List<string> _unlockedSongIds = new List<string>();
        public IReadOnlyList<Song> Songs => _songs;
        #endregion

        #region Current Session
        private Song _currentSong;
        private AudioSource _musicSource;
        private float _songTime;
        private int _currentNoteIndex;
        private List<RhythmNote> _activeNotes = new List<RhythmNote>();
        
        public Song CurrentSong => _currentSong;
        public float SongProgress => _currentSong != null ? _songTime / _currentSong.clip.length : 0;
        #endregion

        #region Scoring
        private int _score;
        private int _combo;
        private int _maxCombo;
        private int _perfectCount;
        private int _goodCount;
        private int _missCount;
        
        public int Score => _score;
        public int Combo => _combo;
        public int MaxCombo => _maxCombo;
        #endregion

        #region Settings
        [Header("Timing Windows (seconds)")]
        [SerializeField] private float _perfectWindow = 0.05f;
        [SerializeField] private float _goodWindow = 0.1f;
        [SerializeField] private float _okayWindow = 0.15f;
        
        [Header("Note Settings")]
        [SerializeField] private float _noteSpawnTime = 2f; // How early notes appear
        [SerializeField] private int _laneCount = 4;
        
        [Header("Scoring")]
        [SerializeField] private int _perfectScore = 100;
        [SerializeField] private int _goodScore = 50;
        [SerializeField] private int _okayScore = 25;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            
            // Unlock first song
            if (_songs.Count > 0 && _unlockedSongIds.Count == 0)
            {
                _unlockedSongIds.Add(_songs[0].songId);
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;
            
            _songTime += Time.deltaTime;
            
            SpawnUpcomingNotes();
            UpdateActiveNotes();
            CheckForMissedNotes();
            
            // Check for song end
            if (_songTime >= _currentSong.clip.length)
            {
                CompleteSong();
            }
        }
        #endregion

        #region Game Flow
        /// <summary>
        /// Start the rhythm game.
        /// </summary>
        public void StartGame()
        {
            if (_state != RhythmState.Idle) return;
            
            _state = RhythmState.SelectingSong;
            GameManager.Instance?.EnterMiniGame();
            OnGameStarted?.Invoke();
            
            Debug.Log("[RhythmGame] Session started");
        }

        /// <summary>
        /// Select and start a song.
        /// </summary>
        public bool SelectSong(string songId)
        {
            if (_state != RhythmState.SelectingSong) return false;
            
            var song = _songs.Find(s => s.songId == songId);
            if (song == null)
            {
                Debug.LogWarning($"[RhythmGame] Song not found: {songId}");
                return false;
            }
            
            if (!_unlockedSongIds.Contains(songId))
            {
                Debug.LogWarning($"[RhythmGame] Song locked: {songId}");
                return false;
            }
            
            StartSong(song);
            return true;
        }

        private void StartSong(Song song)
        {
            _currentSong = song;
            _songTime = 0;
            _currentNoteIndex = 0;
            _activeNotes.Clear();
            
            // Reset scoring
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _perfectCount = 0;
            _goodCount = 0;
            _missCount = 0;
            
            // Start music
            _musicSource.clip = song.clip;
            _musicSource.Play();
            
            _state = RhythmState.Playing;
            OnSongStarted?.Invoke(song);
            
            Debug.Log($"[RhythmGame] Playing: {song.songName}");
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void Pause()
        {
            if (_state != RhythmState.Playing) return;
            
            _state = RhythmState.Paused;
            _musicSource.Pause();
            Time.timeScale = 0;
        }

        /// <summary>
        /// Resume the game.
        /// </summary>
        public void Resume()
        {
            if (_state != RhythmState.Paused) return;
            
            _state = RhythmState.Playing;
            _musicSource.UnPause();
            Time.timeScale = 1;
        }

        private void CompleteSong()
        {
            _state = RhythmState.Completed;
            _musicSource.Stop();
            
            int maxPossibleScore = _currentSong.notes.Count * _perfectScore;
            
            OnSongCompleted?.Invoke(_currentSong, _score, maxPossibleScore);
            
            // Calculate stars (1-3)
            float scorePercent = (float)_score / maxPossibleScore;
            int stars = scorePercent >= 0.9f ? 3 : (scorePercent >= 0.7f ? 2 : 1);
            
            GrantRewards(stars);
            UnlockNewSongs(stars);
            
            HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Success);
            
            Debug.Log($"[RhythmGame] Song complete! Score: {_score}/{maxPossibleScore}");
        }

        /// <summary>
        /// End the session.
        /// </summary>
        public void EndGame()
        {
            _state = RhythmState.Idle;
            _musicSource.Stop();
            _currentSong = null;
            _activeNotes.Clear();
            Time.timeScale = 1;
            
            GameManager.Instance?.ExitMiniGame();
            OnGameEnded?.Invoke();
            
            Debug.Log("[RhythmGame] Session ended");
        }
        #endregion

        #region Note Management
        private void SpawnUpcomingNotes()
        {
            while (_currentNoteIndex < _currentSong.notes.Count)
            {
                var noteData = _currentSong.notes[_currentNoteIndex];
                
                if (noteData.time - _songTime <= _noteSpawnTime)
                {
                    SpawnNote(noteData);
                    _currentNoteIndex++;
                }
                else
                {
                    break;
                }
            }
        }

        private void SpawnNote(NoteData data)
        {
            var note = new RhythmNote
            {
                data = data,
                spawnTime = _songTime,
                isActive = true
            };
            
            _activeNotes.Add(note);
            OnNoteSpawned?.Invoke(note);
        }

        private void UpdateActiveNotes()
        {
            // Notes are updated visually by UI
        }

        private void CheckForMissedNotes()
        {
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                var note = _activeNotes[i];
                if (!note.isActive) continue;
                
                float noteTime = note.data.time;
                
                if (_songTime > noteTime + _okayWindow)
                {
                    // Missed!
                    RegisterNoteResult(note, NoteResult.Miss);
                }
            }
        }
        #endregion

        #region Input
        /// <summary>
        /// Player taps a lane.
        /// </summary>
        public void TapLane(int lane)
        {
            if (!IsPlaying) return;
            
            // Find closest note in this lane
            RhythmNote closestNote = null;
            float closestDiff = float.MaxValue;
            
            foreach (var note in _activeNotes)
            {
                if (!note.isActive) continue;
                if (note.data.lane != lane) continue;
                if (note.data.type == NoteType.Hold) continue; // Hold notes handled separately
                
                float diff = Mathf.Abs(note.data.time - _songTime);
                if (diff < closestDiff && diff <= _okayWindow)
                {
                    closestDiff = diff;
                    closestNote = note;
                }
            }
            
            if (closestNote != null)
            {
                NoteResult result = GetNoteResult(closestDiff);
                RegisterNoteResult(closestNote, result);
            }
        }

        /// <summary>
        /// Player starts holding a lane.
        /// </summary>
        public void StartHold(int lane)
        {
            // TODO: Implement hold note mechanics
        }

        /// <summary>
        /// Player releases a lane.
        /// </summary>
        public void ReleaseHold(int lane)
        {
            // TODO: Implement hold note mechanics
        }

        /// <summary>
        /// Player swipes.
        /// </summary>
        public void Swipe(SwipeDirection direction)
        {
            if (!IsPlaying) return;
            
            // Find swipe note
            foreach (var note in _activeNotes)
            {
                if (!note.isActive) continue;
                if (note.data.type != NoteType.Swipe) continue;
                if (note.data.swipeDirection != direction) continue;
                
                float diff = Mathf.Abs(note.data.time - _songTime);
                if (diff <= _okayWindow)
                {
                    NoteResult result = GetNoteResult(diff);
                    RegisterNoteResult(note, result);
                    break;
                }
            }
        }

        private NoteResult GetNoteResult(float timeDiff)
        {
            if (timeDiff <= _perfectWindow) return NoteResult.Perfect;
            if (timeDiff <= _goodWindow) return NoteResult.Good;
            if (timeDiff <= _okayWindow) return NoteResult.Okay;
            return NoteResult.Miss;
        }

        private void RegisterNoteResult(RhythmNote note, NoteResult result)
        {
            note.isActive = false;
            
            switch (result)
            {
                case NoteResult.Perfect:
                    _score += _perfectScore * (1 + _combo / 10);
                    _combo++;
                    _perfectCount++;
                    HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
                    break;
                    
                case NoteResult.Good:
                    _score += _goodScore * (1 + _combo / 10);
                    _combo++;
                    _goodCount++;
                    HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
                    break;
                    
                case NoteResult.Okay:
                    _score += _okayScore;
                    _combo++;
                    break;
                    
                case NoteResult.Miss:
                    _combo = 0;
                    _missCount++;
                    HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Warning);
                    break;
            }
            
            if (_combo > _maxCombo)
            {
                _maxCombo = _combo;
            }
            
            OnNoteHit?.Invoke(note, result);
            OnComboChanged?.Invoke(_combo);
        }
        #endregion

        #region Rewards
        private void GrantRewards(int stars)
        {
            int coins = _currentSong.baseReward * stars;
            Economy.CurrencyManager.Instance?.AddCoins(coins);
            
            // Unlock dance emote based on performance
            if (stars >= 2)
            {
                // CollectionManager.Instance?.UnlockDance(_currentSong.unlockDanceId);
            }
        }

        private void UnlockNewSongs(int stars)
        {
            if (stars < 2) return;
            
            int currentIndex = _songs.IndexOf(_currentSong);
            if (currentIndex >= 0 && currentIndex < _songs.Count - 1)
            {
                string nextSongId = _songs[currentIndex + 1].songId;
                if (!_unlockedSongIds.Contains(nextSongId))
                {
                    _unlockedSongIds.Add(nextSongId);
                    Debug.Log($"[RhythmGame] Unlocked new song: {nextSongId}");
                }
            }
        }
        #endregion
    }

    #region Rhythm Data Classes
    public enum NoteType
    {
        Tap,
        Hold,
        Swipe
    }

    public enum NoteResult
    {
        Perfect,
        Good,
        Okay,
        Miss
    }

    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [Serializable]
    public class Song
    {
        public string songId;
        public string songName;
        public string artistName;
        public AudioClip clip;
        public Sprite coverArt;
        
        [Header("Difficulty")]
        public int difficulty; // 1-5
        public float bpm;
        
        [Header("Notes")]
        public List<NoteData> notes = new List<NoteData>();
        
        [Header("Rewards")]
        public int baseReward = 50;
        public string unlockDanceId;
        
        [Header("Cultural Info")]
        [TextArea] public string culturalNote; // About the music/instruments
    }

    [Serializable]
    public class NoteData
    {
        public float time;          // When the note should be hit
        public int lane;            // Which lane (0 to laneCount-1)
        public NoteType type;
        public float holdDuration;   // For hold notes
        public SwipeDirection swipeDirection; // For swipe notes
    }

    public class RhythmNote
    {
        public NoteData data;
        public float spawnTime;
        public bool isActive;
    }
    #endregion
}

