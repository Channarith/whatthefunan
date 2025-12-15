using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WhatTheFunan.Cinematics
{
    /// <summary>
    /// Chapter transition screen shown between major story beats.
    /// </summary>
    public class ChapterTransition : MonoBehaviour
    {
        #region UI References
        [Header("Container")]
        [SerializeField] private Canvas _transitionCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private RawImage _videoBackground;
        
        [Header("Chapter Info")]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterTitleText;
        [SerializeField] private TextMeshProUGUI _chapterSubtitleText;
        [SerializeField] private Image _chapterIcon;
        
        [Header("Decorations")]
        [SerializeField] private RectTransform _topBorder;
        [SerializeField] private RectTransform _bottomBorder;
        [SerializeField] private RectTransform _leftFlourish;
        [SerializeField] private RectTransform _rightFlourish;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.8f;
        [SerializeField] private float _holdDuration = 3f;
        [SerializeField] private float _fadeOutDuration = 0.8f;
        [SerializeField] private AnimationCurve _easeCurve;
        #endregion

        #region Chapter Data
        [System.Serializable]
        public class ChapterData
        {
            public int chapterNumber;
            public string chapterTitle;
            public string subtitle;
            public Sprite backgroundSprite;
            public Sprite iconSprite;
            public Color accentColor;
            public AudioClip transitionMusic;
        }

        private static readonly ChapterData[] _chapters = new ChapterData[]
        {
            new ChapterData
            {
                chapterNumber = 1,
                chapterTitle = "The Awakening",
                subtitle = "In the peaceful village, a hero rises",
                accentColor = new Color(0.78f, 0.63f, 0.15f) // Gold
            },
            new ChapterData
            {
                chapterNumber = 2,
                chapterTitle = "Waters of the Naga",
                subtitle = "Beneath the river, an ancient kingdom awaits",
                accentColor = new Color(0.2f, 0.6f, 0.9f) // Blue
            },
            new ChapterData
            {
                chapterNumber = 3,
                chapterTitle = "Temple of Secrets",
                subtitle = "Stone guardians and forgotten mysteries",
                accentColor = new Color(0.4f, 0.3f, 0.2f) // Brown
            },
            new ChapterData
            {
                chapterNumber = 4,
                chapterTitle = "Dance of the Apsara",
                subtitle = "The celestial realm reveals its path",
                accentColor = new Color(0.9f, 0.7f, 0.9f) // Pink
            },
            new ChapterData
            {
                chapterNumber = 5,
                chapterTitle = "Shadows Gather",
                subtitle = "The enemy reveals itself",
                accentColor = new Color(0.3f, 0.1f, 0.4f) // Purple
            },
            new ChapterData
            {
                chapterNumber = 6,
                chapterTitle = "United We Stand",
                subtitle = "The guardians assemble for the final battle",
                accentColor = new Color(0.78f, 0.63f, 0.15f) // Gold
            },
            new ChapterData
            {
                chapterNumber = 7,
                chapterTitle = "Light of Funan",
                subtitle = "The prophecy fulfilled",
                accentColor = new Color(1f, 0.9f, 0.5f) // Bright Gold
            }
        };
        #endregion

        #region State
        private bool _isTransitioning;
        private Coroutine _transitionCoroutine;
        
        public bool IsTransitioning => _isTransitioning;
        #endregion

        #region Public API
        /// <summary>
        /// Show chapter transition for specified chapter.
        /// </summary>
        public void ShowChapter(int chapterNumber, System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            
            if (chapterNumber < 1 || chapterNumber > _chapters.Length)
            {
                Debug.LogWarning($"[ChapterTransition] Invalid chapter: {chapterNumber}");
                onComplete?.Invoke();
                return;
            }
            
            var data = _chapters[chapterNumber - 1];
            _transitionCoroutine = StartCoroutine(PlayTransition(data, onComplete));
        }

        /// <summary>
        /// Show custom transition with provided data.
        /// </summary>
        public void ShowCustom(ChapterData data, System.Action onComplete = null)
        {
            if (_isTransitioning) return;
            
            _transitionCoroutine = StartCoroutine(PlayTransition(data, onComplete));
        }

        /// <summary>
        /// Skip the transition.
        /// </summary>
        public void Skip()
        {
            if (!_isTransitioning) return;
            
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            
            Hide();
        }
        #endregion

        #region Transition Logic
        private IEnumerator PlayTransition(ChapterData data, System.Action onComplete)
        {
            _isTransitioning = true;
            
            // Setup visuals
            SetupVisuals(data);
            
            // Show canvas
            _transitionCanvas.gameObject.SetActive(true);
            _canvasGroup.alpha = 0;
            
            // Reset element positions for animation
            ResetElementPositions();
            
            // Play transition music
            if (data.transitionMusic != null)
            {
                Core.AudioManager.Instance?.PlaySFX(data.transitionMusic);
            }
            
            // Fade in
            yield return FadeIn();
            
            // Animate elements in
            yield return AnimateElementsIn();
            
            // Hold
            yield return new WaitForSeconds(_holdDuration);
            
            // Animate elements out
            yield return AnimateElementsOut();
            
            // Fade out
            yield return FadeOut();
            
            // Hide
            Hide();
            
            onComplete?.Invoke();
        }

        private void SetupVisuals(ChapterData data)
        {
            // Chapter number
            if (_chapterNumberText != null)
            {
                _chapterNumberText.text = $"CHAPTER {ToRomanNumeral(data.chapterNumber)}";
                _chapterNumberText.color = data.accentColor;
            }
            
            // Title
            if (_chapterTitleText != null)
            {
                string localizedTitle = Localization.LocalizationManager.Instance?.GetString($"chapter_{data.chapterNumber}_title") 
                    ?? data.chapterTitle;
                _chapterTitleText.text = localizedTitle;
            }
            
            // Subtitle
            if (_chapterSubtitleText != null)
            {
                string localizedSubtitle = Localization.LocalizationManager.Instance?.GetString($"chapter_{data.chapterNumber}_subtitle") 
                    ?? data.subtitle;
                _chapterSubtitleText.text = localizedSubtitle;
            }
            
            // Background
            if (_backgroundImage != null && data.backgroundSprite != null)
            {
                _backgroundImage.sprite = data.backgroundSprite;
            }
            
            // Icon
            if (_chapterIcon != null && data.iconSprite != null)
            {
                _chapterIcon.sprite = data.iconSprite;
                _chapterIcon.color = data.accentColor;
            }
            
            // Accent color for flourishes
            if (_leftFlourish != null)
            {
                var img = _leftFlourish.GetComponent<Image>();
                if (img != null) img.color = data.accentColor;
            }
            if (_rightFlourish != null)
            {
                var img = _rightFlourish.GetComponent<Image>();
                if (img != null) img.color = data.accentColor;
            }
        }

        private void ResetElementPositions()
        {
            // Move borders offscreen
            if (_topBorder != null)
                _topBorder.anchoredPosition = new Vector2(0, 200);
            if (_bottomBorder != null)
                _bottomBorder.anchoredPosition = new Vector2(0, -200);
            
            // Move flourishes offscreen
            if (_leftFlourish != null)
                _leftFlourish.anchoredPosition = new Vector2(-300, 0);
            if (_rightFlourish != null)
                _rightFlourish.anchoredPosition = new Vector2(300, 0);
            
            // Reset text alpha
            if (_chapterNumberText != null)
                _chapterNumberText.alpha = 0;
            if (_chapterTitleText != null)
                _chapterTitleText.alpha = 0;
            if (_chapterSubtitleText != null)
                _chapterSubtitleText.alpha = 0;
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0;
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = _easeCurve?.Evaluate(elapsed / _fadeInDuration) ?? elapsed / _fadeInDuration;
                _canvasGroup.alpha = t;
                yield return null;
            }
            _canvasGroup.alpha = 1;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = _easeCurve?.Evaluate(elapsed / _fadeOutDuration) ?? elapsed / _fadeOutDuration;
                _canvasGroup.alpha = 1 - t;
                yield return null;
            }
            _canvasGroup.alpha = 0;
        }

        private IEnumerator AnimateElementsIn()
        {
            float duration = 0.6f;
            float elapsed = 0;
            
            Vector2 topStart = new Vector2(0, 200);
            Vector2 bottomStart = new Vector2(0, -200);
            Vector2 leftStart = new Vector2(-300, 0);
            Vector2 rightStart = new Vector2(300, 0);
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutBack(elapsed / duration);
                
                // Animate borders
                if (_topBorder != null)
                    _topBorder.anchoredPosition = Vector2.Lerp(topStart, Vector2.zero, t);
                if (_bottomBorder != null)
                    _bottomBorder.anchoredPosition = Vector2.Lerp(bottomStart, Vector2.zero, t);
                
                // Animate flourishes
                if (_leftFlourish != null)
                    _leftFlourish.anchoredPosition = Vector2.Lerp(leftStart, Vector2.zero, t);
                if (_rightFlourish != null)
                    _rightFlourish.anchoredPosition = Vector2.Lerp(rightStart, Vector2.zero, t);
                
                yield return null;
            }
            
            // Fade in text with stagger
            yield return FadeInText(_chapterNumberText, 0.3f);
            yield return new WaitForSeconds(0.2f);
            yield return FadeInText(_chapterTitleText, 0.4f);
            yield return new WaitForSeconds(0.1f);
            yield return FadeInText(_chapterSubtitleText, 0.3f);
        }

        private IEnumerator AnimateElementsOut()
        {
            // Fade out text first
            StartCoroutine(FadeOutText(_chapterSubtitleText, 0.2f));
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(FadeOutText(_chapterTitleText, 0.2f));
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(FadeOutText(_chapterNumberText, 0.2f));
            
            yield return new WaitForSeconds(0.3f);
            
            // Animate elements out
            float duration = 0.4f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Move borders out
                if (_topBorder != null)
                    _topBorder.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0, 200), t);
                if (_bottomBorder != null)
                    _bottomBorder.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0, -200), t);
                
                yield return null;
            }
        }

        private IEnumerator FadeInText(TextMeshProUGUI text, float duration)
        {
            if (text == null) yield break;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                text.alpha = elapsed / duration;
                yield return null;
            }
            text.alpha = 1;
        }

        private IEnumerator FadeOutText(TextMeshProUGUI text, float duration)
        {
            if (text == null) yield break;
            
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                text.alpha = 1 - (elapsed / duration);
                yield return null;
            }
            text.alpha = 0;
        }

        private void Hide()
        {
            _isTransitioning = false;
            _transitionCanvas.gameObject.SetActive(false);
        }
        #endregion

        #region Helpers
        private string ToRomanNumeral(int number)
        {
            return number switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                6 => "VI",
                7 => "VII",
                8 => "VIII",
                9 => "IX",
                10 => "X",
                _ => number.ToString()
            };
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        #endregion
    }
}

