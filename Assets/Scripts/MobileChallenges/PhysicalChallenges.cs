using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.MobileChallenges
{
    /// <summary>
    /// PHYSICAL MOBILE CHALLENGES! 📱💪
    /// Real-world phone interactions for gameplay!
    /// Shake, slice, tilt, tap, and more!
    /// </summary>
    public class PhysicalChallenges : MonoBehaviour
    {
        public static PhysicalChallenges Instance { get; private set; }

        [Header("Active Challenge")]
        [SerializeField] private PhysicalChallenge _currentChallenge;
        [SerializeField] private bool _isChallengeActive;

        [Header("Challenge Definitions")]
        [SerializeField] private List<PhysicalChallengeDefinition> _challengePool = new List<PhysicalChallengeDefinition>();

        // Events
        public event Action<PhysicalChallenge> OnChallengeStarted;
        public event Action<float> OnChallengeProgress; // 0-1
        public event Action<PhysicalChallengeResult> OnChallengeComplete;
        public event Action OnChallengeFailed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeChallenges();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SubscribeToHardwareEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromHardwareEvents();
        }

        private void SubscribeToHardwareEvents()
        {
            if (MobileHardwareManager.Instance != null)
            {
                MobileHardwareManager.Instance.OnShakeDetected += OnShakeDetected;
                MobileHardwareManager.Instance.OnSliceDetected += OnSliceDetected;
                MobileHardwareManager.Instance.OnTiltChanged += OnTiltChanged;
                MobileHardwareManager.Instance.OnVolumeUpPressed += OnVolumeUp;
                MobileHardwareManager.Instance.OnVolumeDownPressed += OnVolumeDown;
                MobileHardwareManager.Instance.OnLoudSound += OnLoudSoundDetected;
            }
        }

        private void UnsubscribeFromHardwareEvents()
        {
            if (MobileHardwareManager.Instance != null)
            {
                MobileHardwareManager.Instance.OnShakeDetected -= OnShakeDetected;
                MobileHardwareManager.Instance.OnSliceDetected -= OnSliceDetected;
                MobileHardwareManager.Instance.OnTiltChanged -= OnTiltChanged;
                MobileHardwareManager.Instance.OnVolumeUpPressed -= OnVolumeUp;
                MobileHardwareManager.Instance.OnVolumeDownPressed -= OnVolumeDown;
                MobileHardwareManager.Instance.OnLoudSound -= OnLoudSoundDetected;
            }
        }

        private void InitializeChallenges()
        {
            _challengePool = new List<PhysicalChallengeDefinition>
            {
                // ============================================================
                // SHAKE CHALLENGES 📳
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "shake_wake_champa",
                    displayName = "🐘 Wake Up Champa!",
                    description = "Champa is sleeping! SHAKE your phone to wake her up!",
                    instructions = "Shake your phone vigorously!",
                    challengeType = PhysicalChallengeType.Shake,
                    targetCount = 10,
                    timeLimit = 10f,
                    difficulty = 1,
                    rewards = new ChallengeRewards { coins = 50, xp = 25 },
                    characterTied = "champa",
                    successMessage = "🎉 Champa woke up! GOOD MORNING!",
                    failMessage = "😴 Champa fell back asleep... Try harder!"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "shake_coconut",
                    displayName = "🥥 Coconut Shake!",
                    description = "Shake the coconut tree to get coconuts!",
                    instructions = "Shake shake shake! Get 15 coconuts!",
                    challengeType = PhysicalChallengeType.Shake,
                    targetCount = 15,
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 75, xp = 40 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "shake_earthquake",
                    displayName = "🌋 Earthquake Dance!",
                    description = "Create an EARTHQUAKE with your shaking!",
                    instructions = "MAXIMUM SHAKING! Make the ground rumble!",
                    challengeType = PhysicalChallengeType.Shake,
                    targetCount = 25,
                    timeLimit = 20f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, gems = 10 }
                },

                // ============================================================
                // SLICE CHALLENGES ⚔️ (Fruit Ninja style!)
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "slice_shadows",
                    displayName = "⚔️ Shadow Slicer!",
                    description = "Slice the shadow creatures with your phone!",
                    instructions = "Swing your phone like a sword! SLICE!",
                    challengeType = PhysicalChallengeType.Slice,
                    targetCount = 10,
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 },
                    successMessage = "⚔️ Shadow SLICED! You're a master swordsman!"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "slice_fruit_ninja",
                    displayName = "🍉 Fruit Fury!",
                    description = "SLICE ALL THE FRUIT! Watermelons, mangos, and more!",
                    instructions = "Swing in different directions to slice fruit!",
                    challengeType = PhysicalChallengeType.Slice,
                    targetCount = 20,
                    timeLimit = 20f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 },
                    requiredDirections = new SliceDirection[] { SliceDirection.Left, SliceDirection.Right, SliceDirection.Up, SliceDirection.Down }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "slice_naga_dance",
                    displayName = "🐍 Naga Sword Dance!",
                    description = "Follow the Naga Prince's sword dance pattern!",
                    instructions = "Slice in the direction shown! Left! Right! Up!",
                    challengeType = PhysicalChallengeType.SlicePattern,
                    targetCount = 15,
                    timeLimit = 30f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, gems = 10 },
                    characterTied = "naga_prince"
                },

                // ============================================================
                // TILT CHALLENGES 📐
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "tilt_balance_lotus",
                    displayName = "🪷 Lotus Balance!",
                    description = "Balance the lotus flower on the water!",
                    instructions = "Tilt your phone gently to keep the lotus centered!",
                    challengeType = PhysicalChallengeType.Tilt,
                    targetCount = 1, // 1 = stay balanced for duration
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 80, xp = 40 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "tilt_maze_runner",
                    displayName = "🌀 Temple Maze!",
                    description = "Guide the ball through the temple maze!",
                    instructions = "Tilt to roll the ball! Don't fall in holes!",
                    challengeType = PhysicalChallengeType.TiltMaze,
                    targetCount = 1,
                    timeLimit = 60f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 200, xp = 100, gems = 15 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "tilt_pour_water",
                    displayName = "💧 Pour the Blessing!",
                    description = "Pour sacred water into the vessel!",
                    instructions = "Tilt slowly to pour! Don't spill!",
                    challengeType = PhysicalChallengeType.Tilt,
                    targetCount = 1,
                    timeLimit = 20f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 75, xp = 35 }
                },

                // ============================================================
                // VOLUME BUTTON CHALLENGES 🔊
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "volume_drum_beat",
                    displayName = "🥁 Drum Master!",
                    description = "Play the drums with volume buttons!",
                    instructions = "Press volume UP and DOWN to the beat!",
                    challengeType = PhysicalChallengeType.VolumeButtons,
                    targetCount = 20,
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "volume_speed_test",
                    displayName = "⚡ Button Masher!",
                    description = "How fast can you press volume buttons?!",
                    instructions = "MASH those buttons! UP DOWN UP DOWN!",
                    challengeType = PhysicalChallengeType.VolumeButtons,
                    targetCount = 30,
                    timeLimit = 10f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "volume_rhythm",
                    displayName = "🎵 Rhythm Buttons!",
                    description = "Follow the rhythm pattern with volume buttons!",
                    instructions = "UP when you see ⬆️, DOWN when you see ⬇️!",
                    challengeType = PhysicalChallengeType.VolumeRhythm,
                    targetCount = 15,
                    timeLimit = 30f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, gems = 10 }
                },

                // ============================================================
                // HAPTIC RHYTHM CHALLENGES 📳🎵
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "haptic_heartbeat",
                    displayName = "💓 Feel the Heartbeat!",
                    description = "Tap in rhythm with the haptic heartbeat!",
                    instructions = "Feel the vibration, tap when it pulses!",
                    challengeType = PhysicalChallengeType.HapticRhythm,
                    targetCount = 20,
                    timeLimit = 30f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 },
                    hapticBPM = 80f
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "haptic_apsara_dance",
                    displayName = "💃 Blind Apsara Dance!",
                    description = "Dance to the haptic rhythm! Close your eyes!",
                    instructions = "Feel the vibrations and tap in rhythm!",
                    challengeType = PhysicalChallengeType.HapticRhythm,
                    targetCount = 30,
                    timeLimit = 45f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, gems = 10 },
                    hapticBPM = 100f,
                    characterTied = "mealea"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "haptic_morse_code",
                    displayName = "📡 Temple Signal!",
                    description = "Decode the haptic Morse code message!",
                    instructions = "Short tap for dot, long tap for dash!",
                    challengeType = PhysicalChallengeType.HapticMorse,
                    targetCount = 10,
                    timeLimit = 60f,
                    difficulty = 4,
                    rewards = new ChallengeRewards { coins = 200, xp = 100, gems = 20 }
                },

                // ============================================================
                // MICROPHONE CHALLENGES 🎤
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "mic_roar_contest",
                    displayName = "🦁 ROAR Contest!",
                    description = "ROAR as loud as you can with Sena!",
                    instructions = "When the meter appears, ROAR into your phone!",
                    challengeType = PhysicalChallengeType.Microphone,
                    targetCount = 1,
                    timeLimit = 10f,
                    difficulty = 1,
                    rewards = new ChallengeRewards { coins = 75, xp = 35 },
                    characterTied = "sena"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "mic_elephant_call",
                    displayName = "🐘 Elephant Call!",
                    description = "Make an elephant trumpet sound with Champa!",
                    instructions = "BRAAAP! Make the sound into your phone!",
                    challengeType = PhysicalChallengeType.Microphone,
                    targetCount = 3,
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 },
                    characterTied = "champa"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "mic_whisper_secret",
                    displayName = "🤫 Secret Whisper",
                    description = "Whisper the secret password to the Naga!",
                    instructions = "Whisper quietly into your phone...",
                    challengeType = PhysicalChallengeType.MicrophoneQuiet,
                    targetCount = 1,
                    timeLimit = 20f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50, specialItem = "Naga's Secret" },
                    characterTied = "naga_prince"
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "mic_blow_candles",
                    displayName = "🎂 Blow the Candles!",
                    description = "Blow into your phone to blow out birthday candles!",
                    instructions = "BLOW! Like you're blowing out candles!",
                    challengeType = PhysicalChallengeType.MicrophoneBlow,
                    targetCount = 5,
                    timeLimit = 15f,
                    difficulty = 1,
                    rewards = new ChallengeRewards { coins = 60, xp = 30 }
                },

                // ============================================================
                // CAMERA CHALLENGES 📷
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "camera_smile_detector",
                    displayName = "😊 Smile Power!",
                    description = "Your smile powers up the sun!",
                    instructions = "SMILE into the camera! Bigger smile = more power!",
                    challengeType = PhysicalChallengeType.CameraSmile,
                    targetCount = 1,
                    timeLimit = 10f,
                    difficulty = 1,
                    rewards = new ChallengeRewards { coins = 50, xp = 25 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "camera_dance_mirror",
                    displayName = "🪞 Mirror Dance!",
                    description = "Copy the character's dance moves!",
                    instructions = "Move your body to match the dance!",
                    challengeType = PhysicalChallengeType.CameraMotion,
                    targetCount = 10,
                    timeLimit = 30f,
                    difficulty = 3,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, gems = 10 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "camera_color_hunt",
                    displayName = "🎨 Color Hunter!",
                    description = "Find objects in the REAL WORLD matching colors!",
                    instructions = "Point your camera at something RED!",
                    challengeType = PhysicalChallengeType.CameraColor,
                    targetCount = 5,
                    timeLimit = 60f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 }
                },

                // ============================================================
                // GPS/LOCATION CHALLENGES 📍
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "gps_walk_steps",
                    displayName = "🚶 Temple Walk!",
                    description = "Walk around to power up your guardian!",
                    instructions = "Get up and walk! Each 100 steps = power!",
                    challengeType = PhysicalChallengeType.GPSWalk,
                    targetCount = 500, // steps
                    timeLimit = 0f, // no time limit
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 200, xp = 100, gems = 20 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "gps_explore_outside",
                    displayName = "🌳 Nature Explorer!",
                    description = "Go outside to find nature spirits!",
                    instructions = "Move to a new location to discover spirits!",
                    challengeType = PhysicalChallengeType.GPSMove,
                    targetCount = 1,
                    timeLimit = 0f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 150, xp = 75, specialItem = "Nature Spirit" }
                },

                // ============================================================
                // MULTI-TOUCH CHALLENGES 👆👆
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "touch_piano",
                    displayName = "🎹 Finger Piano!",
                    description = "Play the piano with multiple fingers!",
                    instructions = "Use multiple fingers to play the melody!",
                    challengeType = PhysicalChallengeType.MultiTouch,
                    targetCount = 20,
                    timeLimit = 30f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 }
                },
                new PhysicalChallengeDefinition
                {
                    challengeId = "touch_squish",
                    displayName = "👆 Squish the Slimes!",
                    description = "SQUISH shadow slimes with ALL your fingers!",
                    instructions = "More fingers = more squish power! Use 5+ fingers!",
                    challengeType = PhysicalChallengeType.MultiTouchSquish,
                    targetCount = 30,
                    timeLimit = 15f,
                    difficulty = 2,
                    rewards = new ChallengeRewards { coins = 100, xp = 50 }
                },

                // ============================================================
                // COMBO CHALLENGES (Multiple inputs!)
                // ============================================================
                new PhysicalChallengeDefinition
                {
                    challengeId = "combo_warrior",
                    displayName = "⚔️ Ultimate Warrior!",
                    description = "Use ALL your skills! Shake, slice, and shout!",
                    instructions = "Shake! Then Slice! Then ROAR!",
                    challengeType = PhysicalChallengeType.Combo,
                    targetCount = 3,
                    timeLimit = 20f,
                    difficulty = 4,
                    rewards = new ChallengeRewards { coins = 300, xp = 150, gems = 30, specialItem = "Warrior Medal" },
                    comboSequence = new PhysicalChallengeType[] { 
                        PhysicalChallengeType.Shake, 
                        PhysicalChallengeType.Slice, 
                        PhysicalChallengeType.Microphone 
                    }
                }
            };
        }

        #region Challenge Execution

        public void StartChallenge(string challengeId)
        {
            var definition = _challengePool.Find(c => c.challengeId == challengeId);
            if (definition == null)
            {
                Debug.LogError($"Challenge not found: {challengeId}");
                return;
            }

            StartChallenge(definition);
        }

        public void StartChallenge(PhysicalChallengeDefinition definition)
        {
            if (_isChallengeActive)
            {
                Debug.LogWarning("A challenge is already active!");
                return;
            }

            _currentChallenge = new PhysicalChallenge
            {
                definition = definition,
                currentProgress = 0,
                startTime = Time.time,
                isActive = true
            };

            _isChallengeActive = true;

            // Start haptic feedback for rhythm challenges
            if (definition.challengeType == PhysicalChallengeType.HapticRhythm && definition.hapticBPM > 0)
            {
                StartCoroutine(HapticRhythmCoroutine(definition.hapticBPM));
            }

            // Start microphone if needed
            if (definition.challengeType == PhysicalChallengeType.Microphone ||
                definition.challengeType == PhysicalChallengeType.MicrophoneQuiet ||
                definition.challengeType == PhysicalChallengeType.MicrophoneBlow)
            {
                MobileHardwareManager.Instance?.StartListening();
            }

            // Start time limit countdown
            if (definition.timeLimit > 0)
            {
                StartCoroutine(TimeLimitCoroutine(definition.timeLimit));
            }

            Debug.Log($"🎮 Challenge Started: {definition.displayName}");
            OnChallengeStarted?.Invoke(_currentChallenge);
        }

        private IEnumerator TimeLimitCoroutine(float timeLimit)
        {
            float elapsed = 0f;
            while (elapsed < timeLimit && _isChallengeActive)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_isChallengeActive && !_currentChallenge.isCompleted)
            {
                FailChallenge();
            }
        }

        private IEnumerator HapticRhythmCoroutine(float bpm)
        {
            float beatInterval = 60f / bpm;
            while (_isChallengeActive)
            {
                MobileHardwareManager.Instance?.TriggerHaptic(HapticType.Medium);
                yield return new WaitForSeconds(beatInterval);
            }
        }

        private void UpdateProgress(int amount = 1)
        {
            if (!_isChallengeActive || _currentChallenge == null) return;

            _currentChallenge.currentProgress += amount;

            float progress = (float)_currentChallenge.currentProgress / _currentChallenge.definition.targetCount;
            OnChallengeProgress?.Invoke(Mathf.Clamp01(progress));

            // Haptic feedback for progress
            MobileHardwareManager.Instance?.TriggerHaptic(HapticType.Light);

            if (_currentChallenge.currentProgress >= _currentChallenge.definition.targetCount)
            {
                CompleteChallenge();
            }
        }

        private void CompleteChallenge()
        {
            if (!_isChallengeActive || _currentChallenge == null) return;

            _currentChallenge.isCompleted = true;
            _currentChallenge.isActive = false;
            _isChallengeActive = false;

            float timeTaken = Time.time - _currentChallenge.startTime;

            // Success haptic
            MobileHardwareManager.Instance?.TriggerHaptic(HapticType.Success);

            // Stop microphone if it was used
            MobileHardwareManager.Instance?.StopListening();

            var result = new PhysicalChallengeResult
            {
                challenge = _currentChallenge,
                timeTaken = timeTaken,
                success = true,
                rewards = _currentChallenge.definition.rewards
            };

            Debug.Log($"🎉 Challenge Complete: {_currentChallenge.definition.displayName}!");
            Debug.Log($"   Time: {timeTaken:F1}s, Rewards: {result.rewards.coins} coins");

            OnChallengeComplete?.Invoke(result);
        }

        private void FailChallenge()
        {
            if (!_isChallengeActive || _currentChallenge == null) return;

            _currentChallenge.isActive = false;
            _isChallengeActive = false;

            // Failure haptic
            MobileHardwareManager.Instance?.TriggerHaptic(HapticType.Error);

            // Stop microphone if it was used
            MobileHardwareManager.Instance?.StopListening();

            Debug.Log($"❌ Challenge Failed: {_currentChallenge.definition.displayName}");
            OnChallengeFailed?.Invoke();
        }

        #endregion

        #region Hardware Event Handlers

        private void OnShakeDetected(float intensity)
        {
            if (!_isChallengeActive) return;

            if (_currentChallenge.definition.challengeType == PhysicalChallengeType.Shake)
            {
                UpdateProgress();
            }
        }

        private void OnSliceDetected(SliceDirection direction)
        {
            if (!_isChallengeActive) return;

            var type = _currentChallenge.definition.challengeType;
            if (type == PhysicalChallengeType.Slice || type == PhysicalChallengeType.SlicePattern)
            {
                // Check if specific directions are required
                var required = _currentChallenge.definition.requiredDirections;
                if (required != null && required.Length > 0)
                {
                    // Check if direction matches current requirement
                    int progressIndex = _currentChallenge.currentProgress % required.Length;
                    if (direction == required[progressIndex])
                    {
                        UpdateProgress();
                    }
                }
                else
                {
                    UpdateProgress();
                }
            }
        }

        private void OnTiltChanged(Vector3 tilt)
        {
            if (!_isChallengeActive) return;

            var type = _currentChallenge.definition.challengeType;
            if (type == PhysicalChallengeType.Tilt || type == PhysicalChallengeType.TiltMaze)
            {
                // Check if balanced (tilt near zero)
                if (Mathf.Abs(tilt.x) < 10f && Mathf.Abs(tilt.y) < 10f)
                {
                    _currentChallenge.balanceTime += Time.deltaTime;
                    if (_currentChallenge.balanceTime >= _currentChallenge.definition.timeLimit)
                    {
                        CompleteChallenge();
                    }
                }
                else
                {
                    _currentChallenge.balanceTime = 0f; // Reset if unbalanced
                }
            }
        }

        private void OnVolumeUp()
        {
            if (!_isChallengeActive) return;

            var type = _currentChallenge.definition.challengeType;
            if (type == PhysicalChallengeType.VolumeButtons || type == PhysicalChallengeType.VolumeRhythm)
            {
                UpdateProgress();
            }
        }

        private void OnVolumeDown()
        {
            if (!_isChallengeActive) return;

            var type = _currentChallenge.definition.challengeType;
            if (type == PhysicalChallengeType.VolumeButtons || type == PhysicalChallengeType.VolumeRhythm)
            {
                UpdateProgress();
            }
        }

        private void OnLoudSoundDetected(float level)
        {
            if (!_isChallengeActive) return;

            var type = _currentChallenge.definition.challengeType;

            if (type == PhysicalChallengeType.Microphone && level > 0.3f)
            {
                UpdateProgress();
            }
            else if (type == PhysicalChallengeType.MicrophoneQuiet && level < 0.1f && level > 0.01f)
            {
                UpdateProgress();
            }
            else if (type == PhysicalChallengeType.MicrophoneBlow && level > 0.2f)
            {
                UpdateProgress();
            }
        }

        #endregion

        // Public accessors
        public PhysicalChallenge GetCurrentChallenge() => _currentChallenge;
        public bool IsChallengeActive() => _isChallengeActive;
        public List<PhysicalChallengeDefinition> GetAllChallenges() => _challengePool;
    }

    #region Data Classes

    public enum PhysicalChallengeType
    {
        Shake,              // Shake the phone
        Slice,              // Fruit Ninja style slicing
        SlicePattern,       // Slice in specific directions
        Tilt,               // Balance/tilt controls
        TiltMaze,           // Tilt maze game
        VolumeButtons,      // Press volume buttons
        VolumeRhythm,       // Volume buttons to rhythm
        HapticRhythm,       // Feel vibrations, tap in rhythm
        HapticMorse,        // Decode haptic morse code
        Microphone,         // Shout/make noise
        MicrophoneQuiet,    // Whisper
        MicrophoneBlow,     // Blow into mic
        CameraSmile,        // Smile detection
        CameraMotion,       // Motion detection
        CameraColor,        // Color detection
        GPSWalk,            // Walking steps
        GPSMove,            // Move to new location
        MultiTouch,         // Multiple finger input
        MultiTouchSquish,   // Press with many fingers
        Combo               // Multiple challenge types
    }

    [Serializable]
    public class PhysicalChallengeDefinition
    {
        public string challengeId;
        public string displayName;
        public string description;
        public string instructions;
        public PhysicalChallengeType challengeType;
        public int targetCount;
        public float timeLimit;
        public int difficulty; // 1-5
        public ChallengeRewards rewards;
        public string characterTied;
        public string successMessage;
        public string failMessage;
        public float hapticBPM;
        public SliceDirection[] requiredDirections;
        public PhysicalChallengeType[] comboSequence;
        public Sprite icon;
    }

    [Serializable]
    public class PhysicalChallenge
    {
        public PhysicalChallengeDefinition definition;
        public int currentProgress;
        public float startTime;
        public bool isActive;
        public bool isCompleted;
        public float balanceTime; // For tilt challenges
        public int comboStep;     // For combo challenges
    }

    [Serializable]
    public class PhysicalChallengeResult
    {
        public PhysicalChallenge challenge;
        public float timeTaken;
        public bool success;
        public ChallengeRewards rewards;
    }

    [Serializable]
    public class ChallengeRewards
    {
        public int coins;
        public int xp;
        public int gems;
        public string specialItem;
    }

    #endregion
}

