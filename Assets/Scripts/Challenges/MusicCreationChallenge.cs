using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Challenges
{
    /// <summary>
    /// MUSIC CREATION CHALLENGE! 🎵
    /// Let players create their own Khmer-inspired music using traditional instruments!
    /// </summary>
    public class MusicCreationChallenge : MonoBehaviour
    {
        public static MusicCreationChallenge Instance { get; private set; }

        [Header("Music Creation Settings")]
        [SerializeField] private int _beatsPerMeasure = 4;
        [SerializeField] private int _totalMeasures = 8;
        [SerializeField] private float _bpm = 120f;

        [Header("Available Instruments")]
        [SerializeField] private List<InstrumentDefinition> _instruments = new List<InstrumentDefinition>();

        [Header("Current Composition")]
        private MusicComposition _currentComposition;
        private bool _isPlaying;
        private float _playbackTime;
        private int _currentBeat;

        // Events
        public event Action<int> OnBeatPlayed;
        public event Action OnCompositionFinished;
        public event Action<MusicComposition> OnCompositionSaved;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeInstruments();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeInstruments()
        {
            _instruments = new List<InstrumentDefinition>
            {
                // ============================================================
                // TRADITIONAL KHMER INSTRUMENTS (with silly sounds!)
                // ============================================================
                
                // DRUMS - The heartbeat of Khmer music!
                new InstrumentDefinition
                {
                    instrumentId = "skor_thom",
                    displayName = "🥁 Skor Thom (Big Drum)",
                    description = "BOOM BOOM! The big bass drum! Makes your tummy rumble!",
                    category = InstrumentCategory.Drums,
                    sounds = new string[] { "boom", "bam", "thud", "KABOOM" },
                    sillyMode = "Sounds like elephant tummy rumbles! BRRRRUMBLE!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "skor_touch",
                    displayName = "🪘 Skor Touch (Small Drum)",
                    description = "Tap tap tap! Quick little beats like monkey feet!",
                    category = InstrumentCategory.Drums,
                    sounds = new string[] { "tap", "tik", "tok", "bop" },
                    sillyMode = "Sounds like Kavi doing a happy dance! TAP TAP TAP!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "chhing",
                    displayName = "🔔 Chhing (Finger Cymbals)",
                    description = "TING TING! Shiny little cymbals that go 'ching'!",
                    category = InstrumentCategory.Percussion,
                    sounds = new string[] { "ting", "ching", "ping", "ring" },
                    sillyMode = "Like fairy bells if fairies were REALLY LOUD!"
                },

                // GONGS - Big beautiful sounds!
                new InstrumentDefinition
                {
                    instrumentId = "kong_thom",
                    displayName = "🔔 Kong Thom (Big Gong)",
                    description = "GOOOOONG! The big beautiful gong! Feel the vibes!",
                    category = InstrumentCategory.Gongs,
                    sounds = new string[] { "GONG", "BWAAANG", "DOOOONG" },
                    sillyMode = "Sounds like Yak's tummy after too many snacks!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "kong_touch",
                    displayName = "🎐 Kong Touch (Small Gong Set)",
                    description = "A row of gongs! Play melodies that float through the air!",
                    category = InstrumentCategory.Gongs,
                    sounds = new string[] { "dong", "dang", "ding", "dung", "deng" },
                    sillyMode = "Like musical hiccups! DING DONG DING!"
                },

                // XYLOPHONES - Wooden melodies!
                new InstrumentDefinition
                {
                    instrumentId = "roneat_ek",
                    displayName = "🎹 Roneat Ek (High Xylophone)",
                    description = "Tippy tappy wooden keys! High and happy sounds!",
                    category = InstrumentCategory.Xylophone,
                    sounds = new string[] { "tink", "tonk", "plonk", "pink" },
                    sillyMode = "Sounds like raindrops on a silly roof!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "roneat_thung",
                    displayName = "🎹 Roneat Thung (Low Xylophone)",
                    description = "Deep wooden sounds like a wise old tree talking!",
                    category = InstrumentCategory.Xylophone,
                    sounds = new string[] { "bonk", "clonk", "thunk", "plunk" },
                    sillyMode = "Prohm's footsteps turned into music!"
                },

                // STRING INSTRUMENTS - Flowy and beautiful!
                new InstrumentDefinition
                {
                    instrumentId = "chapei",
                    displayName = "🎸 Chapei (Long-Necked Lute)",
                    description = "Storyteller's instrument! Pluck pluck goes the strings!",
                    category = InstrumentCategory.Strings,
                    sounds = new string[] { "twang", "plink", "strum", "pluck" },
                    sillyMode = "Like a cat walking on guitar strings! TWAAANG!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "tro_sau",
                    displayName = "🎻 Tro Sau (Two-String Fiddle)",
                    description = "Swoopy sounds that make you feel feelings!",
                    category = InstrumentCategory.Strings,
                    sounds = new string[] { "swoop", "sweep", "screech", "song" },
                    sillyMode = "Naga's seven heads trying to sing together!"
                },

                // WIND INSTRUMENTS - Breathy and bright!
                new InstrumentDefinition
                {
                    instrumentId = "sralai",
                    displayName = "🪈 Sralai (Quadruple-Reed Oboe)",
                    description = "Nasal and magical! Like a snake charmer's flute!",
                    category = InstrumentCategory.Wind,
                    sounds = new string[] { "waaa", "weee", "wooo", "neee" },
                    sillyMode = "Sounds like Makara with a cold! HONK HONK!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "khloy",
                    displayName = "🎺 Khloy (Bamboo Flute)",
                    description = "Sweet bamboo flute! Makes forest spirits dance!",
                    category = InstrumentCategory.Wind,
                    sounds = new string[] { "too", "tweet", "hoo", "fwee" },
                    sillyMode = "Kangrei's bamboo trying to sing!"
                },

                // SILLY BONUS INSTRUMENTS!
                new InstrumentDefinition
                {
                    instrumentId = "silly_fart_drum",
                    displayName = "💨 Whoopee Drum",
                    description = "It sounds like... well... you know! PFFFT!",
                    category = InstrumentCategory.Silly,
                    sounds = new string[] { "pffft", "prrt", "toot", "flbbbt" },
                    sillyMode = "Makara's special contribution! FIRE FART RHYTHM!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "giggle_bells",
                    displayName = "😂 Giggle Bells",
                    description = "Bells that sound like laughing! Hehehehe!",
                    category = InstrumentCategory.Silly,
                    sounds = new string[] { "hehe", "haha", "teehee", "snort" },
                    sillyMode = "Kavi's laugh turned into music!"
                },
                new InstrumentDefinition
                {
                    instrumentId = "elephant_horn",
                    displayName = "🐘 Elephant Trunk Trumpet",
                    description = "BRAAAP! Champa's trunk makes beautiful music!",
                    category = InstrumentCategory.Silly,
                    sounds = new string[] { "braap", "toot", "honk", "TRUMPET" },
                    sillyMode = "Champa's trunk sneeze, but MUSICAL!"
                }
            };
        }

        public void StartNewComposition(string title = "My Awesome Song")
        {
            _currentComposition = new MusicComposition
            {
                compositionId = Guid.NewGuid().ToString(),
                title = title,
                bpm = _bpm,
                beatsPerMeasure = _beatsPerMeasure,
                totalMeasures = _totalMeasures,
                tracks = new List<MusicTrack>(),
                createdAt = DateTime.UtcNow
            };

            Debug.Log($"🎵 Started new composition: {title}");
        }

        public void AddTrack(string instrumentId)
        {
            if (_currentComposition == null) StartNewComposition();

            var instrument = _instruments.Find(i => i.instrumentId == instrumentId);
            if (instrument == null)
            {
                Debug.LogWarning($"Instrument not found: {instrumentId}");
                return;
            }

            var track = new MusicTrack
            {
                trackId = Guid.NewGuid().ToString(),
                instrument = instrument,
                notes = new List<MusicNote>(),
                volume = 1f,
                isMuted = false
            };

            _currentComposition.tracks.Add(track);
            Debug.Log($"🎹 Added track: {instrument.displayName}");
        }

        public void AddNote(string trackId, int beat, int pitch, float duration = 1f)
        {
            var track = _currentComposition?.tracks.Find(t => t.trackId == trackId);
            if (track == null) return;

            var note = new MusicNote
            {
                beat = beat,
                pitch = pitch,
                duration = duration,
                velocity = 1f
            };

            track.notes.Add(note);
        }

        public void RemoveNote(string trackId, int beat, int pitch)
        {
            var track = _currentComposition?.tracks.Find(t => t.trackId == trackId);
            track?.notes.RemoveAll(n => n.beat == beat && n.pitch == pitch);
        }

        public void PlayComposition()
        {
            if (_currentComposition == null || _isPlaying) return;

            _isPlaying = true;
            _playbackTime = 0f;
            _currentBeat = 0;

            Debug.Log($"▶️ Playing: {_currentComposition.title}");
        }

        public void StopPlayback()
        {
            _isPlaying = false;
            _playbackTime = 0f;
            _currentBeat = 0;

            Debug.Log("⏹️ Playback stopped");
        }

        private void Update()
        {
            if (!_isPlaying || _currentComposition == null) return;

            float beatDuration = 60f / _currentComposition.bpm;
            _playbackTime += Time.deltaTime;

            int newBeat = Mathf.FloorToInt(_playbackTime / beatDuration);
            
            if (newBeat > _currentBeat)
            {
                _currentBeat = newBeat;
                
                // Play all notes on this beat
                foreach (var track in _currentComposition.tracks)
                {
                    if (track.isMuted) continue;

                    var notesOnBeat = track.notes.FindAll(n => n.beat == _currentBeat);
                    foreach (var note in notesOnBeat)
                    {
                        PlayNote(track.instrument, note);
                    }
                }

                OnBeatPlayed?.Invoke(_currentBeat);

                // Check if composition is finished
                int totalBeats = _currentComposition.totalMeasures * _currentComposition.beatsPerMeasure;
                if (_currentBeat >= totalBeats)
                {
                    _isPlaying = false;
                    OnCompositionFinished?.Invoke();
                    Debug.Log("🎉 Composition finished!");
                }
            }
        }

        private void PlayNote(InstrumentDefinition instrument, MusicNote note)
        {
            // In real implementation, this would play actual audio
            string sound = instrument.sounds[note.pitch % instrument.sounds.Length];
            Debug.Log($"🎵 {instrument.displayName}: {sound}");
        }

        public void SaveComposition()
        {
            if (_currentComposition == null) return;

            _currentComposition.savedAt = DateTime.UtcNow;
            
            // Convert to JSON for storage/sharing
            string json = JsonUtility.ToJson(_currentComposition);
            PlayerPrefs.SetString($"composition_{_currentComposition.compositionId}", json);
            
            Debug.Log($"💾 Saved: {_currentComposition.title}");
            OnCompositionSaved?.Invoke(_currentComposition);
        }

        public void SubmitToChallenge(string challengeId)
        {
            if (_currentComposition == null) return;

            SaveComposition();

            var entry = new ChallengeEntry
            {
                entryId = _currentComposition.compositionId,
                playerId = "current_player", // Would come from auth system
                playerName = "Player Name",
                entryData = JsonUtility.ToJson(_currentComposition),
                submittedAt = DateTime.UtcNow
            };

            RotatingChallengeManager.Instance?.SubmitChallengeEntry(challengeId, entry);
            Debug.Log($"📤 Submitted '{_currentComposition.title}' to challenge!");
        }

        public MusicComposition GetCurrentComposition() => _currentComposition;
        public List<InstrumentDefinition> GetAvailableInstruments() => _instruments;
        public bool IsPlaying() => _isPlaying;
    }

    #region Music Data Classes

    public enum InstrumentCategory
    {
        Drums,
        Percussion,
        Gongs,
        Xylophone,
        Strings,
        Wind,
        Silly  // Fun bonus instruments!
    }

    [Serializable]
    public class InstrumentDefinition
    {
        public string instrumentId;
        public string displayName;
        public string description;
        public InstrumentCategory category;
        public string[] sounds;
        public string sillyMode;  // Funny description!
        public Sprite icon;
        public AudioClip[] audioClips;
    }

    [Serializable]
    public class MusicComposition
    {
        public string compositionId;
        public string title;
        public string creatorId;
        public string creatorName;
        public float bpm;
        public int beatsPerMeasure;
        public int totalMeasures;
        public List<MusicTrack> tracks;
        public DateTime createdAt;
        public DateTime savedAt;
    }

    [Serializable]
    public class MusicTrack
    {
        public string trackId;
        public InstrumentDefinition instrument;
        public List<MusicNote> notes;
        public float volume;
        public bool isMuted;
    }

    [Serializable]
    public class MusicNote
    {
        public int beat;        // Which beat (0-based)
        public int pitch;       // Which sound to play
        public float duration;  // How long (in beats)
        public float velocity;  // Volume (0-1)
    }

    #endregion
}

