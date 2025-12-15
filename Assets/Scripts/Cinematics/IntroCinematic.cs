using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Cinematics
{
    /// <summary>
    /// The game's opening cinematic - "The Legend of Funan"
    /// A hybrid of in-engine animation and scripted narrative.
    /// </summary>
    public class IntroCinematic : MonoBehaviour
    {
        #region Storyboard
        /*
         * =====================================================================
         * INTRO CINEMATIC STORYBOARD - "The Legend of Funan"
         * Duration: ~90 seconds
         * =====================================================================
         * 
         * SCENE 1: The Ancient Kingdom (0-15s)
         * - Fade in from black
         * - Aerial view of lush jungle with golden temples
         * - Narrator: "Long ago, in the mists of time..."
         * - Camera descends through clouds
         * 
         * SCENE 2: The Golden Age (15-30s)
         * - Pan across bustling river port (Óc Eo)
         * - Ships from many lands, traders exchanging goods
         * - Narrator: "...there was a kingdom where East met West..."
         * - Show diverse merchants, animals, celebration
         * 
         * SCENE 3: The Guardian Creatures (30-50s)
         * - Montage of the seven guardian creatures
         * - Champa the Elephant leading, Kavi the Monkey playing
         * - Naga rising from waters, Apsara dancing
         * - Narrator: "Protected by magical creatures, chosen by the gods..."
         * 
         * SCENE 4: The Shadow Threat (50-70s)
         * - Sky darkens, shadows creep across land
         * - Shadow Serpent silhouette rises
         * - Guardians scatter, kingdom in peril
         * - Narrator: "But darkness came... and the guardians were separated..."
         * 
         * SCENE 5: The Prophecy (70-85s)
         * - Ancient stone tablet glowing
         * - Prophecy text appears
         * - Narrator: "Yet hope remains. The prophecy speaks of a hero..."
         * - Flash of light, player character silhouette
         * 
         * SCENE 6: Title Card (85-90s)
         * - "WHAT THE FUNAN" logo crashes in
         * - Epic music swell
         * - Fade to gameplay
         * 
         * =====================================================================
         */
        #endregion

        #region UI References
        [Header("Canvas & Overlays")]
        [SerializeField] private Canvas _cinematicCanvas;
        [SerializeField] private CanvasGroup _fadeOverlay;
        [SerializeField] private RawImage _backgroundImage;
        [SerializeField] private RawImage _foregroundImage;
        
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _narratorText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private CanvasGroup _narratorPanel;
        
        [Header("Title")]
        [SerializeField] private RectTransform _titleLogo;
        [SerializeField] private CanvasGroup _titleCanvasGroup;
        
        [Header("Skip UI")]
        [SerializeField] private GameObject _skipPrompt;
        [SerializeField] private Slider _skipProgress;
        #endregion

        #region Sequence Data
        [Header("Timing")]
        [SerializeField] private float _sceneDuration1 = 15f;
        [SerializeField] private float _sceneDuration2 = 15f;
        [SerializeField] private float _sceneDuration3 = 20f;
        [SerializeField] private float _sceneDuration4 = 20f;
        [SerializeField] private float _sceneDuration5 = 15f;
        [SerializeField] private float _sceneDuration6 = 5f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _introMusic;
        [SerializeField] private AudioClip _narratorVoiceover;
        [SerializeField] private AudioClip _titleSFX;
        #endregion

        #region Narration Texts
        private readonly List<NarrationSegment> _narration = new List<NarrationSegment>
        {
            // Scene 1
            new NarrationSegment
            {
                startTime = 2f,
                endTime = 12f,
                text = "Long ago, in the mists of time, when the great rivers shaped the land...",
                locKey = "intro_narration_1"
            },
            // Scene 2
            new NarrationSegment
            {
                startTime = 17f,
                endTime = 28f,
                text = "There was a kingdom where East met West, where traders from distant lands brought wonders untold.",
                locKey = "intro_narration_2"
            },
            // Scene 3 
            new NarrationSegment
            {
                startTime = 32f,
                endTime = 48f,
                text = "Protected by magical creatures, chosen by the gods themselves. The guardians of Funan watched over all.",
                locKey = "intro_narration_3"
            },
            // Scene 4
            new NarrationSegment
            {
                startTime = 52f,
                endTime = 68f,
                text = "But darkness came from the depths. The Shadow Serpent rose, and the guardians were scattered across the land.",
                locKey = "intro_narration_4"
            },
            // Scene 5
            new NarrationSegment
            {
                startTime = 72f,
                endTime = 84f,
                text = "Yet hope remains. The ancient prophecy speaks of a hero who will reunite the guardians and restore the light.",
                locKey = "intro_narration_5"
            }
        };
        #endregion

        #region State
        private bool _isPlaying;
        private bool _isSkipping;
        private float _currentTime;
        private float _skipHoldTime;
        private const float SKIP_REQUIRED_HOLD = 1.5f;
        private int _currentNarrationIndex = -1;
        private Coroutine _introCoroutine;
        #endregion

        #region Public API
        /// <summary>
        /// Start playing the intro cinematic.
        /// </summary>
        public void Play()
        {
            if (_isPlaying) return;
            
            _isPlaying = true;
            _currentTime = 0f;
            _currentNarrationIndex = -1;
            
            gameObject.SetActive(true);
            _cinematicCanvas.gameObject.SetActive(true);
            
            // Start music
            if (_introMusic != null)
            {
                Core.AudioManager.Instance?.PlayMusic(_introMusic, true);
            }
            
            _introCoroutine = StartCoroutine(PlayIntroSequence());
        }

        /// <summary>
        /// Skip the intro (if allowed).
        /// </summary>
        public void Skip()
        {
            if (!_isPlaying || _isSkipping) return;
            
            _isSkipping = true;
            
            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
            }
            
            StartCoroutine(SkipToEnd());
        }

        /// <summary>
        /// Stop immediately without transition.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _isSkipping = false;
            
            if (_introCoroutine != null)
            {
                StopCoroutine(_introCoroutine);
                _introCoroutine = null;
            }
            
            gameObject.SetActive(false);
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isPlaying) return;
            
            _currentTime += Time.deltaTime;
            UpdateSkipInput();
            UpdateNarration();
        }
        #endregion

        #region Main Sequence
        private IEnumerator PlayIntroSequence()
        {
            // Initial fade in
            yield return FadeFromBlack(2f);
            
            // Scene 1: The Ancient Kingdom
            yield return PlayScene1();
            
            // Scene 2: The Golden Age
            yield return PlayScene2();
            
            // Scene 3: The Guardian Creatures
            yield return PlayScene3();
            
            // Scene 4: The Shadow Threat
            yield return PlayScene4();
            
            // Scene 5: The Prophecy
            yield return PlayScene5();
            
            // Scene 6: Title Card
            yield return PlayScene6();
            
            // Complete
            OnIntroComplete();
        }

        private IEnumerator PlayScene1()
        {
            Debug.Log("[IntroCinematic] Scene 1: The Ancient Kingdom");
            
            // Would show aerial jungle/temple view
            // For now, just wait
            
            float sceneStart = _currentTime;
            while (_currentTime - sceneStart < _sceneDuration1 && !_isSkipping)
            {
                yield return null;
            }
        }

        private IEnumerator PlayScene2()
        {
            Debug.Log("[IntroCinematic] Scene 2: The Golden Age");
            
            // Would show bustling port scene
            
            yield return CrossfadeBackground();
            
            float sceneStart = _currentTime;
            while (_currentTime - sceneStart < _sceneDuration2 && !_isSkipping)
            {
                yield return null;
            }
        }

        private IEnumerator PlayScene3()
        {
            Debug.Log("[IntroCinematic] Scene 3: The Guardian Creatures");
            
            // Would show montage of guardians
            // Champa, Kavi, Naga, Apsara, Makara, Prohm, Singha
            
            yield return CrossfadeBackground();
            
            float sceneStart = _currentTime;
            while (_currentTime - sceneStart < _sceneDuration3 && !_isSkipping)
            {
                yield return null;
            }
        }

        private IEnumerator PlayScene4()
        {
            Debug.Log("[IntroCinematic] Scene 4: The Shadow Threat");
            
            // Darken scene, show Shadow Serpent
            yield return DarkenScene();
            
            // Camera shake
            Core.HapticManager.Instance?.TriggerHeavy();
            
            float sceneStart = _currentTime;
            while (_currentTime - sceneStart < _sceneDuration4 && !_isSkipping)
            {
                yield return null;
            }
        }

        private IEnumerator PlayScene5()
        {
            Debug.Log("[IntroCinematic] Scene 5: The Prophecy");
            
            // Show glowing prophecy tablet
            yield return CrossfadeBackground();
            
            float sceneStart = _currentTime;
            while (_currentTime - sceneStart < _sceneDuration5 && !_isSkipping)
            {
                yield return null;
            }
        }

        private IEnumerator PlayScene6()
        {
            Debug.Log("[IntroCinematic] Scene 6: Title Card");
            
            // Epic title reveal
            yield return RevealTitle();
            
            // Play title sound
            if (_titleSFX != null)
            {
                Core.AudioManager.Instance?.PlaySFX(_titleSFX);
            }
            
            Core.HapticManager.Instance?.TriggerHeavy();
            
            yield return new WaitForSeconds(_sceneDuration6);
        }

        private void OnIntroComplete()
        {
            _isPlaying = false;
            
            // Notify CinematicsManager
            CinematicsManager.Instance?.OnCinematicComplete("intro");
            
            // Load main menu or gameplay
            Core.SceneController.Instance?.LoadScene("MainMenu");
        }
        #endregion

        #region Visual Effects
        private IEnumerator FadeFromBlack(float duration)
        {
            if (_fadeOverlay == null) yield break;
            
            _fadeOverlay.alpha = 1f;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            
            _fadeOverlay.alpha = 0f;
        }

        private IEnumerator FadeToBlack(float duration)
        {
            if (_fadeOverlay == null) yield break;
            
            _fadeOverlay.alpha = 0f;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = elapsed / duration;
                yield return null;
            }
            
            _fadeOverlay.alpha = 1f;
        }

        private IEnumerator CrossfadeBackground()
        {
            // Would crossfade between background images
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator DarkenScene()
        {
            // Would apply darkening post-process or overlay
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator RevealTitle()
        {
            if (_titleLogo == null || _titleCanvasGroup == null) yield break;
            
            // Start offscreen and scaled up
            _titleLogo.localScale = Vector3.one * 3f;
            _titleCanvasGroup.alpha = 0f;
            
            float duration = 0.8f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = EaseOutBack(t);
                
                _titleLogo.localScale = Vector3.Lerp(Vector3.one * 3f, Vector3.one, easeT);
                _titleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 2f);
                
                yield return null;
            }
            
            _titleLogo.localScale = Vector3.one;
            _titleCanvasGroup.alpha = 1f;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        #endregion

        #region Narration
        private void UpdateNarration()
        {
            // Find current narration segment
            for (int i = 0; i < _narration.Count; i++)
            {
                var segment = _narration[i];
                
                if (_currentTime >= segment.startTime && _currentTime <= segment.endTime)
                {
                    if (i != _currentNarrationIndex)
                    {
                        _currentNarrationIndex = i;
                        ShowNarration(segment);
                    }
                    return;
                }
            }
            
            // No narration - hide panel
            if (_narratorPanel != null && _narratorPanel.alpha > 0)
            {
                StartCoroutine(FadeNarratorPanel(false));
            }
        }

        private void ShowNarration(NarrationSegment segment)
        {
            if (_narratorText != null)
            {
                // Get localized text
                string text = Localization.LocalizationManager.Instance?.GetString(segment.locKey) ?? segment.text;
                _narratorText.text = text;
            }
            
            if (_narratorPanel != null)
            {
                StartCoroutine(FadeNarratorPanel(true));
            }
        }

        private IEnumerator FadeNarratorPanel(bool show)
        {
            if (_narratorPanel == null) yield break;
            
            float start = _narratorPanel.alpha;
            float end = show ? 1f : 0f;
            float duration = 0.3f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _narratorPanel.alpha = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            
            _narratorPanel.alpha = end;
        }
        #endregion

        #region Skip
        private void UpdateSkipInput()
        {
            bool holdingSkip = Input.GetKey(KeyCode.Space) || 
                              Input.GetKey(KeyCode.Escape) ||
                              (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Stationary);
            
            if (holdingSkip)
            {
                _skipHoldTime += Time.deltaTime;
                
                // Show skip prompt
                if (_skipPrompt != null)
                {
                    _skipPrompt.SetActive(true);
                }
                
                if (_skipProgress != null)
                {
                    _skipProgress.value = _skipHoldTime / SKIP_REQUIRED_HOLD;
                }
                
                if (_skipHoldTime >= SKIP_REQUIRED_HOLD)
                {
                    Skip();
                }
            }
            else
            {
                _skipHoldTime = Mathf.Max(0, _skipHoldTime - Time.deltaTime * 3f);
                
                if (_skipProgress != null)
                {
                    _skipProgress.value = _skipHoldTime / SKIP_REQUIRED_HOLD;
                }
                
                if (_skipHoldTime <= 0 && _skipPrompt != null)
                {
                    _skipPrompt.SetActive(false);
                }
            }
        }

        private IEnumerator SkipToEnd()
        {
            // Quick fade to black
            yield return FadeToBlack(0.5f);
            
            // Complete
            OnIntroComplete();
        }
        #endregion

        #region Data Classes
        [System.Serializable]
        private class NarrationSegment
        {
            public float startTime;
            public float endTime;
            public string text;
            public string locKey;
        }
        #endregion
    }
}

