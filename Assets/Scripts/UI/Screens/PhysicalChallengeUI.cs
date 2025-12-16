using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using WhatTheFunan.MobileChallenges;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// PHYSICAL CHALLENGE UI! 📱
    /// Visual feedback for shake, slice, tilt, and other hardware challenges!
    /// </summary>
    public class PhysicalChallengeUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject _challengePanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Challenge Info")]
        [SerializeField] private Text _challengeTitle;
        [SerializeField] private Text _challengeDescription;
        [SerializeField] private Text _instructionsText;
        [SerializeField] private Image _challengeIcon;

        [Header("Progress")]
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Text _progressText;
        [SerializeField] private Text _timerText;

        [Header("Visual Feedback")]
        [SerializeField] private Image _feedbackFlash;
        [SerializeField] private Animator _feedbackAnimator;
        [SerializeField] private ParticleSystem _successParticles;
        [SerializeField] private ParticleSystem _progressParticles;

        [Header("Challenge-Specific Visuals")]
        [SerializeField] private GameObject _shakeVisual;
        [SerializeField] private GameObject _sliceVisual;
        [SerializeField] private GameObject _tiltVisual;
        [SerializeField] private GameObject _volumeVisual;
        [SerializeField] private GameObject _microphoneVisual;
        [SerializeField] private GameObject _cameraVisual;

        [Header("Shake Feedback")]
        [SerializeField] private Transform _shakePhone;
        [SerializeField] private Text _shakeCountText;

        [Header("Slice Feedback")]
        [SerializeField] private RectTransform _sliceArrow;
        [SerializeField] private Image[] _sliceTargets;
        [SerializeField] private TrailRenderer _sliceTrail;

        [Header("Tilt Feedback")]
        [SerializeField] private RectTransform _tiltBall;
        [SerializeField] private RectTransform _tiltTarget;
        [SerializeField] private Image _balanceMeter;

        [Header("Volume Button Feedback")]
        [SerializeField] private Image _volumeUpButton;
        [SerializeField] private Image _volumeDownButton;
        [SerializeField] private Text _volumeComboText;

        [Header("Microphone Feedback")]
        [SerializeField] private Slider _micLevelBar;
        [SerializeField] private Text _micStatusText;
        [SerializeField] private Image _soundWaveImage;

        [Header("Camera Feedback")]
        [SerializeField] private RawImage _cameraPreview;
        [SerializeField] private Image _smileDetector;

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _resultTitle;
        [SerializeField] private Text _resultMessage;
        [SerializeField] private Text _resultRewards;
        [SerializeField] private Text _resultTime;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _successColor = Color.green;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _failColor = Color.red;

        private PhysicalChallenge _currentChallenge;
        private float _challengeStartTime;
        private bool _isActive;

        private void Start()
        {
            SubscribeToEvents();
            HideAllVisuals();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            if (_isActive && _currentChallenge != null)
            {
                UpdateTimer();
                UpdateChallengeSpecificVisuals();
            }
        }

        private void SubscribeToEvents()
        {
            if (PhysicalChallenges.Instance != null)
            {
                PhysicalChallenges.Instance.OnChallengeStarted += OnChallengeStarted;
                PhysicalChallenges.Instance.OnChallengeProgress += OnChallengeProgress;
                PhysicalChallenges.Instance.OnChallengeComplete += OnChallengeComplete;
                PhysicalChallenges.Instance.OnChallengeFailed += OnChallengeFailed;
            }

            if (MobileHardwareManager.Instance != null)
            {
                MobileHardwareManager.Instance.OnShakeDetected += OnShake;
                MobileHardwareManager.Instance.OnSliceDetected += OnSlice;
                MobileHardwareManager.Instance.OnTiltChanged += OnTilt;
                MobileHardwareManager.Instance.OnVolumeUpPressed += OnVolumeUp;
                MobileHardwareManager.Instance.OnVolumeDownPressed += OnVolumeDown;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (PhysicalChallenges.Instance != null)
            {
                PhysicalChallenges.Instance.OnChallengeStarted -= OnChallengeStarted;
                PhysicalChallenges.Instance.OnChallengeProgress -= OnChallengeProgress;
                PhysicalChallenges.Instance.OnChallengeComplete -= OnChallengeComplete;
                PhysicalChallenges.Instance.OnChallengeFailed -= OnChallengeFailed;
            }

            if (MobileHardwareManager.Instance != null)
            {
                MobileHardwareManager.Instance.OnShakeDetected -= OnShake;
                MobileHardwareManager.Instance.OnSliceDetected -= OnSlice;
                MobileHardwareManager.Instance.OnTiltChanged -= OnTilt;
                MobileHardwareManager.Instance.OnVolumeUpPressed -= OnVolumeUp;
                MobileHardwareManager.Instance.OnVolumeDownPressed -= OnVolumeDown;
            }
        }

        private void OnChallengeStarted(PhysicalChallenge challenge)
        {
            _currentChallenge = challenge;
            _challengeStartTime = Time.time;
            _isActive = true;

            // Show panel
            if (_challengePanel != null)
                _challengePanel.SetActive(true);

            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            // Set challenge info
            if (_challengeTitle != null)
                _challengeTitle.text = challenge.definition.displayName;

            if (_challengeDescription != null)
                _challengeDescription.text = challenge.definition.description;

            if (_instructionsText != null)
                _instructionsText.text = challenge.definition.instructions;

            // Reset progress
            if (_progressBar != null)
            {
                _progressBar.maxValue = challenge.definition.targetCount;
                _progressBar.value = 0;
            }

            if (_progressText != null)
                _progressText.text = $"0 / {challenge.definition.targetCount}";

            // Show appropriate visual
            ShowChallengeVisual(challenge.definition.challengeType);

            // Start animations
            if (_feedbackAnimator != null)
                _feedbackAnimator.SetTrigger("Start");

            Debug.Log($"📱 UI: Challenge started - {challenge.definition.displayName}");
        }

        private void OnChallengeProgress(float progress)
        {
            if (_currentChallenge == null) return;

            int current = Mathf.RoundToInt(progress * _currentChallenge.definition.targetCount);

            if (_progressBar != null)
                _progressBar.value = current;

            if (_progressText != null)
                _progressText.text = $"{current} / {_currentChallenge.definition.targetCount}";

            // Flash feedback
            StartCoroutine(FlashFeedback(_successColor));

            // Progress particles
            if (_progressParticles != null)
                _progressParticles.Play();
        }

        private void OnChallengeComplete(PhysicalChallengeResult result)
        {
            _isActive = false;

            // Success particles
            if (_successParticles != null)
                _successParticles.Play();

            // Show result panel
            ShowResultPanel(result, true);

            // Hide challenge visuals
            HideAllVisuals();

            Debug.Log($"📱 UI: Challenge complete!");
        }

        private void OnChallengeFailed()
        {
            _isActive = false;

            // Flash red
            StartCoroutine(FlashFeedback(_failColor));

            // Show fail result
            var failResult = new PhysicalChallengeResult
            {
                challenge = _currentChallenge,
                success = false,
                timeTaken = Time.time - _challengeStartTime
            };
            ShowResultPanel(failResult, false);

            // Hide challenge visuals
            HideAllVisuals();

            Debug.Log($"📱 UI: Challenge failed!");
        }

        private void ShowResultPanel(PhysicalChallengeResult result, bool success)
        {
            if (_resultPanel != null)
                _resultPanel.SetActive(true);

            if (_challengePanel != null)
                _challengePanel.SetActive(false);

            if (_resultTitle != null)
            {
                _resultTitle.text = success ? "🎉 SUCCESS!" : "❌ TRY AGAIN!";
                _resultTitle.color = success ? _successColor : _failColor;
            }

            if (_resultMessage != null)
            {
                _resultMessage.text = success
                    ? (result.challenge.definition.successMessage ?? "Great job!")
                    : (result.challenge.definition.failMessage ?? "Better luck next time!");
            }

            if (_resultRewards != null && success)
            {
                var rewards = result.rewards;
                _resultRewards.text = $"🪙 {rewards.coins}  ⭐ {rewards.xp}";
                if (rewards.gems > 0)
                    _resultRewards.text += $"  💎 {rewards.gems}";
                if (!string.IsNullOrEmpty(rewards.specialItem))
                    _resultRewards.text += $"\n🎁 {rewards.specialItem}";
            }
            else if (_resultRewards != null)
            {
                _resultRewards.text = "No rewards this time";
            }

            if (_resultTime != null)
            {
                _resultTime.text = $"⏱️ Time: {result.timeTaken:F1}s";
            }
        }

        private void UpdateTimer()
        {
            if (_timerText == null || _currentChallenge == null) return;

            float timeLimit = _currentChallenge.definition.timeLimit;
            if (timeLimit <= 0) return;

            float elapsed = Time.time - _challengeStartTime;
            float remaining = Mathf.Max(0, timeLimit - elapsed);

            _timerText.text = $"⏱️ {remaining:F1}s";

            // Warning color when low
            if (remaining < 5f)
            {
                _timerText.color = _warningColor;
            }
            else
            {
                _timerText.color = _normalColor;
            }
        }

        private void ShowChallengeVisual(PhysicalChallengeType type)
        {
            HideAllVisuals();

            switch (type)
            {
                case PhysicalChallengeType.Shake:
                    if (_shakeVisual != null) _shakeVisual.SetActive(true);
                    break;
                case PhysicalChallengeType.Slice:
                case PhysicalChallengeType.SlicePattern:
                    if (_sliceVisual != null) _sliceVisual.SetActive(true);
                    break;
                case PhysicalChallengeType.Tilt:
                case PhysicalChallengeType.TiltMaze:
                    if (_tiltVisual != null) _tiltVisual.SetActive(true);
                    break;
                case PhysicalChallengeType.VolumeButtons:
                case PhysicalChallengeType.VolumeRhythm:
                    if (_volumeVisual != null) _volumeVisual.SetActive(true);
                    break;
                case PhysicalChallengeType.Microphone:
                case PhysicalChallengeType.MicrophoneQuiet:
                case PhysicalChallengeType.MicrophoneBlow:
                    if (_microphoneVisual != null) _microphoneVisual.SetActive(true);
                    break;
                case PhysicalChallengeType.CameraSmile:
                case PhysicalChallengeType.CameraMotion:
                case PhysicalChallengeType.CameraColor:
                    if (_cameraVisual != null) _cameraVisual.SetActive(true);
                    SetupCameraPreview();
                    break;
            }
        }

        private void HideAllVisuals()
        {
            if (_shakeVisual != null) _shakeVisual.SetActive(false);
            if (_sliceVisual != null) _sliceVisual.SetActive(false);
            if (_tiltVisual != null) _tiltVisual.SetActive(false);
            if (_volumeVisual != null) _volumeVisual.SetActive(false);
            if (_microphoneVisual != null) _microphoneVisual.SetActive(false);
            if (_cameraVisual != null) _cameraVisual.SetActive(false);
        }

        private void UpdateChallengeSpecificVisuals()
        {
            if (_currentChallenge == null) return;

            switch (_currentChallenge.definition.challengeType)
            {
                case PhysicalChallengeType.Tilt:
                case PhysicalChallengeType.TiltMaze:
                    UpdateTiltVisual();
                    break;
                case PhysicalChallengeType.Microphone:
                case PhysicalChallengeType.MicrophoneQuiet:
                case PhysicalChallengeType.MicrophoneBlow:
                    UpdateMicrophoneVisual();
                    break;
            }
        }

        #region Hardware Event Handlers

        private void OnShake(float intensity)
        {
            if (!_isActive) return;

            // Animate shake phone
            if (_shakePhone != null)
            {
                StartCoroutine(ShakePhoneAnimation());
            }

            // Update count
            if (_shakeCountText != null && _currentChallenge != null)
            {
                _shakeCountText.text = $"📳 {_currentChallenge.currentProgress}";
            }

            // Flash
            StartCoroutine(FlashFeedback(_successColor));
        }

        private IEnumerator ShakePhoneAnimation()
        {
            if (_shakePhone == null) yield break;

            Vector3 originalPos = _shakePhone.localPosition;
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-20f, 20f);
                float y = UnityEngine.Random.Range(-10f, 10f);
                _shakePhone.localPosition = originalPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakePhone.localPosition = originalPos;
        }

        private void OnSlice(SliceDirection direction)
        {
            if (!_isActive) return;

            // Animate slice arrow
            if (_sliceArrow != null)
            {
                float angle = direction switch
                {
                    SliceDirection.Up => 0f,
                    SliceDirection.Down => 180f,
                    SliceDirection.Left => 90f,
                    SliceDirection.Right => -90f,
                    _ => 0f
                };
                _sliceArrow.rotation = Quaternion.Euler(0, 0, angle);
                StartCoroutine(SliceAnimation());
            }

            // Flash
            StartCoroutine(FlashFeedback(_successColor));
        }

        private IEnumerator SliceAnimation()
        {
            if (_sliceArrow == null) yield break;

            Vector3 originalScale = _sliceArrow.localScale;
            _sliceArrow.localScale = originalScale * 1.5f;

            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _sliceArrow.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _sliceArrow.localScale = originalScale;
        }

        private void OnTilt(Vector3 tilt)
        {
            // Tilt visual is updated in UpdateTiltVisual()
        }

        private void UpdateTiltVisual()
        {
            if (_tiltBall == null || MobileHardwareManager.Instance == null) return;

            Vector3 tilt = MobileHardwareManager.Instance.GetTilt();

            // Move ball based on tilt
            float maxOffset = 100f;
            float x = Mathf.Clamp(tilt.y * 2f, -maxOffset, maxOffset);
            float y = Mathf.Clamp(-tilt.x * 2f, -maxOffset, maxOffset);

            _tiltBall.anchoredPosition = new Vector2(x, y);

            // Update balance meter
            if (_balanceMeter != null)
            {
                float distance = new Vector2(x, y).magnitude;
                float balance = 1f - Mathf.Clamp01(distance / maxOffset);
                _balanceMeter.fillAmount = balance;
                _balanceMeter.color = balance > 0.8f ? _successColor : (balance > 0.5f ? _warningColor : _failColor);
            }
        }

        private void OnVolumeUp()
        {
            if (!_isActive) return;

            if (_volumeUpButton != null)
            {
                StartCoroutine(ButtonPressAnimation(_volumeUpButton));
            }

            StartCoroutine(FlashFeedback(_successColor));
        }

        private void OnVolumeDown()
        {
            if (!_isActive) return;

            if (_volumeDownButton != null)
            {
                StartCoroutine(ButtonPressAnimation(_volumeDownButton));
            }

            StartCoroutine(FlashFeedback(_successColor));
        }

        private IEnumerator ButtonPressAnimation(Image button)
        {
            if (button == null) yield break;

            Color originalColor = button.color;
            button.color = _successColor;

            yield return new WaitForSeconds(0.1f);

            button.color = originalColor;
        }

        private void UpdateMicrophoneVisual()
        {
            if (MobileHardwareManager.Instance == null) return;

            float level = MobileHardwareManager.Instance.GetMicrophoneLevel();

            if (_micLevelBar != null)
            {
                _micLevelBar.value = level;
            }

            if (_micStatusText != null)
            {
                if (level > 0.3f)
                    _micStatusText.text = "🔊 LOUD!";
                else if (level > 0.1f)
                    _micStatusText.text = "🔉 Normal";
                else
                    _micStatusText.text = "🤫 Quiet";
            }
        }

        private void SetupCameraPreview()
        {
            if (_cameraPreview == null || MobileHardwareManager.Instance == null) return;

            MobileHardwareManager.Instance.StartCamera(true);
            var texture = MobileHardwareManager.Instance.GetCameraTexture();
            if (texture != null)
            {
                _cameraPreview.texture = texture;
            }
        }

        #endregion

        private IEnumerator FlashFeedback(Color color)
        {
            if (_feedbackFlash == null) yield break;

            _feedbackFlash.color = color;
            _feedbackFlash.gameObject.SetActive(true);

            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float alpha = Mathf.Lerp(0.5f, 0f, elapsed / duration);
                _feedbackFlash.color = new Color(color.r, color.g, color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _feedbackFlash.gameObject.SetActive(false);
        }

        // Button callbacks
        public void OnRetryClicked()
        {
            if (_currentChallenge != null)
            {
                PhysicalChallenges.Instance?.StartChallenge(_currentChallenge.definition);
            }
        }

        public void OnCloseClicked()
        {
            if (_challengePanel != null)
                _challengePanel.SetActive(false);
            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            _isActive = false;

            // Stop camera if it was used
            MobileHardwareManager.Instance?.StopCamera();
        }
    }
}

